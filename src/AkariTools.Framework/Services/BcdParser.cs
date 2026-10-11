namespace AkariTools.Framework.Services;

public static class BcdParser
{
    public static string? ParseSetting(string output, string name)
    {
        if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(name))
            return null;

        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0].Equals(name, StringComparison.OrdinalIgnoreCase))
                return parts[1].Trim();
        }

        return null;
    }
}
