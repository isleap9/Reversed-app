using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VainTools.App.Models;

namespace VainTools.App.Services;

/// <summary>
/// Registry-backed implementation of <see cref="IRegistryTweakService"/>.
///
/// Reads check both the 64-bit and 32-bit registry views: <c>Explorer\Advanced</c> in
/// particular can differ between them, and reporting a value from the wrong view would
/// show the user a state that is not what Explorer actually sees.
/// </summary>
public sealed class RegistryTweakService : IRegistryTweakService
{
    /// <summary>The view Explorer itself reads on a 64-bit OS.</summary>
    private const RegistryView PreferredView = RegistryView.Registry64;

    private readonly ILogger<RegistryTweakService> _logger;
    private readonly Lazy<bool> _isElevated;

    public RegistryTweakService(ILogger<RegistryTweakService> logger)
    {
        _logger = logger;
        _isElevated = new Lazy<bool>(DetectElevation);
    }

    /// <inheritdoc />
    public bool IsElevated => _isElevated.Value;

    /// <inheritdoc />
    public TweakState Read(RegistryTweak tweak)
    {
        try
        {
            // Prefer the 64-bit view, then fall back — a value may only exist in one.
            var found = TryReadView(tweak, PreferredView);
            if (found is not null)
            {
                return Interpret(found, tweak);
            }

            var otherView = PreferredView == RegistryView.Registry64
                ? RegistryView.Registry32
                : RegistryView.Registry64;

            var fallback = TryReadView(tweak, otherView);
            if (fallback is not null)
            {
                return Interpret(fallback, tweak);
            }

            // Value absent entirely: report the documented default rather than "unknown".
            return TweakState.Unset;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read tweak {Id}", tweak.Id);
            return TweakState.Unknown;
        }
    }

    /// <inheritdoc />
    public Task ApplyAsync(RegistryTweak tweak, CancellationToken cancellationToken = default)
        => SetAsync(tweak, TweakState.Enabled, cancellationToken);

    /// <inheritdoc />
    public Task RevertAsync(RegistryTweak tweak, CancellationToken cancellationToken = default)
        => SetAsync(tweak, TweakState.Disabled, cancellationToken);

    /// <inheritdoc />
    public async Task SetAsync(RegistryTweak tweak, TweakState state, CancellationToken cancellationToken = default)
    {
        if (state is not (TweakState.Enabled or TweakState.Disabled))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state), state, "Only Enabled and Disabled can be written.");
        }

        if (tweak.RequiresAdmin && !IsElevated)
        {
            throw new UnauthorizedAccessException(
                $"'{tweak.Name}' requires administrator rights. Restart Vain Tools as administrator.");
        }

        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var baseKey = RegistryKey.OpenBaseKey(tweak.Hive, PreferredView);

            if (state == TweakState.Disabled && tweak.RemoveKeyWhenDisabled)
            {
                // The switch is the key's existence: remove it entirely.
                baseKey.DeleteSubKeyTree(tweak.KeyPath, throwOnMissingSubKey: false);

                _logger.LogInformation("Removed {Id} key {Key}", tweak.Id, tweak.KeyPath);
                return;
            }

            var value = state == TweakState.Enabled ? tweak.EnabledValue : tweak.DisabledValue;

            using var key = baseKey.CreateSubKey(tweak.KeyPath, writable: true)
                ?? throw new InvalidOperationException($"Could not open or create '{tweak.KeyPath}'.");

            key.SetValue(tweak.ValueName, value, tweak.ValueKind);

            _logger.LogInformation(
                "Set {Id} ({Key}\\{Value}) to {State}",
                tweak.Id, tweak.KeyPath, tweak.ValueName, state);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RestartExplorerAsync(CancellationToken cancellationToken = default)
    {
        // Mirrors the real app: kill explorer.exe, then start it again. Any open File
        // Explorer windows close, which is why this is a confirmed, explicit action.
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Restarting Explorer");

            var kill = Process.Start(new ProcessStartInfo
            {
                FileName = "taskkill.exe",
                Arguments = "/f /im explorer.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            kill?.WaitForExit(10_000);

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true,
            });
        }, cancellationToken);
    }

    private object? TryReadView(RegistryTweak tweak, RegistryView view)
    {
        using var baseKey = RegistryKey.OpenBaseKey(tweak.Hive, view);
        using var key = baseKey.OpenSubKey(tweak.KeyPath, writable: false);

        return key?.GetValue(tweak.ValueName);
    }

    /// <summary>Maps a stored value onto a <see cref="TweakState"/>.</summary>
    private static TweakState Interpret(object? stored, RegistryTweak tweak)
    {
        if (stored is null)
        {
            return TweakState.Unset;
        }

        if (ValuesEqual(stored, tweak.EnabledValue))
        {
            return TweakState.Enabled;
        }

        if (ValuesEqual(stored, tweak.DisabledValue))
        {
            return TweakState.Disabled;
        }

        // Present but neither of the two known values: report it as disabled so the UI
        // shows a definite state, and a write will normalise it.
        return TweakState.Disabled;
    }

    private static bool ValuesEqual(object stored, object expected) => (stored, expected) switch
    {
        (int a, int b) => a == b,
        (int a, string b) => string.Equals(a.ToString(), b, StringComparison.OrdinalIgnoreCase),
        (string a, int b) => string.Equals(a, b.ToString(), StringComparison.OrdinalIgnoreCase),
        (string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase),
        _ => Equals(stored, expected),
    };

    /// <inheritdoc />
    public string? ReadString(RegistryHive hive, string keyPath, string valueName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, PreferredView);
            using var key = baseKey.OpenSubKey(keyPath, writable: false);
            return key?.GetValue(valueName) as string;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read string {Key}\\{Value}", keyPath, valueName);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task WriteString(RegistryHive hive, string keyPath, string valueName, string value)
    {
        await Task.Run(() =>
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, PreferredView);
            using var key = baseKey.CreateSubKey(keyPath, writable: true)
                ?? throw new InvalidOperationException($"Could not open or create '{keyPath}'.");
            key.SetValue(valueName, value, RegistryValueKind.String);
            _logger.LogInformation("Wrote {Key}\\{Value} = {Value}", keyPath, valueName, value);
        });
    }

    /// <inheritdoc />
    public int? ReadDword(RegistryHive hive, string keyPath, string valueName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, PreferredView);
            using var key = baseKey.OpenSubKey(keyPath, writable: false);
            var value = key?.GetValue(valueName);
            return value switch
            {
                int i => i,
                long l => (int)l,
                _ => null,
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read DWORD {Key}\\{Value}", keyPath, valueName);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task WriteDword(RegistryHive hive, string keyPath, string valueName, int value)
    {
        await Task.Run(() =>
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, PreferredView);
            using var key = baseKey.CreateSubKey(keyPath, writable: true)
                ?? throw new InvalidOperationException($"Could not open or create '{keyPath}'.");
            key.SetValue(valueName, value, RegistryValueKind.DWord);
            _logger.LogInformation("Wrote DWORD {Key}\\{Value} = {Value}", keyPath, valueName, value);
        });
    }

    private static bool DetectElevation()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
