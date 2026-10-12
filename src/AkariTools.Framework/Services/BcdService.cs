using System.Diagnostics;

namespace AkariTools.Framework.Services;

public sealed class BcdService : IBcdService
{
    private const int TimeoutMilliseconds = 10_000;

    private static string? RunBcdEdit(string arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "bcdedit.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            if (!process.Start())
                return null;
            var output = process.StandardOutput.ReadToEnd();
            // Drain stderr so a verbose failure cannot deadlock the child,
            // but a non-zero exit is still a failure (null), not success.
            _ = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(TimeoutMilliseconds))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort; the process already failed to cooperate.
                }
                return null;
            }
            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    public string? ReadSetting(string name)
    {
        try
        {
            // {current} scopes the read to the running OS: /enum alone returns
            // every boot entry and the parser takes the first match (spec §12.9).
            var output = RunBcdEdit("/enum {current}");
            return output is null ? null : BcdParser.ParseSetting(output, name);
        }
        catch
        {
            return null;
        }
    }

    public bool Set(string name, string value)
    {
        try
        {
            return RunBcdEdit($"/set {name} {value}") is not null;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteValue(string name)
    {
        try
        {
            return RunBcdEdit($"/deletevalue {name}") is not null;
        }
        catch
        {
            return false;
        }
    }
}
