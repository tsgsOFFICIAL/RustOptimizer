using RustOptimizer.ViewModels.Mvvm;
using System.Collections.Generic;
using RustOptimizer.Interface;
using System.Threading.Tasks;
using RustOptimizer.Service;
using Avalonia.Threading;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Drives the Network page: live active-adapter info (link speed, throughput, ping/jitter), a
/// one-time local network lookup, an opt-in public-IP lookup, and three OS-level network tweaks.
/// Every tweak write requires administrator rights, so toggling one re-launches this app elevated
/// via <see cref="ElevationHelper"/> instead of writing in-process.
/// </summary>
public sealed class NetworkViewModel : ViewModelBase
{
    private const string NotAvailable = "N/A";

    // Distinct from NotAvailable, matching SystemViewModel's convention - the ping result sits
    // here for up to 2 seconds (PingReferenceHostAsync's timeout) before the first real reading.
    private const string Loading = "…";
    private const string SpeedTestUrl = "https://www.speedtest.net/";

    // How many recent ping samples feed the jitter calculation - at one sample/second, 20 covers
    // the last ~20 seconds, long enough to smooth out a single blip without going stale.
    private const int JitterWindowSize = 20;

    private readonly INetworkTweaksService _networkTweaks;
    private readonly IAppSettingsService _settings;
    private readonly IDialogService _dialogs;
    private DispatcherTimer? _pollTimer;
    private bool _isPolling;
    private readonly Queue<long> _recentPingsMs = new();
    private NetworkByteCounters? _previousByteCounters;
    private DateTime _previousByteCountersTimestamp;

    // Shown once per app session, the first time any tweak is toggled - the OS's own UAC prompt
    // is the real per-action confirmation gate, so this only needs to explain why it's about to appear.
    private bool _hasShownElevationNotice;

    private string _adapterNameText = NotAvailable;
    private string _linkSpeedText = NotAvailable;
    private string _macAddressText = NotAvailable;
    private string _ipv4AddressText = NotAvailable;
    private string _gatewayText = NotAvailable;
    private string _dnsServersText = NotAvailable;
    private bool _publicIpRequested;
    private string _publicIpText = "";
    private string _pingResultText = Loading;
    private string _pingJitterText = NotAvailable;
    private string _downloadRateText = NotAvailable;
    private string _uploadRateText = NotAvailable;
    private bool? _isWireless;

    private bool _networkThrottlingDisabled;
    private bool? _nicPowerSavingDisabled;
    private bool _qosReservedBandwidthDisabled;

    /// <summary>Creates the view model and kicks off the tweaks load. The public IP is opt-in - nothing is fetched until requested.</summary>
    public NetworkViewModel(ILocalizationService localization, INetworkTweaksService networkTweaks,
        IAppSettingsService settings, IDialogService dialogs)
        : base(localization)
    {
        _networkTweaks = networkTweaks;
        _settings = settings;
        _dialogs = dialogs;

        OpenSpeedTestCommand = new RelayCommand(() => Utility.OpenUrl(SpeedTestUrl));
        LookupPublicIpCommand = new RelayCommand(() => _ = LookupPublicIpAsync());

        _ = LoadTweaksAsync();
    }

    /// <summary>The active adapter's name, or a fallback message if none could be resolved.</summary>
    public string AdapterNameText
    {
        get => _adapterNameText;
        private set => SetProperty(ref _adapterNameText, value);
    }

    /// <summary>
    /// Whether the wired-connection warning icon should show - true once the active adapter is
    /// known and it's Wi-Fi rather than Ethernet. Informational only; there's nothing for the app
    /// to toggle here, unlike the tweaks below.
    /// </summary>
    public bool WiredConnectionWarningVisible =>
        _isWireless is { } isWireless && !NetworkOptimizationRecommendations.IsWiredConnectionRecommended(isWireless);

    /// <summary>The active adapter's formatted link speed.</summary>
    public string LinkSpeedText
    {
        get => _linkSpeedText;
        private set => SetProperty(ref _linkSpeedText, value);
    }

    /// <summary>The active adapter's MAC address.</summary>
    public string MacAddressText
    {
        get => _macAddressText;
        private set => SetProperty(ref _macAddressText, value);
    }

    /// <summary>The active adapter's local IPv4 address.</summary>
    public string IPv4AddressText
    {
        get => _ipv4AddressText;
        private set => SetProperty(ref _ipv4AddressText, value);
    }

    /// <summary>The active adapter's default gateway.</summary>
    public string GatewayText
    {
        get => _gatewayText;
        private set => SetProperty(ref _gatewayText, value);
    }

    /// <summary>The active adapter's configured DNS servers, comma-separated.</summary>
    public string DnsServersText
    {
        get => _dnsServersText;
        private set => SetProperty(ref _dnsServersText, value);
    }

