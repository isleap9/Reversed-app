using VainTools.App.Models;

namespace VainTools.App.Services;

/// <summary>Why a <c>.vain</c> file was rejected.</summary>
public enum VainProfileError
{
    None = 0,
    NotAVainFile,
    UnreadableFile,
    MalformedXml,
    WrongRootElement,
    NoProfiles,
    NoImportableSettings,
}

/// <summary>
/// Outcome of parsing a <c>.vain</c> file. Exactly one of
/// <see cref="Document"/> / <see cref="Error"/> is meaningful.
/// </summary>
public sealed record VainProfileParseResult
{
    public VainProfileDocument? Document { get; init; }

    public VainProfileError Error { get; init; } = VainProfileError.None;

    /// <summary>File name the result came from, for display.</summary>
    public string SourceName { get; init; } = string.Empty;

    public bool Success => Error == VainProfileError.None && Document is not null;

    public static VainProfileParseResult Ok(VainProfileDocument document, string sourceName) =>
        new() { Document = document, SourceName = sourceName };

    public static VainProfileParseResult Fail(VainProfileError error, string sourceName) =>
        new() { Error = error, SourceName = sourceName };

    /// <summary>A user-facing explanation of the failure.</summary>
    public string ErrorMessage => Error switch
    {
        VainProfileError.None => string.Empty,
        VainProfileError.NotAVainFile => "That file is not a .vain profile.",
        VainProfileError.UnreadableFile => "Could not read that file.",
        VainProfileError.MalformedXml => "That file is not a valid Vain Toolbox profile.",
        VainProfileError.WrongRootElement => "That file is not a Vain Toolbox profile.",
        VainProfileError.NoProfiles => "That profile did not contain any profiles.",
        VainProfileError.NoImportableSettings =>
            "That profile did not contain any importable DWORD settings.",
        _ => "That profile could not be imported.",
    };
}
