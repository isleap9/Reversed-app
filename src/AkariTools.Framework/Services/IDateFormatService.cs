using AkariTools.Framework.Models;

namespace AkariTools.Framework.Services;

public interface IDateFormatService
{
    string? GetLocaleName();
    bool SetLocaleName(string data);
    string? GetValue(string intlName);
    bool SetValue(string intlName, string data);
    int? GetFirstDay();
    bool SetFirstDay(int index);
}