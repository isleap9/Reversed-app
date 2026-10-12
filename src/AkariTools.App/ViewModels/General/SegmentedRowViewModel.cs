using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AkariTools.App.ViewModels.General;

/// <summary>
/// Base view model for one two-segment (<c>Disabled | Enabled</c>) row,
/// consumed by the shared <c>SegmentedSettingRow</c> control (Task 6 rows).
/// <see cref="ApplyAsync"/> writes the requested segment index; subclasses
/// update <see cref="SelectedIndex"/> on success (typically write then
/// re-read, so the picker reflects reality). Never throws.
/// </summary>
public abstract partial class SegmentedRowViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectedIndex;

    [ObservableProperty]
    private bool _isApplying;

    /// <summary>
    /// Applies the requested segment index and updates
    /// <see cref="SelectedIndex"/> when the write succeeds. Implementations
    /// must not throw; failures leave <see cref="SelectedIndex"/> at its
    /// previous value.
    /// </summary>
    public abstract Task ApplyAsync(int index);

    /// <summary>
    /// Applies the requested segment index. Bound to the control via the
    /// generated <see cref="SelectCommand"/>; the same-value guard makes
    /// re-fired picks (e.g. the segments syncing after a failed apply)
    /// no-ops.
    /// </summary>
    [RelayCommand]
    public async Task SelectAsync(int index)
    {
        // Guard: the page binds SelectedIndex OneWay, so a user pick reaches
        // this command while SelectedIndex still holds the previous value. A
        // re-fired pick carrying the value SelectedIndex already shows
        // (programmatic sync) must not write.
        if (index == SelectedIndex)
            return;

        IsApplying = true;
        try
        {
            await ApplyAsync(index);
        }
        catch
        {
            // Never throw: SelectedIndex keeps its previous value on failure.
        }
        finally
        {
            IsApplying = false;
        }

        // Force the value onto the UI: SelectedIndex only reaches the
        // OneWay-bound control through a change notification, and a failed
        // write leaves the *previous* value (no change). The raise re-syncs
        // the segments to the real state; the pick this re-fires is swallowed
        // by the guard above.
        OnPropertyChanged(nameof(SelectedIndex));
    }
}
