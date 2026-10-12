using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Moq;
using Xunit;

namespace AkariTools.Tests.ViewModels.General;

public sealed class DateTimeViewModelTests
{
    private const string W32TimeParametersPath =
        @"SYSTEM\CurrentControlSet\Services\W32Time\Parameters";

    private static IRegistrySettingService MockRegistry(string? ntpServerValue)
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(
            It.Is<RegistryValue>(v =>
                v.Hive == RegistryHive.LocalMachine &&
                v.Path == W32TimeParametersPath &&
                v.Name == "NtpServer")))
            .Returns(ntpServerValue);
        return registry.Object;
    }

    private static IServiceControl MockServiceControl()
    {
        var sc = new Mock<IServiceControl>();
        sc.Setup(s => s.StopService(It.IsAny<string>())).Returns(true);
        sc.Setup(s => s.StartService(It.IsAny<string>())).Returns(true);
        return sc.Object;
    }

    [Fact]
    public async Task Load_ParsesNtpServerValue()
    {
        var registry = MockRegistry("time.windows.com,0x9 pool.ntp.org,0x2");
        var vm = new DateTimeViewModel(registry, MockServiceControl());

        await vm.LoadAsync();

        Assert.Equal(2, vm.NtpServers.Count);
        Assert.Contains("time.windows.com,0x9", vm.NtpServers);
        Assert.Contains("pool.ntp.org,0x2", vm.NtpServers);
    }

    [Fact]
    public async Task Load_EmptyValue_ClearsServers()
    {
        var registry = MockRegistry(null);
        var vm = new DateTimeViewModel(registry, MockServiceControl());

        await vm.LoadAsync();

        Assert.Empty(vm.NtpServers);
    }

    [Fact]
    public async Task AddServer_RejectsInvalidInput()
    {
        // Review Focus #3: garbage input must be rejected before writing.
        var registry = MockRegistry("");
        var vm = new DateTimeViewModel(registry, MockServiceControl());

        await vm.LoadAsync();

        // Empty string
        vm.NewServer = "   ";
        await vm.AddServerCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.NtpServers);

        // Contains spaces (not a valid single entry)
        vm.NewServer = "time windows com";
        await vm.AddServerCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.NtpServers);
    }

    [Fact]
    public async Task AddServer_AcceptsFlagFormat()
    {
        var registry = MockRegistry("");
        var vm = new DateTimeViewModel(registry, MockServiceControl());

        await vm.LoadAsync();

        vm.NewServer = "pool.ntp.org,0x9";
        await vm.AddServerCommand.ExecuteAsync(null);

        Assert.Null(vm.ErrorMessage);
        Assert.Single(vm.NtpServers);
        Assert.Equal("pool.ntp.org,0x9", vm.NtpServers[0]);
    }

    [Fact]
    public async Task AddServer_RejectsMalformedFlag()
    {
        var registry = MockRegistry("");
        var vm = new DateTimeViewModel(registry, MockServiceControl());

        await vm.LoadAsync();

        // Flag without 0x prefix
        vm.NewServer = "pool.ntp.org,abc";
        await vm.AddServerCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.NtpServers);
    }

    [Fact]
    public async Task AddServer_RejectsEmptyHost()
    {
        // ",0x9" has a valid flag but no host.
        var vm = new DateTimeViewModel(MockRegistry(""), MockServiceControl());
        await vm.LoadAsync();

        vm.NewServer = ",0x9";
        await vm.AddServerCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.NtpServers);
    }

    [Fact]
    public async Task AddServer_RejectsTabWhitespace()
    {
        // Only ' ' was rejected; pasted tabs slipped through to HKLM.
        var vm = new DateTimeViewModel(MockRegistry(""), MockServiceControl());
        await vm.LoadAsync();

        vm.NewServer = "time\twindows.com";
        await vm.AddServerCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.NtpServers);
    }

    [Fact]
    public async Task AddServer_RejectsDuplicates()
    {
        var vm = new DateTimeViewModel(MockRegistry(""), MockServiceControl());
        await vm.LoadAsync();

        vm.NewServer = "time.windows.com,0x9";
        await vm.AddServerCommand.ExecuteAsync(null);
        Assert.Single(vm.NtpServers);

        // Same entry again (case-insensitive) must not double-write to HKLM.
        vm.NewServer = "TIME.windows.com,0x9";
        await vm.AddServerCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Single(vm.NtpServers);
    }

    [Fact]
    public async Task RemoveServer_RemovesEntry()
    {
        var vm = new DateTimeViewModel(MockRegistry(""), MockServiceControl());
        await vm.LoadAsync();

        vm.NewServer = "time.windows.com,0x9";
        await vm.AddServerCommand.ExecuteAsync(null);
        vm.NewServer = "pool.ntp.org,0x2";
        await vm.AddServerCommand.ExecuteAsync(null);
        Assert.Equal(2, vm.NtpServers.Count);

        vm.RemoveServerCommand.Execute("time.windows.com,0x9");

        Assert.Single(vm.NtpServers);
        Assert.Equal("pool.ntp.org,0x2", vm.NtpServers[0]);
    }

    [Fact]
    public async Task Apply_NotifiesCanExecute_WhenListChanges()
    {
        // The Apply button enables only via CanExecuteChanged: WinUI has no
        // CommandManager auto-requery, so the VM must notify on every change.
        var vm = new DateTimeViewModel(MockRegistry(""), MockServiceControl());
        await vm.LoadAsync();
        Assert.False(vm.ApplyCommand.CanExecute(null));

        var fired = false;
        vm.ApplyCommand.CanExecuteChanged += (_, _) => fired = true;

        vm.NewServer = "time.windows.com";
        await vm.AddServerCommand.ExecuteAsync(null);

        Assert.True(fired);
        Assert.True(vm.ApplyCommand.CanExecute(null));
    }

    [Fact]
    public async Task Apply_NotifiesCanExecute_AfterLoadWithServers()
    {
        var vm = new DateTimeViewModel(
            MockRegistry("time.windows.com,0x9"), MockServiceControl());

        var fired = false;
        vm.ApplyCommand.CanExecuteChanged += (_, _) => fired = true;
        await vm.LoadAsync();

        Assert.True(fired);
        Assert.True(vm.ApplyCommand.CanExecute(null));
    }

    [Fact]
    public async Task Apply_RegistryWriteFailure_SetsError()
    {
        // The real registry service returns false (never throws) on denied
        // writes: Apply must surface that instead of reporting success.
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(It.IsAny<RegistryValue>())).Returns("");
        registry.Setup(r => r.WriteString(It.IsAny<RegistryValue>(), It.IsAny<string>())).Returns(false);
        var sc = new Mock<IServiceControl>();

        var vm = new DateTimeViewModel(registry.Object, sc.Object);
        await vm.LoadAsync();
        vm.NtpServers.Add("time.windows.com,0x9");

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        sc.Verify(s => s.StopService(It.IsAny<string>()), Times.Never);
        sc.Verify(s => s.StartService(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Apply_ServiceStopFailure_SetsError()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(It.IsAny<RegistryValue>())).Returns("");
        registry.Setup(r => r.WriteString(It.IsAny<RegistryValue>(), It.IsAny<string>())).Returns(true);
        var sc = new Mock<IServiceControl>();
        sc.Setup(s => s.StopService(It.IsAny<string>())).Returns(false);
        sc.Setup(s => s.StartService(It.IsAny<string>())).Returns(true);

        var vm = new DateTimeViewModel(registry.Object, sc.Object);
        await vm.LoadAsync();
        vm.NtpServers.Add("time.windows.com,0x9");

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task Apply_WritesJoinedValue()
    {
        string? writtenValue = null;
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(It.IsAny<RegistryValue>())).Returns("");
        registry.Setup(r => r.WriteString(
                It.Is<RegistryValue>(v =>
                    v.Hive == RegistryHive.LocalMachine &&
                    v.Path == W32TimeParametersPath &&
                    v.Name == "NtpServer"),
                It.IsAny<string>()))
            .Callback<RegistryValue, string>((_, val) => writtenValue = val)
            .Returns(true);

        var vm = new DateTimeViewModel(registry.Object, MockServiceControl());
        await vm.LoadAsync();

        vm.NtpServers.Add("time.windows.com,0x9");
        vm.NtpServers.Add("pool.ntp.org,0x2");

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.NotNull(writtenValue);
        Assert.Contains("time.windows.com,0x9", writtenValue);
        Assert.Contains("pool.ntp.org,0x2", writtenValue);
    }

    private static IDateFormatService MockDateFormat(
        string? locale = null,
        string? shortDate = null,
        string? longDate = null,
        string? shortTime = null,
        string? timeFormat = null,
        int? firstDay = null)
    {
        var df = new Mock<IDateFormatService>();
        df.Setup(d => d.GetLocaleName()).Returns(locale);
        df.Setup(d => d.GetValue("sShortDate")).Returns(shortDate);
        df.Setup(d => d.GetValue("sLongDate")).Returns(longDate);
        df.Setup(d => d.GetValue("sShortTime")).Returns(shortTime);
        df.Setup(d => d.GetValue("sTimeFormat")).Returns(timeFormat);
        df.Setup(d => d.GetFirstDay()).Returns(firstDay);
        df.Setup(d => d.SetLocaleName(It.IsAny<string>())).Returns(true);
        df.Setup(d => d.SetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        df.Setup(d => d.SetFirstDay(It.IsAny<int>())).Returns(true);
        return df.Object;
    }

    private static ITimeZoneService MockTimeZone(
        IReadOnlyList<TimeZoneOption>? zones = null,
        string? currentKey = null)
    {
        var tz = new Mock<ITimeZoneService>();
        tz.Setup(t => t.GetTimeZones()).Returns(zones ?? Array.Empty<TimeZoneOption>());
        tz.Setup(t => t.GetCurrentKey()).Returns(currentKey);
        tz.Setup(t => t.SetTimeZone(It.IsAny<string>())).Returns(true);
        return tz.Object;
    }

    private static ICliRunner MockCliRunner(string? output = "")
    {
        var runner = new Mock<ICliRunner>();
        runner.Setup(r => r.Run(It.IsAny<string>(), It.IsAny<string>())).Returns(output);
        return runner.Object;
    }

    private static DateTimeViewModel CreateFullVm(
        IRegistrySettingService? registry = null,
        IDateFormatService? dateFormat = null,
        ITimeZoneService? timeZone = null,
        ICliRunner? cliRunner = null)
    {
        return new DateTimeViewModel(
            registry ?? MockRegistry(""),
            MockServiceControl(),
            dateFormat ?? MockDateFormat(),
            timeZone ?? MockTimeZone(),
            cliRunner ?? MockCliRunner());
    }

    [Fact]
    public async Task Load_UnknownFormat_ShowsRaw()
    {
        // A stored sShortDate outside the preset set must surface verbatim.
        var vm = CreateFullVm(dateFormat: MockDateFormat(shortDate: "yyyy/MM/dd"));
        await vm.LoadAsync();
        Assert.Equal("yyyy/MM/dd", vm.SelectedShortDate);
    }

    [Fact]
    public async Task SetTimeZone_Failure_Reverts()
    {
        var zones = new List<TimeZoneOption>
        {
            new("Eastern Standard Time", "(UTC-05:00) Eastern Time (US & Canada)"),
            new("Pacific Standard Time", "(UTC-08:00) Pacific Time (US & Canada)"),
        };
        var tz = new Mock<ITimeZoneService>();
        tz.Setup(t => t.GetTimeZones()).Returns(zones);
        tz.Setup(t => t.GetCurrentKey()).Returns("Eastern Standard Time");
        tz.Setup(t => t.SetTimeZone(It.IsAny<string>())).Returns(false);

        var vm = CreateFullVm(timeZone: tz.Object);
        await vm.LoadAsync();
        Assert.Equal("Eastern Standard Time", vm.SelectedTimeZoneKey);

        await vm.SelectTimeZoneAsync("Pacific Standard Time");

        Assert.Equal("Eastern Standard Time", vm.SelectedTimeZoneKey);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task EditSave_DuplicateExceptSelf_Rejected()
    {
        var vm = CreateFullVm(registry: MockRegistry("alpha,0x1 beta,0x2"));
        await vm.LoadAsync();
        Assert.Equal(2, vm.NtpServers.Count);

        // Edit "alpha,0x1", then try to save it as the other existing entry.
        vm.EditNtpCommand.Execute("alpha,0x1");
        Assert.Equal("alpha,0x1", vm.NewServer);
        vm.NewServer = "beta,0x2";
        await vm.AddServerCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Equal(2, vm.NtpServers.Count);
        Assert.Contains("alpha,0x1", vm.NtpServers);
        Assert.Contains("beta,0x2", vm.NtpServers);
    }

    [Fact]
    public async Task SelectLocale_WritesLocaleName()
    {
        var df = new Mock<IDateFormatService>();
        df.Setup(d => d.GetLocaleName()).Returns("en-US");
        df.Setup(d => d.SetLocaleName(It.IsAny<string>())).Returns(true);

        var vm = CreateFullVm(dateFormat: df.Object);
        await vm.LoadAsync();

        await vm.SelectLocaleAsync("de-DE");

        df.Verify(d => d.SetLocaleName("de-DE"), Times.Once);
        Assert.Equal("de-DE", vm.SelectedLocale);
    }

    [Fact]
    public async Task SelectShortDate_WritesPreset()
    {
        var df = new Mock<IDateFormatService>();
        df.Setup(d => d.GetValue("sShortDate")).Returns("M/d/yyyy");
        df.Setup(d => d.SetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var vm = CreateFullVm(dateFormat: df.Object);
        await vm.LoadAsync();

        await vm.SelectShortDateAsync("yyyy-MM-dd");

        df.Verify(d => d.SetValue("sShortDate", "yyyy-MM-dd"), Times.Once);
        Assert.Equal("yyyy-MM-dd", vm.SelectedShortDate);
    }

    [Fact]
    public async Task SyncTimeNow_RunsResync()
    {
        var runner = new Mock<ICliRunner>();
        runner.Setup(r => r.Run(It.IsAny<string>(), It.IsAny<string>())).Returns("success");

        var vm = CreateFullVm(cliRunner: runner.Object);
        await vm.LoadAsync();
        await vm.SyncTimeNowCommand.ExecuteAsync(null);

        runner.Verify(r => r.Run("w32tm.exe", "/resync /force"), Times.Once);
    }

    [Fact]
    public async Task SyncTimeNow_Failure_SetsError()
    {
        var runner = new Mock<ICliRunner>();
        runner.Setup(r => r.Run(It.IsAny<string>(), It.IsAny<string>())).Returns((string?)null);

        var vm = CreateFullVm(cliRunner: runner.Object);
        await vm.LoadAsync();
        await vm.SyncTimeNowCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task Setter_SelectShortDate_Commits()
    {
        // Binding path: property set (as TwoWay SelectedItem does), then the
        // tracked commit completes with the write applied.
        var df = new Mock<IDateFormatService>();
        df.Setup(d => d.GetValue("sShortDate")).Returns("M/d/yyyy");
        df.Setup(d => d.SetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var vm = CreateFullVm(dateFormat: df.Object);
        await vm.LoadAsync();

        vm.SelectedShortDate = "yyyy-MM-dd";
        await vm.LastCommit;

        df.Verify(d => d.SetValue("sShortDate", "yyyy-MM-dd"), Times.Once);
        Assert.Equal("yyyy-MM-dd", vm.SelectedShortDate);
        Assert.Equal("2026-07-04", vm.PreviewShortDate);
    }

    [Fact]
    public async Task Setter_SelectTimeZone_Failure_Reverts()
    {
        var zones = new List<TimeZoneOption>
        {
            new("Eastern Standard Time", "(UTC-05:00) Eastern Time (US & Canada)"),
            new("Pacific Standard Time", "(UTC-08:00) Pacific Time (US & Canada)"),
        };
        var tz = new Mock<ITimeZoneService>();
        tz.Setup(t => t.GetTimeZones()).Returns(zones);
        tz.Setup(t => t.GetCurrentKey()).Returns("Eastern Standard Time");
        tz.Setup(t => t.SetTimeZone(It.IsAny<string>())).Returns(false);

        var vm = CreateFullVm(timeZone: tz.Object);
        await vm.LoadAsync();

        vm.SelectedTimeZoneKey = "Pacific Standard Time";
        await vm.LastCommit;

        Assert.Equal("Eastern Standard Time", vm.SelectedTimeZoneKey);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task Preview_Updates_WithLocaleAndFormat()
    {
        var vm = CreateFullVm(dateFormat: MockDateFormat(locale: "en-US", shortDate: "M/d/yyyy"));
        await vm.LoadAsync();
        Assert.Equal("7/4/2026", vm.PreviewShortDate);

        await vm.SelectShortDateAsync("yyyy-MM-dd");
        Assert.Equal("2026-07-04", vm.PreviewShortDate);

        // "/" in a custom pattern renders the locale's date separator.
        await vm.SelectLocaleAsync("de-DE");
        Assert.Equal("de-DE", vm.SelectedLocale);
        Assert.Equal("2026-07-04", vm.PreviewShortDate);

        await vm.SelectShortDateAsync("M/d/yyyy");
        Assert.Equal("7.4.2026", vm.PreviewShortDate);
    }

    [Fact]
    public async Task SelectFirstDay_WritesIndex()
    {
        var df = new Mock<IDateFormatService>();
        df.Setup(d => d.GetFirstDay()).Returns(0);
        df.Setup(d => d.SetFirstDay(It.IsAny<int>())).Returns(true);

        var vm = CreateFullVm(dateFormat: df.Object);
        await vm.LoadAsync();
        Assert.Equal(0, vm.SelectedFirstDayIndex);

        await vm.SelectFirstDayAsync(3);

        df.Verify(d => d.SetFirstDay(3), Times.Once);
        Assert.Equal(3, vm.SelectedFirstDayIndex);
    }

    [Fact]
    public async Task SelectFirstDay_Failure_Reverts()
    {
        var df = new Mock<IDateFormatService>();
        df.Setup(d => d.GetFirstDay()).Returns(1);
        df.Setup(d => d.SetFirstDay(It.IsAny<int>())).Returns(false);

        var vm = CreateFullVm(dateFormat: df.Object);
        await vm.LoadAsync();
        Assert.Equal(1, vm.SelectedFirstDayIndex);

        await vm.SelectFirstDayAsync(5);

        Assert.Equal(1, vm.SelectedFirstDayIndex);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task SelectLocale_Failure_Reverts()
    {
        var df = new Mock<IDateFormatService>();
        df.Setup(d => d.GetLocaleName()).Returns("en-US");
        df.Setup(d => d.SetLocaleName(It.IsAny<string>())).Returns(false);

        var vm = CreateFullVm(dateFormat: df.Object);
        await vm.LoadAsync();

        await vm.SelectLocaleAsync("de-DE");

        Assert.Equal("en-US", vm.SelectedLocale);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task SelectShortTime_Failure_Reverts()
    {
        var df = new Mock<IDateFormatService>();
        df.Setup(d => d.GetValue("sShortTime")).Returns("h:mm tt");
        df.Setup(d => d.SetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var vm = CreateFullVm(dateFormat: df.Object);
        await vm.LoadAsync();

        await vm.SelectShortTimeAsync("HH:mm");

        Assert.Equal("h:mm tt", vm.SelectedShortTime);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task SelectShortDate_EmptyValue_RevertsSilently()
    {
        // Binding coercion can push null/empty (unknown raw value, failed
        // pick): must revert without writing and without an error.
        var df = new Mock<IDateFormatService>();
        df.Setup(d => d.GetValue("sShortDate")).Returns("M/d/yyyy");
        df.Setup(d => d.SetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var vm = CreateFullVm(dateFormat: df.Object);
        await vm.LoadAsync();

        await vm.SelectShortDateAsync(null);

        Assert.Equal("M/d/yyyy", vm.SelectedShortDate);
        Assert.Null(vm.ErrorMessage);
        df.Verify(d => d.SetValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
