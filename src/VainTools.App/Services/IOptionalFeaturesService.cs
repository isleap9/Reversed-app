namespace VainTools.App.Services;

/// <summary>
/// Lists and toggles Windows optional features via DISM.
/// </summary>
public interface IOptionalFeaturesService
{
    /// <summary>Lists optional features and their states.</summary>
    Task<IReadOnlyList<OptionalFeature>> GetFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a feature. Requires elevation. Exit 3010 is success with
    /// <see cref="FeatureChangeResult.RestartRequired"/> true.
    /// </summary>
    Task<FeatureChangeResult> EnableFeatureAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a feature. Requires elevation. Irreversible-ish: the caller must confirm first.
    /// Exit 3010 is success with <see cref="FeatureChangeResult.RestartRequired"/> true.
    /// </summary>
    Task<FeatureChangeResult> DisableFeatureAsync(string featureName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a DISM feature change. Exit 0 is success without restart;
/// exit 3010 (ERROR_SUCCESS_REBOOT_REQUIRED) is success with restart required.
/// </summary>
public sealed record FeatureChangeResult(bool RestartRequired);
