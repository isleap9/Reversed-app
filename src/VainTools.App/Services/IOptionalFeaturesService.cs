namespace VainTools.App.Services;

/// <summary>
/// Lists and toggles Windows optional features via DISM.
/// </summary>
public interface IOptionalFeaturesService
{
    /// <summary>Lists optional features and their states.</summary>
    Task<IReadOnlyList<OptionalFeature>> GetFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>Enables a feature. Requires elevation.</summary>
    Task EnableFeatureAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>Disables a feature. Requires elevation. Irreversible-ish: the caller must confirm first.</summary>
    Task DisableFeatureAsync(string featureName, CancellationToken cancellationToken = default);
}
