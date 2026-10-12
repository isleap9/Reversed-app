using AkariTools.Framework.Services;
using Xunit;

namespace AkariTools.Tests.Services;

public sealed class CliRunnerTests
{
    [Fact]
    public void Run_Echo_ReturnsOutput()
    {
        // Act
        var runner = new CliRunner();
        var result = runner.Run("cmd.exe", "/c echo HelloWorld");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("HelloWorld", result);
    }

    [Fact]
    public void BogusExe_ReturnsNullNotThrow()
    {
        // Act
        var runner = new CliRunner();
        var result = runner.Run("nonexistent-exe-12345", "/some-arg");

        // Assert
        Assert.Null(result);
    }
}