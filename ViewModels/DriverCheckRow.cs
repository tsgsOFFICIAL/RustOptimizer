namespace RustOptimizer.ViewModels;

/// <summary>
/// One row in the "Update Drivers" prompt: a component's label (e.g. "GPU"), what was detected for
/// it, and a link to its vendor's driver page when one is known.
/// </summary>
public sealed record DriverCheckRow(string Label, string Detected, string? LinkUrl)
{
    /// <summary>Whether <see cref="LinkUrl"/> resolved to a known vendor page - drives the row's Check button.</summary>
    public bool HasLink => LinkUrl is not null;
}