using System.Diagnostics;

namespace AkariTools.Framework.Services;

/// <summary>
/// CLI runner that executes commands with a fixed timeout.
/// Follows the pattern established by BcdService.
/// </summary>
public sealed class CliRunner : ICliRunner
{
    private const int TimeoutMilliseconds = 10_000;

    public string? Run(string exe, string args)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
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
}