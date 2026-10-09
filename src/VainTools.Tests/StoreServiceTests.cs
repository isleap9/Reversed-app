using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VainTools.App.Services;
using Xunit;

namespace VainTools.Tests;

/// <summary>
/// Service tests for <see cref="StoreService"/>. The <c>winget</c> CLI is never
/// launched from here — <see cref="IProcessRunner"/> is mocked with output captured
/// from a real <c>winget search</c> on this machine (v1.29.380), so the fixtures
/// reproduce the single-space column separators winget actually emits.
/// </summary>
public sealed class StoreServiceTests
{
    private readonly Mock<IProcessRunner> _processRunnerMock = new();
    private readonly StoreService _service;

    public StoreServiceTests()
    {
        _service = new StoreService(
            _processRunnerMock.Object,
            NullLogger<StoreService>.Instance);
    }

    [Fact]
    public void SearchApps_ParsesRealWingetOutput()
    {
        SetupWinget(new ProcessResult(0, RealSingleRowOutput, string.Empty));

        var apps = _service.SearchApps("7zip");

        var app = Assert.Single(apps);
        Assert.Equal("7-Zip", app.Name);
        Assert.Equal("7zip.7zip", app.Id);
        Assert.Equal("26.04", app.Version);
        Assert.Equal("winget", app.Source);
    }

    [Fact]
    public void SearchApps_ParsesMultiRowOutput()
    {
        // Names up to 40 characters long, ids with and without dots, an "Unknown"
        // version from the msstore source, and a Match column folded away.
        SetupWinget(new ProcessResult(0, RealMultiRowOutput, string.Empty));

        var apps = _service.SearchApps("visual studio code");

        Assert.Equal(6, apps.Count);
        Assert.Equal(
            new StoreApp("Visual Studio Code", "XP9KHM4BK9FZ7Q", "Unknown", "msstore"),
            apps[0]);
        Assert.Equal(
            new StoreApp("Microsoft Visual Studio Code Insiders CLI", "Microsoft.VisualStudioCode.Insiders.CLI", "1.127.0", "winget"),
            apps[^1]);
    }

    [Fact]
    public void SearchApps_ParsesOutputWithMatchColumn()
    {
        SetupWinget(new ProcessResult(0, OutputWithMatchColumn, string.Empty));

        var app = Assert.Single(_service.SearchApps("vlc"));

        Assert.Equal("VLC media player", app.Name);
        Assert.Equal("VideoLAN.VLC", app.Id);
        Assert.Equal("3.0.20", app.Version);
        Assert.Equal("winget", app.Source);
    }

    [Fact]
    public void SearchApps_HandlesEmptyResults()
    {
        SetupWinget(new ProcessResult(0, "No package found matching input criteria.", string.Empty));

        Assert.Empty(_service.SearchApps("zzzz-no-such-package-zzzz"));
    }

    [Fact]
    public void SearchApps_HandlesHeaderOnlyOutput()
    {
        SetupWinget(new ProcessResult(0, RealSingleRowOutput.Split('\n')[0] + "\n", string.Empty));

        Assert.Empty(_service.SearchApps("7zip"));
    }

    [Fact]
    public void SearchApps_HandlesTrulyEmptyOutput()
    {
        SetupWinget(new ProcessResult(0, string.Empty, string.Empty));

        Assert.Empty(_service.SearchApps("7zip"));
    }

    [Fact]
    public void SearchApps_ReturnsEmptyListWhenWingetExitsNonZero()
    {
        SetupWinget(new ProcessResult(-1978335294, string.Empty, "No sources configured; winget cannot search."));

        Assert.Empty(_service.SearchApps("7zip"));
    }

    [Fact]
    public void SearchApps_TreatsRealNoResultsExitAsAnEmptyResult()
    {
        // Verified against winget v1.29.380 on this machine: a query that matches
        // nothing exits -1978335212 and prints "No package found matching input
        // criteria." on STDOUT. That is a successful search with nothing to show, so the
        // service must return an empty list, not surface an error.
        SetupWinget(new ProcessResult(
            -1978335212, "No package found matching input criteria.", string.Empty));

        Assert.Empty(_service.SearchApps("snipping tools"));
    }

    [Fact]
    public void SearchApps_KeepsAMultiWordQueryInOneArgument()
    {
        // The app is the proof that a multi-word query must stay ONE argv element:
        // splitting "snipping tools" into two makes winget fail outright with
        // "Found a positional argument when none was expected: 'tools'" (exit
        // -1978335230), which is a different failure from "no package found".
        SetupWinget(new ProcessResult(0, RealSingleRowOutput, string.Empty));

        _service.SearchApps("snipping tools");

        _processRunnerMock.Verify(
            x => x.RunAsync(
                "winget",
                It.Is<string[]>(a => a.SequenceEqual(new[] { "search", "snipping tools", "--accept-source-agreements" }))),
            Times.Once);
    }

    [Fact]
    public void SearchApps_ReturnsEmptyListForBlankQuery()
    {
        Assert.Empty(_service.SearchApps("   "));
        Assert.Empty(_service.SearchApps(string.Empty));

        // A blank query must never reach the process layer.
        _processRunnerMock.Verify(
            x => x.RunAsync("winget", It.IsAny<string[]>()),
            Times.Never);
    }

    [Fact]
    public void ParseSearch_HandlesMissingVersionAndSource()
    {
        const string output = """
            Name            Id             Version Source
            ----------------------------------------------
            Spartan Tool     Contoso.Spartan

            """;

        var app = Assert.Single(StoreService.ParseSearch(output));

        Assert.Equal("Spartan Tool", app.Name);
        Assert.Equal("Contoso.Spartan", app.Id);
        Assert.Equal(string.Empty, app.Version);
        Assert.Equal(string.Empty, app.Source);
    }