    /// <summary>
    /// Whether the public IP lookup has been requested this session. Starts <see langword="false"/>
    /// so nothing is sent to a third party just from opening the page - the row shows
    /// <see cref="LookupPublicIpCommand"/>'s button until the user explicitly asks for it.
    /// </summary>
    public bool PublicIpRequested
    {
        get => _publicIpRequested;
        private set => SetProperty(ref _publicIpRequested, value);
    }

    /// <summary>This machine's public-facing IP address, empty until <see cref="LookupPublicIpCommand"/> is used.</summary>
    public string PublicIpText
    {
        get => _publicIpText;
        private set => SetProperty(ref _publicIpText, value);
    }

    /// <summary>Looks up this machine's public IP address via a third-party service. Only ever called on explicit user request.</summary>
    public RelayCommand LookupPublicIpCommand { get; }

    /// <summary>The round-trip time of the most recent ping to the reference host, refreshed every second.</summary>
    public string PingResultText
    {
        get => _pingResultText;
        private set => SetProperty(ref _pingResultText, value);
    }

    /// <summary>
    /// The average change between consecutive recent pings (jitter) - a spiky, inconsistent
    /// connection matters more for gaming than the raw ping number alone.
    /// </summary>
    public string PingJitterText
    {
        get => _pingJitterText;
        private set => SetProperty(ref _pingJitterText, value);
    }

    /// <summary>The active adapter's live download rate, computed between the last two poll ticks.</summary>
    public string DownloadRateText
    {
        get => _downloadRateText;
        private set => SetProperty(ref _downloadRateText, value);
    }

    /// <summary>The active adapter's live upload rate, computed between the last two poll ticks.</summary>
    public string UploadRateText
    {
        get => _uploadRateText;
        private set => SetProperty(ref _uploadRateText, value);
    }

    /// <summary>Opens Speedtest.net for a full download/upload/latency test, since the built-in ping is latency-only.</summary>
    public RelayCommand OpenSpeedTestCommand { get; }

    /// <summary>Whether the Multimedia Class Scheduler Service's network throttling is disabled.</summary>
    public bool NetworkThrottlingDisabled
    {
        get => _networkThrottlingDisabled;
        set
        {
            if (!SetProperty(ref _networkThrottlingDisabled, value))
                return;

            OnPropertyChanged(nameof(NetworkThrottlingWarningVisible));
            _ = ApplyElevatedTweakAsync("NetworkThrottling", value, () => _networkThrottlingDisabled = !value,
                nameof(NetworkThrottlingDisabled), nameof(NetworkThrottlingWarningVisible));
        }
    }

    /// <summary>Whether the network-throttling warning icon should show - true when it's not at its recommended value.</summary>
    public bool NetworkThrottlingWarningVisible => !NetworkOptimizationRecommendations.IsNetworkThrottlingRecommended(NetworkThrottlingDisabled);

    /// <summary>
    /// Whether NIC power-saving is disabled for the active adapter. <see langword="null"/> until
    /// resolved, or if the adapter's driver Class subkey couldn't be found - the row stays hidden
    /// (see <see cref="ShowNicPowerSavingToggle"/>) until then.
    /// </summary>
    public bool? NicPowerSavingDisabled
    {
        get => _nicPowerSavingDisabled;
        set
        {
            if (value is not { } disabled || !SetProperty(ref _nicPowerSavingDisabled, value))
                return;

            OnPropertyChanged(nameof(NicPowerSavingWarningVisible));
            _ = ApplyElevatedTweakAsync("NicPowerSaving", disabled, () => _nicPowerSavingDisabled = !disabled,
                nameof(NicPowerSavingDisabled), nameof(NicPowerSavingWarningVisible));
        }
    }

    /// <summary>Whether the active adapter's power-saving state could be resolved, so the toggle has something to act on.</summary>
    public bool ShowNicPowerSavingToggle => _nicPowerSavingDisabled.HasValue;

    /// <summary>Whether the NIC power-saving warning icon should show - true when applicable and not at its recommended value.</summary>
    public bool NicPowerSavingWarningVisible =>
        NicPowerSavingDisabled is { } disabled && !NetworkOptimizationRecommendations.IsNicPowerSavingRecommended(disabled);

    /// <summary>Whether QoS's reserved-bandwidth limit for non-best-effort traffic is disabled.</summary>
    public bool QosReservedBandwidthDisabled
    {
        get => _qosReservedBandwidthDisabled;
        set
        {
            if (!SetProperty(ref _qosReservedBandwidthDisabled, value))
                return;

            OnPropertyChanged(nameof(QosReservedBandwidthWarningVisible));
            _ = ApplyElevatedTweakAsync("QosReservedBandwidth", value, () => _qosReservedBandwidthDisabled = !value,
                nameof(QosReservedBandwidthDisabled), nameof(QosReservedBandwidthWarningVisible));
        }
    }

