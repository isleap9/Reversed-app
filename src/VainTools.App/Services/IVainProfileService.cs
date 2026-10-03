using VainTools.App.Models;

namespace VainTools.App.Services;

/// <summary>
/// Reads and writes Vain Toolbox <c>.vain</c> profile files.
///
/// Parsing is deliberately tolerant: a bad file yields a
/// <see cref="VainProfileParseResult"/> describing the problem and never throws.
/// </summary>
public interface IVainProfileService
{
    /// <summary>Parses <c>.vain</c> content from a stream.</summary>
    VainProfileParseResult Parse(Stream stream, string sourceName);

    /// <summary>Parses a <c>.vain</c> file from disk.</summary>
    Task<VainProfileParseResult> ParseFileAsync(string path);

    /// <summary>Serialises profiles back to <c>.vain</c> format (UTF-16, as the real app writes).</summary>
    Task WriteFileAsync(string path, VainProfileDocument document);

    /// <summary>Imported profiles, persisted across restarts.</summary>
    Task<IReadOnlyList<VainProfile>> GetImportedProfilesAsync();

    /// <summary>Stores imported profiles for later application.</summary>
    Task SaveImportedProfilesAsync(IEnumerable<VainProfile> profiles);
}
