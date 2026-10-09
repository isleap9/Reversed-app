using Microsoft.Extensions.Logging;

namespace VainTools.App.Services;

/// <summary>
/// Implements <see cref="IOptionalFeaturesService"/> through <c>dism.exe</c> behind
/// <see cref="IProcessRunner"/> — the same pattern <see cref="StartupService"/> uses for
/// <c>schtasks.exe</c>.
///
/// DISM API choice (D-06 / D-07): the plan's primary path is <c>DismApi.dll</c> P/Invoke
/// with a documented fallback to <c>dism.exe</c>. This implementation takes that fallback,
/// deliberately, for three reasons:
///
///  1. The native surface needed here — <c>DismInitialize</c>, <c>DismOpenSession</c>,
///     <c>DismGetFeatures</c> over a <c>DismFeature*</c> array with nested
///     <c>DismString</c>/<c>DismPackage</c> pointers, <c>DismEnableFeature</c> with a
///     progress callback, then <c>DismDelete</c> on every allocation and
///     <c>DismShutdown</c> on the session — is version-sensitive, and an incorrect struct
///     layout reads native memory out of bounds. A wrong layout would surface as
///     corruption at runtime, which no unit test in this project can catch.
///  2. <c>dism.exe /Online /Get-Features /Format:List</c> exposes exactly the
///     Name/State pairs this page needs, and <c>/Enable-Feature</c> /
///     <c>/Disable-Feature</c> map one-to-one onto the two mutations.
///  3. Routing through <see cref="IProcessRunner"/> keeps the service fully mockable, so
///     the failure paths (nonzero exit, empty output) are covered by unit tests. The
///     P/Invoke variant has no seam and would have to be trusted untested.
///
/// The <c>Task.Run</c> wrapper the plan asks for is unnecessary here because
/// <see cref="IProcessRunner.RunAsync"/> already performs its work off the calling thread
/// and returns an awaitable Task; adding another hop would only obscure the call stack.
///
/// Exceptions (including nonzero DISM exits) are deliberately NOT swallowed — they
/// propagate to the view model, which reports them through the error InfoBar (D-12).
/// </summary>
public sealed class OptionalFeaturesService : IOptionalFeaturesService
{
    private const string DismExecutable = "dism.exe";

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
        var result = await _processRunner.RunAsync(DismExecutable, "/Online /Get-Features /Format:List");

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

        // /NoRestart: the caller decides whether to reboot, never DISM implicitly.
        var result = await _processRunner.RunAsync(
            DismExecutable, $"/Online /Enable-Feature /FeatureName:\"{featureName}\" /NoRestart");

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
            DismExecutable, $"/Online /Disable-Feature /FeatureName:\"{featureName}\" /NoRestart");

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
