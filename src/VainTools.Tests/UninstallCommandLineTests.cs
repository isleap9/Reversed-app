using VainTools.App.Services;
using Xunit;

namespace VainTools.Tests;

/// <summary>
/// Unit tests for <see cref="UninstallCommandLine"/>: the CreateProcess-style split of a
/// registry uninstall command line, bare-name resolution to System32, and the exit-code to
/// outcome mapping. File-existence probes and the system directory are injected, so no test
/// touches the real file system or launches anything.
/// </summary>
public sealed class UninstallCommandLineTests
{
    private const string SystemDirectory = @"C:\Windows\System32";
    private const string UninstallWithSpaces = @"C:\Program Files\App\uninstall.exe";

    /// <summary>Probe that answers false everywhere: nothing exists on disk.</summary>
    private static UninstallLaunch Parse(string commandLine) =>
        UninstallCommandLine.Parse(commandLine, _ => false, SystemDirectory);

    private static UninstallLaunch Parse(string commandLine, Func<string, bool> fileExists) =>
        UninstallCommandLine.Parse(commandLine, fileExists, SystemDirectory);

    [Fact]
    public void Parse_QuotedPathWithSpacesThenSilent_SplitsAtClosingQuote()
    {
        var launch = Parse($"\"{UninstallWithSpaces}\" /SILENT");

        Assert.Equal(UninstallWithSpaces, launch.FileName);
        Assert.Equal("/SILENT", launch.Arguments);
    }

    [Fact]
    public void Parse_UnquotedPathWithSpaces_PicksShortestExistingPrefix()
    {
        var launch = Parse($"{UninstallWithSpaces} /S", p => p == UninstallWithSpaces);

        Assert.Equal(UninstallWithSpaces, launch.FileName);
        Assert.Equal("/S", launch.Arguments);
    }

    [Fact]
    public void Parse_UnquotedPrefixWithoutExtension_AppendsExe()
    {
        var launch = Parse(@"C:\Program Files\App\uninstall /S", p => p == UninstallWithSpaces);

        Assert.Equal(UninstallWithSpaces, launch.FileName);
        Assert.Equal("/S", launch.Arguments);
    }

    [Fact]
    public void Parse_WhenNoPrefixExistsOnDisk_FallsBackToFirstToken()
    {
        var launch = Parse(@"C:\Nope\app.exe /S");

        Assert.Equal(@"C:\Nope\app.exe", launch.FileName);
        Assert.Equal("/S", launch.Arguments);
    }

    [Fact]
    public void Parse_BareMsiExecName_ResolvesToSystem32Copy()
    {
        var expected = $@"{SystemDirectory}\MsiExec.exe";
        var launch = Parse("MsiExec.exe /X{90160000-0011-0000-0000-0000000FF1CE}", p => p == expected);

        Assert.Equal(expected, launch.FileName);
        Assert.Equal("/X{90160000-0011-0000-0000-0000000FF1CE}", launch.Arguments);
    }

    [Fact]
    public void Parse_BareNameAbsentFromSystem32_IsLeftUnchanged()
    {
        var launch = Parse("NotOnThisMachine.exe /x");

        Assert.Equal("NotOnThisMachine.exe", launch.FileName);
        Assert.Equal("/x", launch.Arguments);
    }

    [Fact]
    public void Parse_KeepsEmbeddedQuotesAndCommasVerbatim()
    {
        // rundll32 takes a DLL path and an entry point separated by a comma; both must reach
        // the process untouched.
        var expected = $@"{SystemDirectory}\RunDll32.exe";
        var launch = Parse(
            "RunDll32.exe \"C:\\x y\\z.dll\",Uninstall",
            p => p.EndsWith("RunDll32.exe", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(expected, launch.FileName);
        Assert.Equal("\"C:\\x y\\z.dll\",Uninstall", launch.Arguments);
    }

    [Fact]
    public void Parse_UnterminatedLeadingQuote_TakesRemainderAsFileName()
    {
        var launch = Parse($"\"{UninstallWithSpaces}");

        Assert.Equal(UninstallWithSpaces, launch.FileName);
        Assert.Equal(string.Empty, launch.Arguments);
    }

    [Fact]
    public void Parse_EmptyQuotedName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Parse("\"\" /S"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Parse_BlankInput_Throws(string commandLine)
    {
        Assert.Throws<ArgumentException>(() => Parse(commandLine));
    }

    [Fact]
    public void Parse_RealFileSystemOverload_DelegatesWithRealProbes()
    {
        // Exercises the single-argument overload (File.Exists + Environment.SystemDirectory).
        var launch = UninstallCommandLine.Parse($@"C:\VainToolsMissing-{Guid.NewGuid():N}\uninst.exe /S");

        Assert.EndsWith(@"\uninst.exe", launch.FileName, StringComparison.Ordinal);
        Assert.Equal("/S", launch.Arguments);
    }

    [Fact]
    public void ExitCodeConstants_MatchWindowsMeanings()
    {
        Assert.Equal(0, UninstallCommandLine.ExitSuccess);
        Assert.Equal(3010, UninstallCommandLine.ExitRestartRequired);
        Assert.Equal(1641, UninstallCommandLine.ExitRestartInitiated);
        Assert.Equal(1602, UninstallCommandLine.ExitUserCancelled);
    }

    [Theory]
    [InlineData(0, UninstallOutcome.Succeeded)]
    [InlineData(3010, UninstallOutcome.SucceededRestartRequired)]
    [InlineData(1641, UninstallOutcome.SucceededRestartRequired)]
    [InlineData(1602, UninstallOutcome.Cancelled)]
    [InlineData(1, UninstallOutcome.Failed)]
    [InlineData(7, UninstallOutcome.Failed)]
    [InlineData(-1, UninstallOutcome.Failed)]
    [InlineData(1605, UninstallOutcome.Failed)]
    public void InterpretExitCode_MapsCodeToOutcome(int exitCode, UninstallOutcome expected)
    {
        Assert.Equal(expected, UninstallCommandLine.InterpretExitCode(exitCode));
    }
}
