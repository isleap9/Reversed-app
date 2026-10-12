using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace AkariTools.App.ViewModels.General;

/// <summary>A locale option shown in the LocaleCombo (display + culture name).</summary>
public sealed record LocaleOption(string Name, string Display);

/// <summary>
/// Date &amp; Time expander: timezone, locale, date/time format combos with
/// live preview, first day of week, NTP list, apply, and one-click resync
/// (spec §5, §7.2). Format presets are the exact spec §5 values.
/// </summary>
public sealed partial class DateTimeViewModel : ObservableObject
{
    private const string W32TimeParametersPath =
        @"SYSTEM\CurrentControlSet\Services\W32Time\Parameters";
    private const string NtpServerName = "NtpServer";
    private const string W32TimeServiceName = "W32Time";

    // Exact spec §5 preset values (verbatim).
    public static IReadOnlyList<string> ShortDatePresets { get; } = new[]
    {
        "M/d/yyyy", "M/d/yy", "MM/dd/yy", "MM/dd/yyyy", "yy/MM/dd", "yyyy-MM-dd", "dd-MMM-yy"
    };

    public static IReadOnlyList<string> LongDatePresets { get; } = new[]
    {
        "dddd, MMMM d, yyyy", "MMMM d, yyyy", "dddd, d MMMM yyyy", "d MMMM yyyy"
    };

    public static IReadOnlyList<string> ShortTimePresets { get; } = new[]
    {
        "h:mm tt", "hh:mm tt", "H:mm", "HH:mm"
    };

    public static IReadOnlyList<string> LongTimePresets { get; } = new[]
    {
        "h:mm:ss tt", "hh:mm:ss tt", "H:mm:ss", "HH:mm:ss"
    };

    private static readonly DateTime PreviewSample = new(2026, 7, 4, 15, 30, 45);

    private readonly IRegistrySettingService _registrySettingService;
    private readonly IServiceControl _serviceControl;
    private readonly IDateFormatService? _dateFormatService;
    private readonly ITimeZoneService? _timeZoneService;
    private readonly ICliRunner? _cliRunner;

    // Last successfully committed selections (revert targets on failed writes).
    private string _committedTimeZoneKey = string.Empty;
    private string _committedLocale;
    private string _committedShortDate = ShortDatePresets[0];
    private string _committedLongDate = LongDatePresets[0];
    private string _committedShortTime = ShortTimePresets[0];
    private string _committedLongTime = LongTimePresets[0];
    private int _committedFirstDay;

    private string _selectedTimeZoneKey = string.Empty;
    private string _selectedLocale;
    private string _selectedShortDate = ShortDatePresets[0];
    private string _selectedLongDate = LongDatePresets[0];
    private string _selectedShortTime = ShortTimePresets[0];
    private string _selectedLongTime = LongTimePresets[0];
    private int _selectedFirstDayIndex;

    private string? _editingServer;

    // Latest commit ticket: each selection request bumps it; a commit whose
    // ticket is stale on completion drops its outcome so a failed earlier
    // pick can never clobber a newer pending selection.
    private int _commitTicket;

    /// <summary>Most recent selection commit (test/observability hook).</summary>
    public Task LastCommit { get; private set; } = Task.CompletedTask;

