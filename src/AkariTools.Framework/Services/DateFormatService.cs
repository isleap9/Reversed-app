using AkariTools.Framework.Models;

namespace AkariTools.Framework.Services;

public sealed class DateFormatService : IDateFormatService
{
    private readonly string _intlPath;
    private readonly IRegistrySettingService _registry;

    public DateFormatService(IRegistrySettingService registry) : this(registry, @"Control Panel\International") { }

    public DateFormatService(IRegistrySettingService registry, string intlPath)
    {
        _registry = registry;
        _intlPath = intlPath;
    }

    public string? GetLocaleName()
    {
        try
        {
            return _registry.ReadString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, "LocaleName"));
        }
        catch
        {
            return null;
        }
    }

    public bool SetLocaleName(string data)
    {
        try
        {
            return _registry.WriteString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, "LocaleName"), data);
        }
        catch
        {
            return false;
        }
    }

    public string? GetValue(string intlName)
    {
        try
        {
            return _registry.ReadString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, intlName));
        }
        catch
        {
            return null;
        }
    }

    public bool SetValue(string intlName, string data)
    {
        try
        {
            return _registry.WriteString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, intlName), data);
        }
        catch
        {
            return false;
        }
    }

    public int? GetFirstDay()
    {
        try
        {
            // The real iFirstDayOfWeek representation is REG_SZ (decimal string).
            var raw = _registry.ReadString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, "iFirstDayOfWeek"));
            return int.TryParse(raw, out var index) ? index : null;
        }
        catch
        {
            return null;
        }
    }

    public bool SetFirstDay(int index)
    {
        try
        {
            // The real iFirstDayOfWeek representation is REG_SZ (decimal string), not REG_DWORD.
            return _registry.WriteString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, "iFirstDayOfWeek"), index.ToString());
        }
        catch
        {
            return false;
        }
    }
}
