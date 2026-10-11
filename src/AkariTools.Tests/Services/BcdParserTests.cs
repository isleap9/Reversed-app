using AkariTools.Framework.Services;
using Xunit;

namespace AkariTools.Tests.Services;

public sealed class BcdParserTests
{
    private const string EnglishSample = """
        Windows Boot Loader
        -------------------
        identifier              {current}
        device                  partition=C:
        path                    \Windows\system32\winload.exe
        description             Windows 10
        hypervisorlaunchtype    Auto
        isolatedcontext         Yes
        """;

    // Localized (e.g. German) bcdedit output where the token name itself is translated.
    private const string LocalizedSample = """
        Windows-Startladeprogramm
        -------------------------
        Bezeichner               {current}
        Gerat                    partition=C:
        Hypervisorstarttyp       Auto
        """;

    [Fact]
    public void ParseSetting_FindsValue()
    {
        Assert.Equal("Auto", BcdParser.ParseSetting(EnglishSample, "hypervisorlaunchtype"));
    }

    [Fact]
    public void UnknownOutput_ReturnsNull()
    {
        Assert.Null(BcdParser.ParseSetting(LocalizedSample, "hypervisorlaunchtype"));
    }

    [Fact]
    public void MissingName_ReturnsNull()
    {
        Assert.Null(BcdParser.ParseSetting(EnglishSample, "nosuchsetting"));
    }
}
