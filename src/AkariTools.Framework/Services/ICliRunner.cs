namespace AkariTools.Framework.Services;

/// <summary>
/// Interface for running CLI commands with a timeout.
/// </summary>
public interface ICliRunner
{
    /// <summary>
    /// Runs an executable with arguments, returning standard output.
    /// Returns null if the command fails or times out.
    /// </summary>
    /// <param name="exe">The executable path or command to run.</param>
    /// <param name="args">Arguments to pass to the executable.</param>
    /// <returns>Standard output as a string, or null on failure/timeout.</param>
    string? Run(string exe, string args);
}