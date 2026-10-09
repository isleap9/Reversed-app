using Microsoft.Extensions.Logging;

namespace VainTools.App.Services;

/// <summary>
/// Implements <see cref="IOptionalFeaturesService"/> through <c>dism.exe</c> behind
/// <see cref="IProcessRunner"/> (the same pattern <see cref="StartupService"/> uses for
/// <c>schtasks.exe</c>).
///
/// Why dism.exe instead of raw <c>DismApi.dll</c> P/Invoke: the DISM API surface needed
/// here (session open, feature enumeration with progress callbacks, capability structs)
/// is large, version-sensitive, and untestable without the real API — while
/// <c>dism.exe /Online /Get-Features /Format:List</c> exposes exactly the Name/State
/// pairs this page needs, and stays fully mockable through <see cref="IProcessRunner"/>.
/// This is the fallback the plan explicitly allows; the choice is recorded here rather
/// than scattered across call sites.
///
/// Exceptions (including nonzero DISM exits) are deliberately NOT swallowed — they
/// propagate to the view model, which reports them through the error InfoBar.
/// </summary>
public sealed class OptionalFeaturesService : IOptionalFeaturesService
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<OptionalFeaturesService> _logger;

    public OptionalFeaturesService(IProcessRunner processRunner, ILogger<OptionalFeaturesService> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OptionalFeature>> GetFeaturesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _processRunner.RunAsync("dism.exe", "/Online /Get-Features /Format:List");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dism.exe /Get-Features failed (exit {result.ExitCode}). {result.StdErr.Trim()}");
        }

        var features = ParseFeatures(result.StdOut);
        _logger.LogInformation("Enumerated {Count} optional features", features.Count);
        return features;
    }

    /// <inheritdoc />
    public async Task EnableFeatureAsync(string featureName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);

        var result = await _processRunner.RunAsync(
            "dism.exe", $"/Online /Enable-Feature /FeatureName:\"{featureName}\" /NoRestart");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dism.exe failed to enable \"{featureName}\" (exit {result.ExitCode}). {result.StdErr.Trim()}");
        }

        _logger.LogInformation("Enabled optional feature {Feature}", featureName);
    }

    /// <inheritdoc />
    public async Task DisableFeatureAsync(string featureName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);

        var result = await _processRunner.RunAsync(
            "dism.exe", $"/Online /Disable-Feature /FeatureName:\"{featureName}\" /NoRestart");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dism.exe failed to disable \"{featureName}\" (exit {result.ExitCode}). {result.StdErr.Trim()}");
        }

        _logger.LogInformation("Disabled optional feature {Feature}", featureName);
    }

    /// <summary>
    /// Parses <c>dism /Online /Get-Features /Format:List</c> output into Name/State pairs.
    /// Blocks look like:
    /// <code>
    /// Feature Name : Microsoft-Windows-Subsystem-Linux
    /// State : Disabled
    /// </code>
    /// Unparseable lines are skipped; a block only yields a feature when both fields are present.
    /// </summary>
    public static IReadOnlyList<OptionalFeature> ParseFeatures(string stdout)
    {
        var features = new List<OptionalFeature>();
        string? pendingName = null;

        foreach (var line in stdout.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r').Trim();
            if (trimmed.Length == 0)
            {
                pendingName = null;
                continue;
            }

            var colon = trimmed.IndexOf(':');
            if (colon < 0)
            {
                continue;
            }

            var field = trimmed[..colon].Trim();
            var value = trimmed[(colon + 1)..].Trim();

            if (field.Equals("Feature Name", StringComparison.OrdinalIgnoreCase))
            {
                pendingName = value.Length > 0 ? value : null;
            }
            else if (field.Equals("State", StringComparison.OrdinalIgnoreCase) && pendingName is not null)
            {
                features.Add(new OptionalFeature(pendingName, value));
                pendingName = null;
            }
        }

        return features;
    }
}
