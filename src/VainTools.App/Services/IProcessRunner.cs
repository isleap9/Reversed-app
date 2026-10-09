namespace VainTools.App.Services;

/// <summary>
/// Abstraction over process execution for testability.
/// </summary>
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string fileName, string arguments);

    /// <summary>
    /// Runs a process with an explicit argument vector.
    /// <para>
    /// Each element becomes one discrete process argument, so untrusted input (a winget
    /// search query, for example) can never break out of its own argument and inject
    /// additional switches (T-06-09). Prefer this overload whenever any part of the
    /// command line comes from user input.
    /// </para>
    /// </summary>
    Task<ProcessResult> RunAsync(string fileName, params string[] arguments);
}

/// <summary>
/// Result of a process execution.
/// </summary>
public record ProcessResult(int ExitCode, string StdOut, string StdErr);
