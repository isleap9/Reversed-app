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

    public async Task<ProcessResult> RunAsync(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdoutResult = await stdout;
        var stderrResult = await stderr;

        _logger.LogDebug("Process {FileName} {Arguments} exited with {ExitCode}",
            fileName, arguments, process.ExitCode);

        return new ProcessResult(process.ExitCode, stdoutResult, stderrResult);
    }
}
