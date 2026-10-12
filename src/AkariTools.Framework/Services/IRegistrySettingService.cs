using AkariTools.Framework.Models;

namespace AkariTools.Framework.Services;

public interface IRegistrySettingService
{
    int? ReadDword(RegistryValue value);
    bool WriteDword(RegistryValue value, int data);
    string? ReadString(RegistryValue value);
    bool WriteString(RegistryValue value, string data);
    bool WriteExpandString(RegistryValue value, string data);
    bool KeyExists(RegistryHive hive, string path);
    bool CreateKey(RegistryHive hive, string path);
    bool DeleteKey(RegistryHive hive, string path);
    bool DeleteValue(RegistryValue value);
}
