using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace VainTools.App.Services;

/// <summary>
/// Default implementation using <see cref="Process.Start"/>.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;

    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        _logger = logger;
    }

    public Task<ProcessResult> RunAsync(string fileName, string arguments)
        => RunCoreAsync(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        });

    /// <inheritdoc />
    /// <remarks>
    /// The argument vector goes into <see cref="ProcessStartInfo.ArgumentList"/>, so
    /// .NET performs the Windows command-line quoting per element. A value containing a
    /// space, a quote or a backslash stays one argument (T-06-09).
    /// </remarks>
    public Task<ProcessResult> RunAsync(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return RunCoreAsync(startInfo);
    }

    private async Task<ProcessResult> RunCoreAsync(ProcessStartInfo startInfo)
    {
        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdoutResult = await stdout;
        var stderrResult = await stderr;

        var commandLine = startInfo.ArgumentList.Count > 0
            ? string.Join(' ', startInfo.ArgumentList)
            : startInfo.Arguments;

        _logger.LogDebug("Process {FileName} {Arguments} exited with {ExitCode}",
            startInfo.FileName, commandLine, process.ExitCode);

        return new ProcessResult(process.ExitCode, stdoutResult, stderrResult);
    }
}
