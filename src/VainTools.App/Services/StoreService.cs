using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace VainTools.App.Services;

/// <summary>
/// Searches for and installs applications through the <c>winget</c> CLI (D-08).
///
/// Design notes:
///
///  * <b>No shell strings.</b> The search query and the package id are always passed
///    as single elements of the argument vector through
///    <see cref="IProcessRunner.RunAsync(string, string[])"/>, so untrusted input can
///    never break out of its argument and inject extra switches (T-06-09).
///  * <b>Search failures are reported, not thrown.</b> A nonzero winget exit,
///    unparseable rows or an empty result all return an empty list and log a warning,
///    so the page shows its own user-facing copy instead of a raw CLI error (D-12 /
///    T-06-14). Process-layer exceptions are not caught.
///  * <b>Install returns the raw result.</b> <see cref="InstallAppAsync"/> hands back
///    the <see cref="ProcessResult"/> — including exit code and stderr — and lets the
///    caller decide what a failure means for the user.
///  * <b>No JSON mode.</b> The plan names <c>--format json</c> as a winget 1.4+
///    option; winget has no such flag for <c>search</c> (v1.29.380 rejects both
///    <c>--output</c> and <c>--format</c>), so the table parser below is the only
///    path. It is written against real winget output captured on this machine.
///  * <b>Sync search.</b> <see cref="SearchApps"/> blocks on the process run so the
///    contract stays the one the plan specifies; callers offload it with
///    <c>Task.Run</c> (see <c>StoreViewModel</c>) so the UI thread is never blocked.
/// </summary>
public sealed class StoreService : IStoreService
{
    private const string WingetExecutable = "winget";

    /// <summary>A token winget would accept as a package id: no spaces, one or more dots, at least one letter.</summary>
    private static readonly Regex PackageIdToken = new(@"^[A-Za-z0-9][A-Za-z0-9._'+-]*$", RegexOptions.Compiled);

    /// <summary>A version column value: digits and dots, optionally v-prefixed or pre-release suffixed.</summary>
    private static readonly Regex VersionToken = new(@"^v?\d+(\.\d+){1,}(-[A-Za-z0-9.]+)?$", RegexOptions.Compiled);

    private readonly IProcessRunner _processRunner;
    private readonly ILogger<StoreService> _logger;

    public StoreService(IProcessRunner processRunner, ILogger<StoreService> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<StoreApp> SearchApps(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var result = _processRunner
            .RunAsync(WingetExecutable, "search", query, "--accept-source-agreements")
            .GetAwaiter()
            .GetResult();

        if (result.ExitCode != 0)
        {
            _logger.LogWarning(
                "winget search for \"{Query}\" failed (exit {ExitCode}). {StdErr}",
                query, result.ExitCode, result.StdErr);
            return [];
        }

        var apps = ParseSearch(result.StdOut);
        _logger.LogInformation("winget search for \"{Query}\" returned {Count} app(s)", query, apps.Count);
        return apps;
    }

    /// <inheritdoc />
    public async Task<ProcessResult> InstallAppAsync(string appId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);

        var result = await _processRunner.RunAsync(
            WingetExecutable,
            "install", "--id", appId, "--exact", "--accept-source-agreements", "--accept-package-agreements");

        if (result.ExitCode == 0)
        {
            _logger.LogInformation("winget installed {AppId}", appId);
        }
        else
        {
            _logger.LogWarning(
                "winget install of {AppId} failed (exit {ExitCode}). {StdErr}",
                appId, result.ExitCode, result.StdErr);
        }

        return result;
    }

    /// <summary>
    /// Parses <c>winget search</c> table output into <see cref="StoreApp"/> records.
    ///
    /// <para>
    /// winget prints a header (<c>Name  Id  Version  [Match]  Source</c>) and then one
    /// row per package. The column separator is <b>not</b> a fixed run of spaces —
    /// real output on this machine separates adjacent short values with a single space
    /// (<c>7-Zip 7zip.7zip 26.04   winget</c>), so splitting on runs of two-or-more
    /// whitespace is unsafe. The character offsets of the header labels are used as the
    /// column regions instead, and winget keeps those offsets stable across every row
    /// of a single run.
    /// </para>
    /// <para>
    /// Parsing is deliberately defensive: blank lines, the <c>---</c> rule and
    /// progress-bar noise are skipped, a row with no usable id is dropped rather than
    /// throwing, and an empty or header-only output yields an empty list.
    /// </para>
    /// </summary>
    public static IReadOnlyList<StoreApp> ParseSearch(string? stdout)
    {
        var apps = new List<StoreApp>();
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return apps;
        }

        int[]? columns = null;

        foreach (var rawLine in stdout.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            // Blank lines, the "-----" rule and progress-bar noise carry no data.
            if (!line.Any(char.IsLetterOrDigit))
            {
                continue;
            }

            if (columns is null && TryReadHeader(line, out columns))
            {
                continue;
            }

            if (line.TrimStart().StartsWith("---", StringComparison.Ordinal))
            {
                continue;
            }

            var app = MapRow(line, columns);
            if (app is not null)
            {
                apps.Add(app);
            }
        }

