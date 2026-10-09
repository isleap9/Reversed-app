namespace VainTools.App.Services;

/// <summary>
/// A resolved uninstall command: the executable to start and the argument text to hand
/// it, exactly as it appeared in the registry.
/// </summary>
public sealed record UninstallLaunch(string FileName, string Arguments);

/// <summary>
/// Splits a registry uninstall command line into the executable and its arguments.
///
/// <para>
/// Only the executable token is split off. The remaining argument text is passed through
/// byte-for-byte — never re-quoted, unescaped or split further. This mirrors what Windows
/// itself does when it creates a process, and it matters because uninstallers such as
/// MsiExec and the NSIS family parse their own raw command line: re-quoting an element
/// changes what they receive.
/// </para>
///
/// <para>
/// Wrapping the command in a command interpreter was considered and rejected. An
/// interpreter expands <c>&amp;</c>, <c>|</c>, <c>^</c> and <c>%VAR%</c> inside a string read
/// from a hive any non-elevated process of the same user can write, and the uninstaller
/// inherits administrator rights. Handing the whole string to the shell has the same
/// exposure plus its own quoting rules.
/// </para>
/// </summary>
public static class UninstallCommandLine
{
    /// <summary>
    /// Splits <paramref name="commandLine"/> using the real file system for existence
     /// probes and the real system directory for bare-name resolution.
    /// </summary>
    public static UninstallLaunch Parse(string commandLine)
        => Parse(commandLine, File.Exists, Environment.SystemDirectory);

    /// <summary>
    /// Testable overload: existence probes and the system directory are injected so unit
    /// tests can exercise the probing rules without touching the real file system.
    /// </summary>
    public static UninstallLaunch Parse(string commandLine, Func<string, bool> fileExists, string systemDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandLine);

        var text = commandLine.Trim();

        // A quoted first token: the file name is the text between the quotes.
        if (text.StartsWith('"'))
        {
            var closing = text.IndexOf('"', 1);
            if (closing < 0)
            {
                // Unterminated quote: treat the remainder as the file name, no arguments.
                return new UninstallLaunch(text[1..], string.Empty);
            }

            var quoted = text[1..closing];
            if (quoted.Length == 0)
            {
                throw new ArgumentException(
                    "The uninstall command names an empty program.", nameof(commandLine));
            }

            return new UninstallLaunch(
                ResolveBareName(quoted, fileExists, systemDirectory),
                text[(closing + 1)..].Trim());
        }

        var (fileName, arguments) = SplitUnquoted(text, fileExists);
        return new UninstallLaunch(ResolveBareName(fileName, fileExists, systemDirectory), arguments);
    }

    /// <summary>
    /// Splits an unquoted command line by trying the shortest space-delimited prefix that
    /// names an existing file first — the order CreateProcess uses — so a path containing
    /// spaces resolves in full rather than at its first gap. Falls back to the plain
    /// first-token split when nothing on disk matches.
    /// </summary>
    private static (string FileName, string Arguments) SplitUnquoted(string text, Func<string, bool> fileExists)
    {
        foreach (var index in SeparatorIndexes(text))
        {
            var candidate = text[..index];
            if (TryResolvePrefix(candidate, fileExists, out var resolved))
            {
                return (resolved, text[index..].Trim());
            }
        }

        return TryResolvePrefix(text, fileExists, out var whole)
            ? (whole, string.Empty)
            : SplitFirstToken(text);
    }

    /// <summary>Every space or tab position in <paramref name="text"/>, shortest prefix first.</summary>
    private static IEnumerable<int> SeparatorIndexes(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is ' ' or '\t')
            {
                yield return i;
            }
        }
    }

    /// <summary>
    /// A candidate only counts when it carries a directory separator and names a file on
    /// disk (or does once <c>.exe</c> is appended, which is what CreateProcess does).
    /// </summary>
    private static bool TryResolvePrefix(string candidate, Func<string, bool> fileExists, out string resolved)
    {
        if (candidate.Contains('\\') || candidate.Contains('/'))
        {
            if (fileExists(candidate))
            {
                resolved = candidate;
                return true;
            }

            if (!Path.HasExtension(candidate) && fileExists(candidate + ".exe"))
            {
                resolved = candidate + ".exe";
                return true;
            }
        }

        resolved = candidate;
        return false;
    }

    /// <summary>
    /// Resolves a bare executable name (no directory separator) to its copy in
    /// <paramref name="systemDirectory"/>, so a same-named file planted beside the elevated
    /// app is never the one that runs.
    /// </summary>
    private static string ResolveBareName(string fileName, Func<string, bool> fileExists, string systemDirectory)
    {
        if (fileName.Length == 0 || fileName.Contains('\\') || fileName.Contains('/'))
        {
            return fileName;
        }

        var candidate = Path.Combine(systemDirectory, fileName);
        if (fileExists(candidate))
        {
            return candidate;
        }

        if (!Path.HasExtension(fileName) && fileExists(candidate + ".exe"))
        {
            return candidate + ".exe";
        }

        return fileName;
    }

    /// <summary>Plain first-whitespace-token split, with the remainder as the arguments.</summary>
    private static (string FileName, string Arguments) SplitFirstToken(string text)
    {
        var index = text.AsSpan().IndexOfAny(' ', '\t');
        return index < 0
            ? (text, string.Empty)
            : (text[..index], text[index..].Trim());
    }
}
