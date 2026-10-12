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
        => _registry.ReadString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, "LocaleName"));

    public bool SetLocaleName(string data)
        => _registry.WriteString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, "LocaleName"), data);

    public string? GetValue(string intlName)
        => _registry.ReadString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, intlName));

    public bool SetValue(string intlName, string data)
        => _registry.WriteString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, intlName), data);

    public int? GetFirstDay()
        => _registry.ReadDword(new RegistryValue(RegistryHive.CurrentUser, _intlPath, "iFirstDayOfWeek"));

    public bool SetFirstDay(int index)
        => _registry.WriteDword(new RegistryValue(RegistryHive.CurrentUser, _intlPath, "iFirstDayOfWeek"), index);
}