    /// <summary>Whether the QoS warning icon should show - true when it's not at its recommended value.</summary>
    public bool QosReservedBandwidthWarningVisible =>
        !NetworkOptimizationRecommendations.IsQosReservedBandwidthRecommended(QosReservedBandwidthDisabled);

    /// <summary>
    /// Re-reads every network tweak from the registry. The view model is cached across visits (see
    /// <c>MainWindowViewModel.Navigate</c>), so without this, a tweak changed outside the app would
    /// keep showing whatever this instance last knew. Call from the view's attach-to-visual-tree
    /// lifecycle so it's current on every visit.
    /// </summary>
    public void RefreshTweaks() => _ = LoadTweaksAsync();

    /// <summary>
    /// Starts polling the active adapter's name/link speed and a fresh ping to the reference host
    /// every second - both can change without the app's involvement (Wi-Fi roaming to a different
    /// rate, a cable swap, reconnecting to a different network, or the connection just getting
    /// worse/better). Call from the view's attach-to-visual-tree lifecycle so polling pauses while
    /// a different page is showing.
    /// </summary>
    public void StartPolling()
    {
        _ = PollAsync();

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _pollTimer.Tick += async (_, _) => await PollAsync();
        _pollTimer.Start();
    }

    /// <summary>Stops polling. Call from the view's detach-from-visual-tree lifecycle.</summary>
    public void StopPolling()
    {
        _pollTimer?.Stop();
        _pollTimer = null;
    }

    /// <summary>
    /// Loads the active adapter's name/link speed/local network config, its live throughput, and a
    /// fresh ping to the reference host, skipping a tick if the previous one is still running - the
    /// ping's own up-to-2-second timeout means a tick can occasionally overrun its 1-second interval.
    /// </summary>
    private async Task PollAsync()
    {
        if (_isPolling)
            return;

        _isPolling = true;
        try
        {
            (NetworkAdapterInfo? info, NetworkByteCounters? byteCounters) = await Task.Run(() =>
                (_networkTweaks.GetActiveAdapterInfo(), _networkTweaks.GetActiveAdapterByteCounters()));

            AdapterNameText = info?.Name ?? Localization["NoActiveAdapterText"];
            LinkSpeedText = info is { } resolved ? FormatLinkSpeed(resolved.LinkSpeedBps) : NotAvailable;
            MacAddressText = info is { MacAddress.Length: > 0 } withMac ? withMac.MacAddress : NotAvailable;
            IPv4AddressText = info?.IPv4Address ?? NotAvailable;
            GatewayText = info?.GatewayAddress ?? NotAvailable;
            DnsServersText = info is { DnsServers.Count: > 0 } withDns ? string.Join(", ", withDns.DnsServers) : NotAvailable;
            _isWireless = info?.IsWireless;
            OnPropertyChanged(nameof(WiredConnectionWarningVisible));

            UpdateThroughput(byteCounters);

            long? roundTripMs = await _networkTweaks.PingReferenceHostAsync();
            PingResultText = roundTripMs is { } ms
                ? string.Format(Localization["PingSucceededFormat"], ms)
                : Localization["PingFailedText"];
            UpdateJitter(roundTripMs);
        }
        finally
        {
            _isPolling = false;
        }
    }

    /// <summary>
    /// Looks up the public IP address on explicit request, once per session - not part of the
    /// per-second poll, both to avoid hammering a third-party service and because nothing should
    /// leave the machine just from opening the page.
    /// </summary>
    private async Task LookupPublicIpAsync()
    {
        if (PublicIpRequested)
            return;

        PublicIpRequested = true;
        PublicIpText = Loading;
        PublicIpText = await _networkTweaks.GetPublicIpAddressAsync() ?? NotAvailable;
    }

    /// <summary>
    /// Computes a live download/upload rate from the change in cumulative byte counters since the
    /// last tick. Clamped at zero to absorb a counter reset (e.g. the adapter reconnected) instead
    /// of showing a negative rate.
    /// </summary>
    private void UpdateThroughput(NetworkByteCounters? current)
    {
        DateTime now = DateTime.UtcNow;

        if (current is { } counters && _previousByteCounters is { } previous)
        {
            double elapsedSeconds = (now - _previousByteCountersTimestamp).TotalSeconds;
            if (elapsedSeconds > 0)
            {
                DownloadRateText = FormatThroughput(Math.Max(0, counters.BytesReceived - previous.BytesReceived) / elapsedSeconds);
                UploadRateText = FormatThroughput(Math.Max(0, counters.BytesSent - previous.BytesSent) / elapsedSeconds);
            }
        }

        _previousByteCounters = current;
        _previousByteCountersTimestamp = now;
    }

