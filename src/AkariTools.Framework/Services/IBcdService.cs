namespace AkariTools.Framework.Services;

public interface IBcdService
{
    string? ReadSetting(string name);
    bool Set(string name, string value);
    bool DeleteValue(string name);
}
