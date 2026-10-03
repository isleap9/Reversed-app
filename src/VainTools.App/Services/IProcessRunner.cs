namespace VainTools.App.Services;

/// <summary>
/// Abstraction over process execution for testability.
/// </summary>
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string fileName, string arguments);
}

/// <summary>
/// Result of a process execution.
/// </summary>
public record ProcessResult(int ExitCode, string StdOut, string StdErr);
