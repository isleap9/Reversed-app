using VainTools.App.Models;

namespace VainTools.App.Services;

/// <summary>
/// Service for managing Windows power plans via powercfg.exe.
/// </summary>
public interface IPowerService
{
    bool IsElevated { get; }
    Task<IReadOnlyList<PowerPlan>> GetPlansAsync();
    Task<IReadOnlyList<PowerSetting>> GetSettingsAsync(Guid planGuid);
    Task ApplySettingAsync(Guid planGuid, string settingGuid, string value);
    Task RevertPlanAsync(Guid planGuid);
}