    /// <summary>
    /// Adds a successful ping to the rolling jitter window (capped at <see cref="JitterWindowSize"/>)
    /// and recomputes <see cref="PingJitterText"/> as the average change between consecutive samples.
    /// </summary>
    private void UpdateJitter(long? roundTripMs)
    {
        if (roundTripMs is not { } ms)
            return;

        _recentPingsMs.Enqueue(ms);
        while (_recentPingsMs.Count > JitterWindowSize)
            _recentPingsMs.Dequeue();

        if (_recentPingsMs.Count < 2)
            return;

        long[] samples = [.. _recentPingsMs];
        double averageDelta = Enumerable.Range(1, samples.Length - 1).Average(i => Math.Abs(samples[i] - samples[i - 1]));
        PingJitterText = $"{averageDelta:0.#} ms";
    }

    /// <summary>Loads the current state of every network tweak.</summary>
    private async Task LoadTweaksAsync()
    {
        NetworkTweaksSettings settings = await Task.Run(_networkTweaks.GetNetworkTweaksSettings);

        _networkThrottlingDisabled = settings.NetworkThrottlingDisabled ?? false;
        OnPropertyChanged(nameof(NetworkThrottlingDisabled));
        OnPropertyChanged(nameof(NetworkThrottlingWarningVisible));

        _nicPowerSavingDisabled = settings.NicPowerSavingDisabled;
        OnPropertyChanged(nameof(NicPowerSavingDisabled));
        OnPropertyChanged(nameof(ShowNicPowerSavingToggle));
        OnPropertyChanged(nameof(NicPowerSavingWarningVisible));

        _qosReservedBandwidthDisabled = settings.QosReservedBandwidthDisabled ?? false;
        OnPropertyChanged(nameof(QosReservedBandwidthDisabled));
        OnPropertyChanged(nameof(QosReservedBandwidthWarningVisible));
    }

    /// <summary>
    /// Shows a one-time-per-session UAC explainer, re-launches this exe elevated to apply one
    /// tweak, and reverts the optimistic UI update (re-notifying the given properties) on cancel or
    /// failure. On success, re-reads every tweak from the registry rather than trusting the
    /// optimistic value.
    /// </summary>
    private async Task ApplyElevatedTweakAsync(string tweakKey, bool enabled, Action revert, params string[] propertyNames)
    {
        if (!_hasShownElevationNotice)
        {
            _hasShownElevationNotice = true;
            bool proceed = await _dialogs.ShowConfirmAsync(Localization,
                Localization["ElevationRequiredTitle"], Localization["ElevationRequiredMessage"],
                Localization["ElevationRequiredConfirmLabel"], isDestructive: false);

            if (!proceed)
            {
                Revert(revert, propertyNames);
                return;
            }
        }

        ElevatedRunResult result = await Task.Run(() =>
            ElevationHelper.RunElevated("--apply-network-tweak", tweakKey, enabled ? "1" : "0"));

        if (result == ElevatedRunResult.Success)
            RefreshTweaks();
        else
            Revert(revert, propertyNames);
    }

    /// <summary>Reverts an optimistic UI update and re-notifies the given properties.</summary>
    private void Revert(Action revert, string[] propertyNames)
    {
        revert();
        foreach (string propertyName in propertyNames)
            OnPropertyChanged(propertyName);
    }

    /// <summary>Formats a link speed in bits/second as e.g. "1 Gbps" or "100 Mbps".</summary>
    private static string FormatLinkSpeed(long bitsPerSecond)
    {
        if (bitsPerSecond <= 0)
            return NotAvailable;

        double mbps = bitsPerSecond / 1_000_000.0;
        return mbps >= 1000 ? $"{mbps / 1000.0:0.#} Gbps" : $"{mbps:0} Mbps";
    }

    /// <summary>
    /// Formats a throughput rate in bytes/second, honouring the user's unit preference: either
    /// "1.2 MB/s" / "340 KB/s" (bytes, binary multiples) or "9.6 Mbps" / "340 Kbps" (bits, decimal
    /// multiples - the convention network hardware is advertised in).
    /// </summary>
    private string FormatThroughput(double bytesPerSecond)
    {
        if (_settings.Current.ThroughputUnit == ThroughputUnit.Bits)
        {
            double kbps = bytesPerSecond * 8 / 1000.0;
            return kbps >= 1000 ? $"{kbps / 1000.0:0.#} Mbps" : $"{kbps:0} Kbps";
        }

        double kbPerSecond = bytesPerSecond / 1024.0;
        return kbPerSecond >= 1024 ? $"{kbPerSecond / 1024.0:0.#} MB/s" : $"{kbPerSecond:0} KB/s";
    }
}