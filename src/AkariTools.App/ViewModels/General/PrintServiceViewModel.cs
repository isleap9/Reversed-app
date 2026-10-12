using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ServiceProcess;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// Print Service segmented row (design spec §5): the Spooler start mode,
/// same rules as <c>GeneralViewModel</c> — Automatic or Manual displays On
/// (index 1; Manual adds the <see cref="Note"/>), anything else is Off
/// (index 0). Applies set the start mode (Automatic/Disabled) and
/// start/stop the service for immediate effect, then re-read to confirm;
/// failures leave <see cref="SegmentedRowViewModel.SelectedIndex"/> at its
/// previous value and surface <see cref="LastError"/>. Never throws.
/// </summary>
public partial class PrintServiceViewModel : SegmentedRowViewModel
{
    private const string SpoolerServiceName = "Spooler";

    private readonly IServiceControl _serviceControl;

    /// <summary>Non-null when Spooler runs in Manual start mode (displays as On + note).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    private string? _note;

    [ObservableProperty]
    private string? _lastError;

    public PrintServiceViewModel(IServiceControl serviceControl)
    {
        _serviceControl = serviceControl;
    }

    public string Label => "Print Service";

    public string Description => Note is null
        ? "Turn the Print Spooler service on or off."
        : "Turn the Print Spooler service on or off. Start mode: Manual.";

    /// <summary>Reads the Spooler start mode; Automatic/Manual = On, else Off.</summary>
    public async Task LoadAsync()
    {
        LastError = null;
        try
        {
            var mode = await Task.Run(() => _serviceControl.GetStartMode(SpoolerServiceName));
            SelectedIndex = Map(mode);
            Note = mode == ServiceStartMode.Manual ? "Manual" : null;
        }
        catch
        {
            // Never throw: leave SelectedIndex at its previous value on failure.
        }
    }

    /// <summary>
    /// Sets the Spooler start mode and starts/stops the service for immediate
    /// effect, then re-reads so the picker reflects reality. A failed start
    /// mode write keeps the previous index; a failed start/stop keeps the
    /// applied index (the mode was set) and reports.
    /// </summary>
    public override async Task ApplyAsync(int index)
    {
        if (index == SelectedIndex)
            return;

        LastError = null;
        try
        {
            var on = index == 1;
            var mode = on ? ServiceStartMode.Automatic : ServiceStartMode.Disabled;
            var ok = await Task.Run(() => _serviceControl.SetStartMode(SpoolerServiceName, mode));
            if (!ok)
            {
                LastError = $"Failed to set {SpoolerServiceName} start mode to {mode}.";
                return;
            }

            var runningOk = on
                ? await Task.Run(() => _serviceControl.StartService(SpoolerServiceName))
                : await Task.Run(() => _serviceControl.StopService(SpoolerServiceName));
            if (!runningOk)
                LastError = $"{SpoolerServiceName} start mode set, but the service could not be {(on ? "started" : "stopped")}.";

            var current = await Task.Run(() => _serviceControl.GetStartMode(SpoolerServiceName));
            SelectedIndex = Map(current);
            Note = current == ServiceStartMode.Manual ? "Manual" : null;
        }
        catch
        {
            // Never throw: SelectedIndex keeps its previous value on failure.
            LastError ??= "Failed to apply Print Service setting.";
        }
    }

    private static int Map(ServiceStartMode? mode) =>
        mode is ServiceStartMode.Automatic or ServiceStartMode.Manual ? 1 : 0;
}
