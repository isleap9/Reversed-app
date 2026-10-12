using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace AkariTools.Tests;

// The native bootstrapper is MddBootstrapInitialize(majorMinorVersion,
// versionTag, minVersion): three parameters, wide strings. A two-parameter
// ANSI declaration feeds the native call a garbage third argument and fails
// exactly on clean machines where the bootstrapper matters.
public sealed class BootstrapperTests
{
    [Fact]
    public void BootstrapPInvoke_MatchesNativeSignature()
    {
        // Internal to the App assembly: resolve via a public sibling type.
        var appAssembly = typeof(AkariTools.App.ViewModels.General.GeneralViewModel).Assembly;
        var bootstrapper = appAssembly.GetType("AkariTools.App.Bootstrapper");
        Assert.NotNull(bootstrapper);

        var method = bootstrapper.GetMethod(
            "MddBootstrapInitialize", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        Assert.Equal(3, method.GetParameters().Length);
        Assert.Equal(typeof(uint), method.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(string), method.GetParameters()[1].ParameterType);

        var dllImport = method.GetCustomAttribute<DllImportAttribute>();
        Assert.NotNull(dllImport);
        Assert.Equal(CharSet.Unicode, dllImport.CharSet);
    }
}
