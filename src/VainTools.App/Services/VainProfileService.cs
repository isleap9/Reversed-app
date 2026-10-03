using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using VainTools.App.Models;
using VainTools.Framework.Services;

namespace VainTools.App.Services;

/// <summary>
/// Reads and writes Vain Toolbox <c>.vain</c> profile files.
///
/// The format is XML with an <c>ArrayOfProfile</c> root, written as UTF-16 by the
/// real application. This implementation also accepts UTF-8 so hand-edited files
/// still import.
/// </summary>
public sealed class VainProfileService : IVainProfileService
{
    /// <summary>Settings key under which imported profiles are persisted.</summary>
    private const string ImportedProfilesKey = "Vain.ImportedProfiles";

    private static readonly XmlSerializer Serializer = new(typeof(VainProfileDocument));

    private readonly ISettingsService _settings;
    private readonly ILogger<VainProfileService> _logger;

    public VainProfileService(ISettingsService settings, ILogger<VainProfileService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public VainProfileParseResult Parse(Stream stream, string sourceName)
    {
        if (!sourceName.EndsWith(".vain", StringComparison.OrdinalIgnoreCase))
        {
            return VainProfileParseResult.Fail(VainProfileError.NotAVainFile, sourceName);
        }

        try
        {
            // Buffer the stream so the encoding can be sniffed and the root element
            // inspected before deserialising.
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);

            string text;
            try
            {
                text = DecodeToText(buffer.ToArray());
            }
            catch (DecoderFallbackException ex)
            {
                _logger.LogDebug(ex, "Undecodable .vain content in {Source}", sourceName);
                return VainProfileParseResult.Fail(VainProfileError.MalformedXml, sourceName);
            }

            XDocument xml;
            try
            {
                xml = XDocument.Parse(text, LoadOptions.None);
            }
            catch (XmlException ex)
            {
                _logger.LogDebug(ex, "Malformed .vain XML in {Source}", sourceName);
                return VainProfileParseResult.Fail(VainProfileError.MalformedXml, sourceName);
            }

            if (!string.Equals(xml.Root?.Name.LocalName, "ArrayOfProfile", StringComparison.Ordinal))
            {
                return VainProfileParseResult.Fail(VainProfileError.WrongRootElement, sourceName);
            }

            VainProfileDocument? document;
            try
            {
                using var reread = new StringReader(text);
                document = (VainProfileDocument?)Serializer.Deserialize(reread);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogDebug(ex, "Could not deserialise {Source}", sourceName);
                return VainProfileParseResult.Fail(VainProfileError.MalformedXml, sourceName);
            }

            if (document is null || document.Profiles.Count == 0)
            {
                return VainProfileParseResult.Fail(VainProfileError.NoProfiles, sourceName);
            }

            if (document.DwordSettingCount == 0)
            {
                return VainProfileParseResult.Fail(VainProfileError.NoImportableSettings, sourceName);
            }

            return VainProfileParseResult.Ok(document, sourceName);
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "Could not read {Source}", sourceName);
            return VainProfileParseResult.Fail(VainProfileError.UnreadableFile, sourceName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected failure parsing {Source}", sourceName);
            return VainProfileParseResult.Fail(VainProfileError.MalformedXml, sourceName);
        }
    }

    /// <inheritdoc />
    public async Task<VainProfileParseResult> ParseFileAsync(string path)
    {
        var name = Path.GetFileName(path);

        try
        {
            await using var stream = File.OpenRead(path);
            return Parse(stream, name);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not open {Path}", path);
            return VainProfileParseResult.Fail(VainProfileError.UnreadableFile, name);
        }
    }

    /// <summary>
    /// Decodes <c>.vain</c> bytes to text.
    ///
    /// The real application writes UTF-16 with a BOM, but files are also seen without
    /// a BOM and as UTF-8 (a hand-edited copy). The byte-order-mark is preferred; when
    /// absent, a NUL byte in the first two positions means UTF-16 little-endian, and
    /// anything else is treated as UTF-8.
    /// </summary>
    private static string DecodeToText(byte[] bytes)
    {
        if (bytes.Length >= 2)
        {
            if (bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return new UnicodeEncoding(bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: true)
                    .GetString(bytes, 2, bytes.Length - 2);
            }

            if (bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return new UnicodeEncoding(bigEndian: true, byteOrderMark: true, throwOnInvalidBytes: true)
                    .GetString(bytes, 2, bytes.Length - 2);
            }

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true)
                    .GetString(bytes, 3, bytes.Length - 3);
            }

            // No BOM: a NUL in the first code unit means UTF-16LE.
            if (bytes[0] == 0x00 || bytes[1] == 0x00)
            {
                return new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true)
                    .GetString(bytes);
            }
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
    }

    /// <inheritdoc />
    public async Task WriteFileAsync(string path, VainProfileDocument document)
    {
        // UTF-16 to match the real application's output.
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
            Async = true,
        };

        await using var stream = File.Create(path);
        await using var writer = XmlWriter.Create(stream, settings);
        Serializer.Serialize(writer, document);
        await writer.FlushAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VainProfile>> GetImportedProfilesAsync()
    {
        var stored = await _settings.GetAsync<List<VainProfile>>(ImportedProfilesKey);
        return stored ?? [];
    }

    /// <inheritdoc />
    public async Task SaveImportedProfilesAsync(IEnumerable<VainProfile> profiles)
    {
        await _settings.SetAsync(ImportedProfilesKey, profiles.ToList());
    }
}
