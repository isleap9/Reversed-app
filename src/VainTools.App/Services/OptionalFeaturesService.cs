using System.Text.RegularExpressions;
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
    /// <summary>
    /// Absolute System32 path so the application directory is never searched (T-06-21).
    /// </summary>
    public static readonly string DismPath = Path.Combine(Environment.SystemDirectory, "dism.exe");

    /// <summary>ERROR_SUCCESS_REBOOT_REQUIRED: DISM applied the change; a restart finishes it.</summary>
    public const int ExitRestartRequired = 3010;

    private static readonly Regex ValidFeatureNamePattern =
        new("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

    /// <summary>
    /// True when <paramref name="name"/> contains only characters DISM feature names
    /// use (letters, digits, '.', '_' and '-'). Defence in depth (T-06-20): the name
    /// travels as one argv element, and this check refuses anything else before launch.
    /// </summary>
    public static bool IsValidFeatureName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && ValidFeatureNamePattern.IsMatch(name);

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
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _processRunner.RunAsync(DismPath, "/Online", "/Get-Features", "/Format:List");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dism.exe /Get-Features failed (exit {result.ExitCode}). {result.StdErr.Trim()}");
        }

        var features = ParseFeatures(result.StdOut);
        if (features.Count == 0 && result.StdOut.Trim().Length > 0)
        {
            // A successful run whose output parses to nothing (localized DISM text,
            // for example) must surface as a load failure, never as "No features".
            throw new InvalidOperationException(
                "dism.exe returned feature output that could not be read. The output may be in a language other than English.");
        }

        _logger.LogInformation("Enumerated {Count} optional features", features.Count);
        return features;
    }

    /// <inheritdoc />
    public Task<FeatureChangeResult> EnableFeatureAsync(string featureName, CancellationToken cancellationToken = default) =>
        ChangeFeatureAsync("/Enable-Feature", "enable", featureName, cancellationToken);

    /// <inheritdoc />
    public Task<FeatureChangeResult> DisableFeatureAsync(string featureName, CancellationToken cancellationToken = default) =>
        ChangeFeatureAsync("/Disable-Feature", "disable", featureName, cancellationToken);

    private async Task<FeatureChangeResult> ChangeFeatureAsync(
        string verbSwitch, string verb, string featureName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
        if (!IsValidFeatureName(featureName))
        {
            throw new ArgumentException(
                $"'{featureName}' is not a valid Windows feature name.", nameof(featureName));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // /NoRestart: the caller decides whether to reboot, never DISM implicitly.
        // Two or more argv elements bind to the argument-vector overload (WR-03).
        var result = await _processRunner.RunAsync(
            DismPath, "/Online", verbSwitch, $"/FeatureName:{featureName}", "/NoRestart");

        if (result.ExitCode == 0)
        {
            _logger.LogInformation("{Verb}d optional feature {Feature}", verb, featureName);
            return new FeatureChangeResult(false);
        }

        if (result.ExitCode == ExitRestartRequired)
        {
            _logger.LogInformation("{Verb}d optional feature {Feature}; restart required", verb, featureName);
            return new FeatureChangeResult(true);
        }

        var detail = result.StdErr.Trim();
        if (detail.Length == 0)
        {
            // DISM writes its errors to stdout.
            detail = result.StdOut.Trim();
        }

        throw new InvalidOperationException(
            $"dism.exe failed to {verb} '{featureName}' (exit {result.ExitCode}). {detail}");
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