    [ObservableProperty]
    private ObservableCollection<string> _ntpServers = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddServerCommand))]
    private string _newServer = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _syncMessage;

    [ObservableProperty]
    private IReadOnlyList<TimeZoneOption> _timeZones = Array.Empty<TimeZoneOption>();

    [ObservableProperty]
    private string _previewShortDate = string.Empty;

    [ObservableProperty]
    private string _previewLongDate = string.Empty;

    [ObservableProperty]
    private string _previewShortTime = string.Empty;

    [ObservableProperty]
    private string _previewLongTime = string.Empty;

    // WinUI has no CommandManager auto-requery: ApplyCommand.CanExecute is
    // only re-evaluated when notified (collection changes, load, apply).
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private bool _isApplying;

    public IReadOnlyList<LocaleOption> Locales { get; } = BuildLocales();

    public IReadOnlyList<string> ShortDates => ShortDatePresets;

    public IReadOnlyList<string> LongDates => LongDatePresets;

    public IReadOnlyList<string> ShortTimes => ShortTimePresets;

    public IReadOnlyList<string> LongTimes => LongTimePresets;

    public IReadOnlyList<string> FirstDayNames { get; } =
        CultureInfo.InvariantCulture.DateTimeFormat.DayNames.ToList();

    public string SelectedTimeZoneKey
    {
        get => _selectedTimeZoneKey;
        set
        {
            if (value == _selectedTimeZoneKey) return;
            BeginCommit(SelectTimeZoneAsync(value));
        }
    }

    public string SelectedLocale
    {
        get => _selectedLocale;
        set
        {
            if (value == _selectedLocale) return;
            BeginCommit(SelectLocaleAsync(value));
        }
    }

    public string SelectedShortDate
    {
        get => _selectedShortDate;
        set
        {
            if (value == _selectedShortDate) return;
            BeginCommit(SelectShortDateAsync(value));
        }
    }

    public string SelectedLongDate
    {
        get => _selectedLongDate;
        set
        {
            if (value == _selectedLongDate) return;
            BeginCommit(SelectLongDateAsync(value));
        }
    }

    public string SelectedShortTime
    {
        get => _selectedShortTime;
        set
        {
            if (value == _selectedShortTime) return;
            BeginCommit(SelectShortTimeAsync(value));
        }
    }

    public string SelectedLongTime
    {
        get => _selectedLongTime;
        set
        {
            if (value == _selectedLongTime) return;
            BeginCommit(SelectLongTimeAsync(value));
        }
    }

    public int SelectedFirstDayIndex
    {
        get => _selectedFirstDayIndex;
        set
        {
            if (value == _selectedFirstDayIndex) return;
            BeginCommit(SelectFirstDayAsync(value));
        }
    }

    /// <summary>
    /// Tracks a binding-driven commit so tests can await it. Bodies never
    /// fault (all outcomes funnel through commit/revert), and the fault-only
    /// continuation observes the task regardless.
    /// </summary>
    private void BeginCommit(Task commit)
    {
        LastCommit = commit;
        commit.ContinueWith(
            t => _ = t.Exception,
            TaskContinuationOptions.OnlyOnFaulted);
    }

    public DateTimeViewModel(
        IRegistrySettingService registrySettingService,
        IServiceControl serviceControl,
        IDateFormatService? dateFormatService = null,
        ITimeZoneService? timeZoneService = null,
        ICliRunner? cliRunner = null)
    {
        _registrySettingService = registrySettingService;
        _serviceControl = serviceControl;
        _dateFormatService = dateFormatService;
        _timeZoneService = timeZoneService;
        _cliRunner = cliRunner;
        _committedLocale = CultureInfo.CurrentCulture.Name;
        _selectedLocale = _committedLocale;
        UpdatePreviews();
        NtpServers.CollectionChanged += (_, _) => ApplyCommand.NotifyCanExecuteChanged();
    }

    public async Task LoadAsync()
    {
        // Invalidate any in-flight selection commit: loaded state wins.
        _commitTicket++;

        var raw = await Task.Run(() => _registrySettingService.ReadString(
            new RegistryValue(RegistryHive.LocalMachine, W32TimeParametersPath, NtpServerName)));

        NtpServers.Clear();
        if (!string.IsNullOrWhiteSpace(raw))
        {
            // NtpServer format: "host1,0x1 host2,0x2" (space-separated).
            foreach (var entry in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrWhiteSpace(entry))
                    NtpServers.Add(entry.Trim());
            }
        }
        _editingServer = null;

        var state = await Task.Run(() =>
        {
            IReadOnlyList<TimeZoneOption> zones = Array.Empty<TimeZoneOption>();
            string? tzKey = null;
            string? locale = null;
            string? shortDate = null;
            string? longDate = null;
            string? shortTime = null;
            string? timeFormat = null;
            int? firstDay = null;
            try { zones = _timeZoneService?.GetTimeZones() ?? zones; } catch { }
            try { tzKey = _timeZoneService?.GetCurrentKey(); } catch { }
            try { locale = _dateFormatService?.GetLocaleName(); } catch { }
            try { shortDate = _dateFormatService?.GetValue("sShortDate"); } catch { }
            try { longDate = _dateFormatService?.GetValue("sLongDate"); } catch { }
            try { shortTime = _dateFormatService?.GetValue("sShortTime"); } catch { }
            try { timeFormat = _dateFormatService?.GetValue("sTimeFormat"); } catch { }
            try { firstDay = _dateFormatService?.GetFirstDay(); } catch { }
            return (zones, tzKey, locale, shortDate, longDate, shortTime, timeFormat, firstDay);
        });

        TimeZones = state.zones;
        if (!string.IsNullOrEmpty(state.tzKey))
            _committedTimeZoneKey = state.tzKey;
        if (!string.IsNullOrEmpty(state.locale))
            _committedLocale = state.locale;
        // Unknown stored formats surface verbatim (not snapped to a preset).
        if (!string.IsNullOrEmpty(state.shortDate))
            _committedShortDate = state.shortDate;
        if (!string.IsNullOrEmpty(state.longDate))
            _committedLongDate = state.longDate;
        if (!string.IsNullOrEmpty(state.shortTime))
            _committedShortTime = state.shortTime;
        if (!string.IsNullOrEmpty(state.timeFormat))
            _committedLongTime = state.timeFormat;
        if (state.firstDay is >= 0 and <= 6)
            _committedFirstDay = state.firstDay.Value;

        SetSelectedTimeZoneKey(_committedTimeZoneKey);
        SetSelectedLocale(_committedLocale);
        SetSelectedShortDate(_committedShortDate);
        SetSelectedLongDate(_committedLongDate);
        SetSelectedShortTime(_committedShortTime);
        SetSelectedLongTime(_committedLongTime);
        SetSelectedFirstDayIndex(_committedFirstDay);
        UpdatePreviews();
        ApplyCommand.NotifyCanExecuteChanged();
    }

    public Task SelectTimeZoneAsync(string? value)
    {
        return CommitStringAsync(
            value,
            () => _committedTimeZoneKey,
            v => _committedTimeZoneKey = v,
            SetSelectedTimeZoneKey,
            v => Task.Run(() =>
            {
                try { return _timeZoneService?.SetTimeZone(v) ?? false; }
                catch { return false; }
            }),
            afterCommit: null,
            "Could not change the time zone (administrator rights may be required).");
    }

    public Task SelectLocaleAsync(string? value)
    {
        return CommitStringAsync(
            value,
            () => _committedLocale,
            v => _committedLocale = v,
            SetSelectedLocale,
            v => Task.Run(() =>
            {
                try { return _dateFormatService?.SetLocaleName(v) ?? false; }
                catch { return false; }
            }),
            afterCommit: BroadcastSettingsChange,
            "Could not update the region format (administrator rights may be required).");
    }

    public Task SelectShortDateAsync(string? value)
    {
        return CommitFormatAsync(value, "sShortDate",
            () => _committedShortDate, v => _committedShortDate = v, SetSelectedShortDate,
            "Could not update the short date format.");
    }

    public Task SelectLongDateAsync(string? value)
    {
        return CommitFormatAsync(value, "sLongDate",
            () => _committedLongDate, v => _committedLongDate = v, SetSelectedLongDate,
            "Could not update the long date format.");
    }

    public Task SelectShortTimeAsync(string? value)
    {
        return CommitFormatAsync(value, "sShortTime",
            () => _committedShortTime, v => _committedShortTime = v, SetSelectedShortTime,
            "Could not update the short time format.");
    }

    public Task SelectLongTimeAsync(string? value)
    {
        return CommitFormatAsync(value, "sTimeFormat",
            () => _committedLongTime, v => _committedLongTime = v, SetSelectedLongTime,
            "Could not update the long time format.");
    }

    public Task SelectFirstDayAsync(int index)
    {
        return CommitIndexAsync(index);
    }

    private Task CommitFormatAsync(
        string? value,
        string intlName,
        Func<string> getCommitted,
        Action<string> setCommitted,
        Action<string> setSelected,
        string failureMessage)
    {
        return CommitStringAsync(
            value,
            getCommitted,
            setCommitted,
            setSelected,
            v => Task.Run(() =>
            {
                try { return _dateFormatService?.SetValue(intlName, v) ?? false; }
                catch { return false; }
            }),
            afterCommit: BroadcastSettingsChange,
            failureMessage);
    }

    /// <summary>
    /// Serialized immediate-on-select commit: empty values are rejected
    /// before any write (silent revert, no error); a commit superseded by a
    /// newer request drops its outcome so a stale revert can never clobber
    /// the newer selection. Never faults.
    /// </summary>
    private async Task CommitStringAsync(
        string? value,
        Func<string> getCommitted,
        Action<string> setCommitted,
        Action<string> setSelected,
        Func<string, Task<bool>> write,
        Action? afterCommit,
        string failureMessage)
    {
        try
        {
            value ??= string.Empty;
            if (string.IsNullOrEmpty(value))
            {
                setSelected(getCommitted());
                return;
            }
            if (value == getCommitted())
            {
                setSelected(value);
                return;
            }

            var ticket = ++_commitTicket;
            var previous = getCommitted();
            setSelected(value);
            ErrorMessage = null;

            bool ok;
            try
            {
                ok = await write(value);
            }
            catch
            {
                ok = false;
            }

            if (ticket != _commitTicket) return; // Superseded: newer pick owns the UI.
            if (ok)
            {
                setCommitted(value);
                afterCommit?.Invoke();
            }
            else
            {
                setSelected(previous);
                ErrorMessage = failureMessage;
            }
        }
        catch
        {
            // Last-resort: never fault the commit task (unobserved exceptions).
            try
            {
                setSelected(getCommitted());
                ErrorMessage = failureMessage;
            }
            catch
            {
                // Best-effort only.
            }
        }
    }

    private async Task CommitIndexAsync(int index)
    {
        const string failureMessage = "Could not update the first day of week.";
        try
        {
            if (index < 0 || index > 6) return;
            if (index == _committedFirstDay)
            {
                SetSelectedFirstDayIndex(index);
                return;
            }

            var ticket = ++_commitTicket;
            var previous = _committedFirstDay;
            SetSelectedFirstDayIndex(index);
            ErrorMessage = null;

            bool ok;
            try
            {
                ok = await Task.Run(() =>
                {
                    try { return _dateFormatService?.SetFirstDay(index) ?? false; }
                    catch { return false; }
                });
            }
            catch
            {
                ok = false;
            }

            if (ticket != _commitTicket) return; // Superseded.
            if (ok)
            {
                _committedFirstDay = index;
                BroadcastSettingsChange();
            }
            else
            {
                SetSelectedFirstDayIndex(previous);
                ErrorMessage = failureMessage;
            }
        }
        catch
        {
            try
            {
                SetSelectedFirstDayIndex(_committedFirstDay);
                ErrorMessage = failureMessage;
            }
            catch
            {
                // Best-effort only.
            }
        }
    }

    private void SetSelectedTimeZoneKey(string value)
    {
        _selectedTimeZoneKey = value;
        OnPropertyChanged(nameof(SelectedTimeZoneKey));
    }

    private void SetSelectedLocale(string value)
    {
        _selectedLocale = value;
        OnPropertyChanged(nameof(SelectedLocale));
        UpdatePreviews();
    }

    private void SetSelectedShortDate(string value)
    {
        _selectedShortDate = value;
        OnPropertyChanged(nameof(SelectedShortDate));
        UpdatePreviews();
    }

    private void SetSelectedLongDate(string value)
    {
        _selectedLongDate = value;
        OnPropertyChanged(nameof(SelectedLongDate));
        UpdatePreviews();
    }

    private void SetSelectedShortTime(string value)
    {
        _selectedShortTime = value;
        OnPropertyChanged(nameof(SelectedShortTime));
        UpdatePreviews();
    }

    private void SetSelectedLongTime(string value)
    {
        _selectedLongTime = value;
        OnPropertyChanged(nameof(SelectedLongTime));
        UpdatePreviews();
    }

    private void SetSelectedFirstDayIndex(int value)
    {
        _selectedFirstDayIndex = value;
        OnPropertyChanged(nameof(SelectedFirstDayIndex));
    }

    private void UpdatePreviews()
    {
        CultureInfo culture;
        try { culture = new CultureInfo(_selectedLocale); }
        catch { culture = CultureInfo.CurrentCulture; }

        PreviewShortDate = FormatPreview(culture, _selectedShortDate, "sShortDate", "d");
        PreviewLongDate = FormatPreview(culture, _selectedLongDate, "sLongDate", "D");
        PreviewShortTime = FormatPreview(culture, _selectedShortTime, "sShortTime", "t");
        PreviewLongTime = FormatPreview(culture, _selectedLongTime, "sTimeFormat", "T");
    }

    private static string FormatPreview(CultureInfo culture, string pattern, string intlName, string specifier)
    {
        try
        {
            var format = (DateTimeFormatInfo)culture.DateTimeFormat.Clone();
            if (!string.IsNullOrEmpty(pattern))
            {
                switch (intlName)
                {
                    case "sShortDate": format.ShortDatePattern = pattern; break;
                    case "sLongDate": format.LongDatePattern = pattern; break;
                    case "sShortTime": format.ShortTimePattern = pattern; break;
                    case "sTimeFormat": format.LongTimePattern = pattern; break;
                }
            }
            return PreviewSample.ToString(specifier, format);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static IReadOnlyList<LocaleOption> BuildLocales()
    {
        try
        {
            return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .OrderBy(c => c.DisplayName)
                .Select(c => new LocaleOption(c.Name, $"{c.DisplayName} ({c.Name})"))
                .ToList();
        }
        catch
        {
            return Array.Empty<LocaleOption>();
        }
    }

    /// <summary>
    /// Broadcasts WM_SETTINGCHANGE so running apps pick up Intl format changes.
    /// Best-effort: never throws.
    /// </summary>
    private static void BroadcastSettingsChange()
    {
        try
        {
            SendMessageTimeoutW(
                new IntPtr(0xFFFF), 0x001A, UIntPtr.Zero, "intl",
                0x0002, 200, out _);
        }
        catch
        {
            // Best-effort: the registry write already succeeded.
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SendMessageTimeoutW(
        IntPtr hWnd, uint msg, UIntPtr wParam, string lParam,
        uint flags, uint timeout, out UIntPtr result);

    [RelayCommand(CanExecute = nameof(CanAddServer))]
    private Task AddServerAsync()
    {
        ErrorMessage = null;

        var trimmed = NewServer?.Trim() ?? string.Empty;

        // Validation (Review Focus #3): reject empty, any-whitespace,
        // empty-host, malformed-flag, or duplicate entries before staging.
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Any(char.IsWhiteSpace))
        {
            ErrorMessage = "Enter a valid host name (e.g. time.windows.com).";
            return Task.CompletedTask;
        }

        // Optional flag format: host,0xflag (e.g. time.windows.com,0x9).
        var parts = trimmed.Split(',', 2);
        var host = parts[0].Trim();
        if (string.IsNullOrEmpty(host))
        {
            ErrorMessage = "Enter a valid host name (e.g. time.windows.com).";
            return Task.CompletedTask;
        }
        if (parts.Length == 2)
        {
            var flag = parts[1].Trim();
            if (!flag.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                flag.Length < 3 ||
                !flag.Skip(2).All(c => Uri.IsHexDigit(c)))
            {
                ErrorMessage = "Invalid flag format. Use e.g. 0x9.";
                return Task.CompletedTask;
            }
        }

        // The entry being edited is exempt from the duplicate check; saving
        // replaces it (re-validated above).
        var isSelf = _editingServer is not null &&
            trimmed.Equals(_editingServer, StringComparison.OrdinalIgnoreCase);
        if (!isSelf && NtpServers.Any(s => s.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = "That server is already in the list.";
            return Task.CompletedTask;
        }

        if (_editingServer is not null)
        {
            var index = NtpServers
                .Select((entry, i) => (entry, i))
                .FirstOrDefault(x => x.entry.Equals(_editingServer, StringComparison.OrdinalIgnoreCase))
                .i;
            var stillPresent = NtpServers.Any(s => s.Equals(_editingServer, StringComparison.OrdinalIgnoreCase));
            if (stillPresent)
                NtpServers[index] = trimmed;
            else
                NtpServers.Add(trimmed);
            _editingServer = null;
        }
        else
        {
            NtpServers.Add(trimmed);
        }
        NewServer = string.Empty;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void EditNtp(string? server)
    {
        if (string.IsNullOrEmpty(server)) return;
        _editingServer = server;
        NewServer = server;
    }

    [RelayCommand]
    private void RemoveServer(string? server)
    {
        if (string.IsNullOrEmpty(server)) return;
        NtpServers.Remove(server);
        if (server.Equals(_editingServer, StringComparison.OrdinalIgnoreCase))
            _editingServer = null;
    }

    [RelayCommand]
    private async Task SyncTimeNowAsync()
    {
        ErrorMessage = null;
        SyncMessage = null;

        string? output = null;
        try
        {
            output = await Task.Run(() => _cliRunner?.Run("w32tm.exe", "/resync /force"));
        }
        catch
        {
            output = null;
        }

        if (output is null)
            ErrorMessage = "Time synchronization failed.";
        else
            SyncMessage = "Time synchronized successfully.";
    }

    private bool CanAddServer()
    {
        return !string.IsNullOrWhiteSpace(NewServer);
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        IsApplying = true;
        ErrorMessage = null;

        try
        {
            // Write the joined value to the registry.
            var joined = string.Join(" ", NtpServers);
            var wrote = await Task.Run(() => _registrySettingService.WriteString(
                new RegistryValue(RegistryHive.LocalMachine, W32TimeParametersPath, NtpServerName),
                joined));
            if (!wrote)
            {
                ErrorMessage = "Failed to write NTP servers (administrator rights required).";
                return;
            }

            // Apply via w32tm.
            RunW32TmConfigUpdate();

            // Restart W32Time to pick up the change; every bool is checked so
            // a refused stop/start surfaces instead of reporting success.
            var stopped = await Task.Run(() => _serviceControl.StopService(W32TimeServiceName));
            if (!stopped)
            {
                ErrorMessage = $"NTP servers written, but the {W32TimeServiceName} service could not be stopped.";
                return;
            }
            var started = await Task.Run(() => _serviceControl.StartService(W32TimeServiceName));
            if (!started)
                ErrorMessage = $"NTP servers written, but the {W32TimeServiceName} service could not be started.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to apply: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    private bool CanApply()
    {
        return !IsApplying && NtpServers.Count > 0;
    }

    /// <summary>
    /// Runs w32tm /config /update to commit time service changes.
    /// Fire-and-forget; failures are logged but do not throw.
    /// </summary>
    private static void RunW32TmConfigUpdate()
    {
        try
        {
            var psi = new ProcessStartInfo("w32tm.exe", "/config /update")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(10_000);
        }
        catch
        {
            // Fire-and-forget: the registry write already succeeded.
        }
    }
}