        return apps;
    }

    /// <summary>
    /// Reads the character offset of every column label in the winget header row.
    /// <c>Match</c> only exists on newer winget builds, so it is optional.
    /// </summary>
    private static bool TryReadHeader(string line, out int[]? columns)
    {
        columns = null;

        var trimmed = line.Trim();
        if (!trimmed.StartsWith("Name", StringComparison.OrdinalIgnoreCase) ||
            !trimmed.Contains("Id", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nameAt = line.IndexOf("Name", StringComparison.OrdinalIgnoreCase);
        if (nameAt < 0)
        {
            return false;
        }

        var offsets = new List<int>(5) { nameAt };
        foreach (var label in new[] { "Id", "Version", "Match", "Source" })
        {
            var at = line.IndexOf(label, offsets[^1] + label.Length, StringComparison.OrdinalIgnoreCase);
            if (at >= 0)
            {
                offsets.Add(at);
            }
        }

        // Name, Id and Version are the minimum the page can render.
        if (offsets.Count < 4)
        {
            return false;
        }

        columns = offsets.ToArray();
        return true;
    }

    /// <summary>
    /// Maps one data row. The header regions are tried first; a row whose id does not
    /// land inside the id region falls back to shape-based token reconstruction.
    /// </summary>
    private static StoreApp? MapRow(string line, int[]? columns)
    {
        var row = line.TrimEnd();

        if (columns is { Length: >= 4 })
        {
            // One [start, end) region per header column.
            var regions = new (int Start, int End)[columns.Length];
            for (var i = 0; i < columns.Length; i++)
            {
                var start = Math.Min(columns[i], row.Length);
                var end = i + 1 < columns.Length ? Math.Min(columns[i + 1], row.Length) : row.Length;
                regions[i] = (start, end);
            }

            // A value longer than its header region is cut mid-token. Detect that by
            // measuring the value's own token inside the id region: if the token runs
            // past the region's end, it overruns. (A value that merely fills its region
            // exactly, so the next column starts right at the boundary, is NOT an
            // overflow — that is the normal case and must not be extended.)
            var idStart = regions[1].Start;

            // Skip the region's left padding to the first non-whitespace character.
            var idTokenStart = idStart;
            while (idTokenStart < regions[1].End && char.IsWhiteSpace(row[idTokenStart]))
            {
                idTokenStart++;
            }

            var idTokenEnd = idTokenStart;
            while (idTokenEnd < row.Length && !char.IsWhiteSpace(row[idTokenEnd]))
            {
                idTokenEnd++;
            }

            if (idTokenEnd > regions[1].End)
            {
                regions[1] = (idStart, Math.Min(idTokenEnd, row.Length));

                if (regions[2].Start < idTokenEnd)
                {
                    regions[2] = (idTokenEnd, Math.Max(idTokenEnd, regions[2].End));
                }
            }

            var id = Slice(row, regions[1]);
            if (LooksLikePackageId(id))
            {
                return new StoreApp(
                    Slice(row, regions[0]) is { Length: > 0 } name ? name : id,
                    id,
                    Slice(row, regions[2]),
                    Slice(row, regions[^1]));
            }
        }

        return MapRowByShape(row);
    }

    /// <summary>Returns the trimmed text of one region of <paramref name="row"/>.</summary>
    private static string Slice(string row, (int Start, int End) region)
    {
        if (region.End <= region.Start)
        {
            return string.Empty;
        }

        return row[region.Start..region.End].Trim();
    }

    /// <summary>
    /// Rebuilds a row from its whitespace-separated tokens, anchored on the package id.
    /// Only the name may contain spaces, so everything before the id is the name and
    /// everything after it is version / match / source.
    /// </summary>
    private static StoreApp? MapRowByShape(string row)
    {
        var tokens = row
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (tokens.Count < 2)
        {
            return null;
        }

        var idIndex = tokens.FindIndex(1, LooksLikePackageId);
        if (idIndex < 0)
        {
            return null;
        }

        var id = tokens[idIndex];
        var name = string.Join(' ', tokens.Take(idIndex));
        var tail = tokens.GetRange(idIndex + 1, tokens.Count - idIndex - 1);

        var versionIndex = tail.FindIndex(VersionToken.IsMatch);
        var version = versionIndex >= 0 ? tail[versionIndex] : string.Empty;
        var source = tail.Count > versionIndex + 1 ? tail[^1] : string.Empty;

        return new StoreApp(name.Length > 0 ? name : id, id, version, source);
    }

    /// <summary>
    /// True when a token has the shape of a winget package id: either
    /// <c>Publisher.Product</c> — two or more dot-separated segments, none of them empty
    /// and none of them a pure version — or a single msstore-style alphanumeric token
    /// such as <c>XP9KHM4BK9FZ7Q</c>.
    /// <para>
    /// The strictness matters: a sentence fragment that merely ends in a full stop
    /// ("criteria.", "found.") otherwise reads as an id and produces phantom rows from
    /// footers like "1 application(s) found."
    /// </para>
    /// </summary>
    private static bool LooksLikePackageId(string token)
    {
        if (token.Length == 0 || !PackageIdToken.IsMatch(token))
        {
            return false;
        }

        if (!token.Contains('.'))
        {
            // msstore publishes single-token ids such as "XP9KHM4BK9FZ7Q". Requiring a
            // digit keeps plain English words ("matching", "tools") from qualifying.
            return token.Length >= 8 && token.Any(char.IsDigit) && token.All(char.IsLetterOrDigit);
        }

        if (VersionToken.IsMatch(token))
        {
            return false;
        }

        var segments = token.Split('.');
        return segments.Length >= 2 && segments.All(s => s.Length > 0);
    }
}
