using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AkariTools.App;

/// <summary>
/// Initializes the Windows App SDK bootstrapper for unpackaged apps.
/// The ModuleInitializer attribute ensures this runs before the
/// generated Main entry point, so the COM classes are registered
/// before Application.Start is called.
/// </summary>
internal static class Bootstrapper
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        try
        {
            // Load the native bootstrapper DLL explicitly before P/Invoke.
            var baseDir = AppContext.BaseDirectory;
            var runtimesDir = Path.Combine(baseDir, "runtimes");

            if (Directory.Exists(runtimesDir))
            {
                var rid = RuntimeInformation.RuntimeIdentifier;
                if (string.IsNullOrEmpty(rid))
                {
                    rid = RuntimeInformation.ProcessArchitecture switch
                    {
                        Architecture.X64 => "win-x64",
                        Architecture.X86 => "win-x86",
                        Architecture.Arm64 => "win-arm64",
                        _ => "win-x64"
                    };
                }

                var dllPath = Path.Combine(runtimesDir, rid, "native", "Microsoft.WindowsAppRuntime.Bootstrap.dll");
                if (File.Exists(dllPath))
                    NativeLibrary.Load(dllPath);
            }

            // version 0x00010000 = 1.0, tag "2.5.1" matches installed runtime.
            MddBootstrapInitialize(0x00010000, "2.5.1");
        }
        catch
        {
            // Bootstrapper failure is non-fatal: the COM classes may already
            // be registered from installed Windows App SDK Appx packages.
        }
    }

    [DllImport("Microsoft.WindowsAppRuntime.Bootstrap.dll", SetLastError = true)]
    private static extern int MddBootstrapInitialize(uint version, string tag);
}