    [Fact]
    public void ParseSearch_SkipsProgressNoiseAndFooters()
    {
        const string output = """
            Name            Id             Version Source
            ----------------------------------------------
            ▒▒▒▒▒ 12%
            Ok App           Contoso.Ok     1.0.0   winget
            1 application(s) found.

            """;

        var apps = StoreService.ParseSearch(output);

        var app = Assert.Single(apps);
        Assert.Equal("Contoso.Ok", app.Id);
        Assert.Equal("winget", app.Source);
    }

    [Fact]
    public void ParseSearch_ReturnsEmptyForNullOrWhitespace()
    {
        Assert.Empty(StoreService.ParseSearch(null!));
        Assert.Empty(StoreService.ParseSearch("   "));
    }

    [Fact]
    public void SearchApps_PassesQueryAsASingleArgument()
    {
        // T-06-09: a query that looks like extra switches must stay ONE argument, so
        // winget cannot be tricked into running "--exact" or an injected switch.
        SetupWinget(new ProcessResult(0, RealSingleRowOutput, string.Empty));

        _service.SearchApps("notepad++ --exact");

        _processRunnerMock.Verify(
            x => x.RunAsync(
                "winget",
                It.Is<string[]>(a =>
                    a.Length == 3 &&
                    a[0] == "search" &&
                    a[1] == "notepad++ --exact" &&
                    a[2] == "--accept-source-agreements")),
            Times.Once);
    }

    [Fact]
    public async Task InstallAppAsync_CallsWingetWithCorrectArguments()
    {
        SetupWinget(new ProcessResult(0, string.Empty, string.Empty));

        await _service.InstallAppAsync("Microsoft.VisualStudioCode");

        _processRunnerMock.Verify(
            x => x.RunAsync(
                "winget",
                It.Is<string[]>(a =>
                    a.Length == 6 &&
                    a[0] == "install" &&
                    a[1] == "--id" &&
                    a[2] == "Microsoft.VisualStudioCode" &&
                    a[3] == "--exact" &&
                    a[4] == "--accept-source-agreements" &&
                    a[5] == "--accept-package-agreements")),
            Times.Once);
    }

    [Fact]
    public async Task InstallAppAsync_ReturnsRawProcessResult()
    {
        SetupWinget(new ProcessResult(0, "Successfully installed", string.Empty));

        var result = await _service.InstallAppAsync("7zip.7zip");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("Successfully installed", result.StdOut);
    }

    [Fact]
    public async Task InstallAppAsync_ReturnsFailureResultWithoutThrowing()
    {
        SetupWinget(new ProcessResult(
            -1978335294, string.Empty, "No package found matching input criteria."));

        var result = await _service.InstallAppAsync("No.Such.Package");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("No package found", result.StdErr);
    }

    [Fact]
    public async Task InstallAppAsync_PropagatesProcessFailure()
    {
        // Contract shared with the other CLI-backed services: the service never
        // swallows; the view model reports.
        _processRunnerMock
            .Setup(x => x.RunAsync("winget", It.IsAny<string[]>()))
            .ThrowsAsync(new InvalidOperationException("winget.exe was not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.InstallAppAsync("7zip.7zip"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InstallAppAsync_RejectsBlankAppId(string appId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.InstallAppAsync(appId));

        _processRunnerMock.Verify(
            x => x.RunAsync("winget", It.IsAny<string[]>()),
            Times.Never);
    }

    [Fact]
    public async Task InstallAppAsync_RejectsNullAppId()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.InstallAppAsync(null!));
    }

    private void SetupWinget(ProcessResult result)
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("winget", It.IsAny<string[]>()))
            .ReturnsAsync(result);
    }

    // Captured verbatim from `winget search --id 7zip.7zip` on this machine
    // (v1.29.380). Note the single-space separators around the id — the reason the
    // parser anchors on header offsets rather than on runs of whitespace.
    private const string RealSingleRowOutput = """
        Name  Id        Version Source
        -------------------------------
        7-Zip 7zip.7zip 26.04   winget

        """;

    // Captured verbatim from `winget search -n 6 "visual studio code"` on this machine.
    private const string RealMultiRowOutput = """
        Name                                      Id                                      Version Source
        --------------------------------------------------------------------------------------------------
        Visual Studio Code                        XP9KHM4BK9FZ7Q                          Unknown msstore
        Visual Studio Code - Insiders             XP8LFCZM790F6B                          Unknown msstore
        Microsoft Visual Studio Code              Microsoft.VisualStudioCode              1.140.0 winget
        Microsoft Visual Studio Code CLI          Microsoft.VisualStudioCode.CLI          1.126.0 winget
        Microsoft Visual Studio Code Insiders     Microsoft.VisualStudioCode.Insiders     1.127.0 winget
        Microsoft Visual Studio Code Insiders CLI Microsoft.VisualStudioCode.Insiders.CLI 1.127.0 winget

        """;

    // Newer winget adds a "Match" column; the parser must not mistake it for Source.
    private const string OutputWithMatchColumn = """
        Name                Id             Version  Match   Source
        -----------------------------------------------------------
        VLC media player    VideoLAN.VLC   3.0.20   vlc     winget

        """;
}

/// <summary>
/// Regression coverage for the argument-vector overload added to
/// <see cref="IProcessRunner"/> for T-06-09: it must launch a real process, capture
/// both streams and report the exit code, and keep a multi-word value in one argument.
/// </summary>
public sealed class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_WithArgumentVector_ExecutesAndCapturesOutput()
    {
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);

        // "one two" must arrive at the target process as a single argument.
        var result = await runner.RunAsync("cmd.exe", "/c", "echo", "one two");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("one two", result.StdOut);
    }
}
