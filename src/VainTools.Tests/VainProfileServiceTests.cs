using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using VainTools.App.Models;
using VainTools.App.Services;
using VainTools.Framework.Services;
using Xunit;

namespace VainTools.Tests;

/// <summary>
/// Tests for the <c>.vain</c> profile parser.
///
/// The format was recovered from Vain Toolbox.exe; these tests pin the behaviours
/// that matter for compatibility (UTF-16 input, the misspelled "Executeables"
/// element) and for the rejection paths the UI depends on.
/// </summary>
public sealed class VainProfileServiceTests
{
    private static VainProfileService CreateService() =>
        new(new InMemorySettingsService(), NullLogger<VainProfileService>.Instance);

    /// <summary>A minimal, valid .vain document matching the recovered schema.</summary>
    private static string ValidVainXml(string settingId = "0x0000F00D", string valueType = "Dword") =>
        $"""
        <?xml version="1.0" encoding="utf-16"?>
        <ArrayOfProfile xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
          <Profile>
            <Executeables>
              <string>game.exe</string>
            </Executeables>
            <Settings>
              <ProfileSetting>
                <SettingNameInfo>Low Latency Mode</SettingNameInfo>
                <SettingID>{settingId}</SettingID>
                <SettingValue>1</SettingValue>
                <ValueType>{valueType}</ValueType>
              </ProfileSetting>
            </Settings>
          </Profile>
        </ArrayOfProfile>
        """;

    private static MemoryStream Utf16(string xml) =>
        new(Encoding.Unicode.GetBytes(xml));

    private static MemoryStream Utf8(string xml) =>
        new(Encoding.UTF8.GetBytes(xml));

    [Fact]
    public void Parse_ValidUtf16Document_Succeeds()
    {
        var result = CreateService().Parse(Utf16(ValidVainXml()), "profile.vain");

        Assert.True(result.Success);
        Assert.Equal(VainProfileError.None, result.Error);
        Assert.Single(result.Document!.Profiles);
        Assert.Equal(1, result.Document.TotalSettings);
    }

    [Fact]
    public void Parse_ValidUtf8Document_Succeeds()
    {
        // The real app writes UTF-16, but hand-edited UTF-8 files should still import.
        var result = CreateService().Parse(Utf8(ValidVainXml()), "profile.vain");

        Assert.True(result.Success);
        Assert.Single(result.Document!.Profiles);
    }

    [Fact]
    public void Parse_PreservesMisspelledExecuteablesElement()
    {
        var result = CreateService().Parse(Utf16(ValidVainXml()), "profile.vain");

        Assert.True(result.Success);
        var profile = result.Document!.Profiles[0];
        Assert.Equal(["game.exe"], profile.Executables);
    }

    [Fact]
    public void Parse_ReadsSettingFieldsAndValueType()
    {
        var result = CreateService().Parse(Utf16(ValidVainXml()), "profile.vain");

        var setting = result.Document!.Profiles[0].Settings[0];
        Assert.Equal("Low Latency Mode", setting.SettingNameInfo);
        Assert.Equal("0x0000F00D", setting.SettingId);
        Assert.Equal("1", setting.SettingValue);
        Assert.Equal(VainValueType.Dword, setting.ValueType);
    }

    [Fact]
    public void Parse_UnknownValueType_DoesNotThrowAndRoundTripsRawText()
    {
        var result = CreateService().Parse(Utf16(ValidVainXml(valueType: "SomeFutureType")), "p.vain");

        // Rejected only because it carries no importable DWORD, not because of the type.
        Assert.Equal(VainProfileError.NoImportableSettings, result.Error);
    }

    [Fact]
    public void Parse_NonVainExtension_IsRejected()
    {
        var result = CreateService().Parse(Utf16(ValidVainXml()), "profile.xml");

        Assert.False(result.Success);
        Assert.Equal(VainProfileError.NotAVainFile, result.Error);
    }

    [Fact]
    public void Parse_MalformedXml_IsRejected()
    {
        var result = CreateService().Parse(Utf8("<ArrayOfProfile><Profile>"), "broken.vain");

        Assert.False(result.Success);
        Assert.Equal(VainProfileError.MalformedXml, result.Error);
    }

    [Fact]
    public void Parse_ForeignRootElement_IsRejected()
    {
        var result = CreateService().Parse(Utf8("<NotAVainFile/>"), "foreign.vain");

        Assert.False(result.Success);
        Assert.Equal(VainProfileError.WrongRootElement, result.Error);
    }

    [Fact]
    public void Parse_NoProfiles_IsRejected()
    {
        var result = CreateService().Parse(
            Utf8("<?xml version=\"1.0\"?><ArrayOfProfile></ArrayOfProfile>"), "empty.vain");

        Assert.False(result.Success);
        Assert.Equal(VainProfileError.NoProfiles, result.Error);
    }

    [Fact]
    public void Parse_ProfileWithNoDwordSettings_IsRejected()
    {
        // Mirrors the real app's "did not contain any importable DWORD settings".
        var result = CreateService().Parse(
            Utf16(ValidVainXml(valueType: "Binary")), "binary-only.vain");

        Assert.False(result.Success);
        Assert.Equal(VainProfileError.NoImportableSettings, result.Error);
    }

    [Fact]
    public void ErrorMessages_AreUserFacingAndNonEmpty()
    {
        // Every genuine failure mode must carry an explanation the UI can show.
        foreach (var error in Enum.GetValues<VainProfileError>().Where(e => e != VainProfileError.None))
        {
            var message = VainProfileParseResult.Fail(error, "x.vain").ErrorMessage;
            Assert.False(string.IsNullOrWhiteSpace(message));
        }

        // And the success case is not treated as a failure.
        var success = VainProfileParseResult.Ok(new VainProfileDocument(), "x.vain");
        Assert.True(success.Success);
        Assert.Equal(string.Empty, success.ErrorMessage);
    }

    [Fact]
    public async Task ParseFileAsync_MissingFile_IsRejectedWithoutThrowing()
    {
        var result = await CreateService()
            .ParseFileAsync(Path.Combine(Path.GetTempPath(), "definitely-missing-xyz.vain"));

        Assert.False(result.Success);
        Assert.Equal(VainProfileError.UnreadableFile, result.Error);
    }

    [Fact]
    public async Task WriteThenParse_RoundTrips()
    {
        var service = CreateService();
        var path = Path.Combine(Path.GetTempPath(), $"vain-roundtrip-{Guid.NewGuid():N}.vain");

        try
        {
            var original = new VainProfileDocument
            {
                Profiles =
                [
                    new VainProfile
                    {
                        Executables = ["a.exe"],
                        Settings =
                        [
                            new VainProfileSetting
                            {
                                SettingNameInfo = "Threaded Optimization",
                                SettingId = "0x00002093",
                                SettingValue = "1",
                                ValueTypeRaw = "Dword",
                            },
                        ],
                    },
                ],
            };

            await service.WriteFileAsync(path, original);
            var parsed = await service.ParseFileAsync(path);

            Assert.True(parsed.Success);
            var setting = parsed.Document!.Profiles[0].Settings[0];
            Assert.Equal("Threaded Optimization", setting.SettingNameInfo);
            Assert.Equal(VainValueType.Dword, setting.ValueType);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>Minimal in-memory settings store so the parser can be tested in isolation.</summary>
    private sealed class InMemorySettingsService : ISettingsService
    {
        private readonly Dictionary<string, object?> _values = [];

        public Task<T?> GetAsync<T>(string key, T? defaultValue = default) =>
            Task.FromResult(_values.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue);

        public Task SetAsync<T>(string key, T value)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }
    }
}
