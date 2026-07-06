using System.Threading.Tasks;
using System.Threading;

namespace RustOptimizer.Interface;

/// <summary>
/// Describes an available update: the version being offered, and the release asset to download.
/// </summary>
public sealed record UpdateInfo(string Version, string DownloadUrl, string AssetName);

/// <summary>
/// Checks GitHub Releases for a newer version and applies it in place.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Queries the latest GitHub release and returns update info if it's newer than the running version,
    /// or <see langword="null"/> if already up to date.
    /// </summary>
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default);

    /// <summary>
    /// Downloads the release asset for <paramref name="update"/>, extracts it, and swaps it in over the
    /// current install once the app exits. Terminates the current process on success.
    /// </summary>
    Task ApplyUpdateAsync(UpdateInfo update, CancellationToken ct = default);
}
