using AkariTools.Framework.Models;
using Microsoft.Win32;
using RegistryHive = AkariTools.Framework.Models.RegistryHive;

namespace AkariTools.Framework.Services;

public sealed class RegistrySettingService : IRegistrySettingService
{
    private static RegistryKey OpenHive(RegistryHive hive) => hive switch
    {
        RegistryHive.CurrentUser => Registry.CurrentUser,
        RegistryHive.LocalMachine => Registry.LocalMachine,
        RegistryHive.ClassesRoot => Registry.ClassesRoot,
        _ => throw new ArgumentOutOfRangeException(nameof(hive)),
    };

    public int? ReadDword(RegistryValue value)
    {
        try
        {
            using var key = OpenHive(value.Hive).OpenSubKey(value.Path);
            return key?.GetValue(value.Name) is int data ? data : null;
        }
        catch
        {
            return null;
        }
    }

    public bool WriteDword(RegistryValue value, int data)
    {
        try
        {
            using var key = OpenHive(value.Hive).CreateSubKey(value.Path);
            if (key is null)
                return false;
            key.SetValue(value.Name, data, RegistryValueKind.DWord);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string? ReadString(RegistryValue value)
    {
        try
        {
            using var key = OpenHive(value.Hive).OpenSubKey(value.Path);
            return key?.GetValue(value.Name) as string;
        }
        catch
        {
            return null;
        }
    }

    public bool WriteString(RegistryValue value, string data)
    {
        try
        {
            using var key = OpenHive(value.Hive).CreateSubKey(value.Path);
            if (key is null)
                return false;
            key.SetValue(value.Name, data, RegistryValueKind.String);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool KeyExists(RegistryHive hive, string path)
    {
        try
        {
            using var key = OpenHive(hive).OpenSubKey(path);
            return key is not null;
        }
        catch
        {
            return false;
        }
    }

    public bool CreateKey(RegistryHive hive, string path)
    {
        try
        {
            using var key = OpenHive(hive).CreateSubKey(path);
            return key is not null;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteKey(RegistryHive hive, string path)
    {
        try
        {
            OpenHive(hive).DeleteSubKeyTree(path, throwOnMissingSubKey: false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
