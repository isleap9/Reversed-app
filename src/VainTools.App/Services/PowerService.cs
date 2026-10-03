using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using VainTools.App.Models;

namespace VainTools.App.Services;

/// <summary>
/// Implementation of <see cref="IPowerService"/> wrapping powercfg.exe.
/// </summary>
public sealed class PowerService : IPowerService
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<PowerService> _logger;

    public PowerService(IProcessRunner processRunner, ILogger<PowerService> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    public bool IsElevated =>
        new System.Security.Principal.WindowsPrincipal(
            System.Security.Principal.WindowsIdentity.GetCurrent())
            .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

    public async Task<IReadOnlyList<PowerPlan>> GetPlansAsync()
    {
        var result = await _processRunner.RunAsync("powercfg.exe", "/list");
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"powercfg /list failed (exit {result.ExitCode}): {result.StdErr}");
        return ParsePlans(result.StdOut);
    }

    public async Task<IReadOnlyList<PowerSetting>> GetSettingsAsync(Guid planGuid)
    {
        var result = await _processRunner.RunAsync("powercfg.exe", $"/query {planGuid}");
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"powercfg /query failed (exit {result.ExitCode}): {result.StdErr}");
        return ParseSettings(result.StdOut);
    }

    public async Task ApplySettingAsync(Guid planGuid, string settingGuid, string value)
    {
        // Set AC value
        var acResult = await _processRunner.RunAsync("powercfg.exe",
            $"/setacvalueindex {planGuid} {settingGuid} {value}");
        if (acResult.ExitCode != 0)
            throw new InvalidOperationException($"powercfg /setacvalueindex failed (exit {acResult.ExitCode}): {acResult.StdErr}");

        // Set DC value
        var dcResult = await _processRunner.RunAsync("powercfg.exe",
            $"/setdcvalueindex {planGuid} {settingGuid} {value}");
        if (dcResult.ExitCode != 0)
            throw new InvalidOperationException($"powercfg /setdcvalueindex failed (exit {dcResult.ExitCode}): {dcResult.StdErr}");
    }

    public async Task RevertPlanAsync(Guid planGuid)
    {
        var result = await _processRunner.RunAsync("powercfg.exe", "/restoredefaultschemes");
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"powercfg /restoredefaultschemes failed (exit {result.ExitCode}): {result.StdErr}");
    }

    private static IReadOnlyList<PowerPlan> ParsePlans(string output)
    {
        var plans = new List<PowerPlan>();
        var regex = new Regex(
            @"Power Scheme GUID:\s+([a-f0-9-]+)\s+\((.+?)\)",
            RegexOptions.IgnoreCase);
        foreach (Match match in regex.Matches(output))
        {
            var guid = Guid.Parse(match.Groups[1].Value);
            var name = match.Groups[2].Value;
            var isActive = match.Groups[2].Value.Contains("*") ||
                           output.Contains($"{match.Groups[1].Value}  ({match.Groups[2].Value}) *");
            plans.Add(new PowerPlan(guid, name.TrimEnd('*', ' '), isActive));
        }
        return plans;
    }

    private static IReadOnlyList<PowerSetting> ParseSettings(string output)
    {
        var settings = new List<PowerSetting>();
        var lines = output.Split('\n');
        string? currentPlanGuid = null;
        string? currentSubgroupGuid = null;
        string? currentSubgroupName = null;
        string? currentSettingGuid = null;
        string? currentSettingName = null;
        var possibleValues = new List<string>();
        string? currentAcValue = null;
        string? currentDcValue = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            var planMatch = Regex.Match(line, @"Power Scheme GUID:\s+([a-f0-9-]+)\s+\((.+?)\)");
            if (planMatch.Success)
            {
                currentPlanGuid = planMatch.Groups[1].Value;
                continue;
            }

            var subgroupMatch = Regex.Match(line, @"Subgroup GUID:\s+([a-f0-9-]+)\s+\((.+?)\)");
            if (subgroupMatch.Success)
            {
                currentSubgroupGuid = subgroupMatch.Groups[1].Value;
                currentSubgroupName = subgroupMatch.Groups[2].Value;
                continue;
            }

            var settingMatch = Regex.Match(line, @"Power Setting GUID:\s+([a-f0-9-]+)\s+\((.+?)\)");
            if (settingMatch.Success)
            {
                if (currentSettingGuid != null)
                {
                    settings.Add(new PowerSetting(
                        currentSettingGuid,
                        currentSettingName ?? string.Empty,
                        currentSubgroupGuid ?? string.Empty,
                        currentSubgroupName ?? string.Empty,
                        currentAcValue ?? string.Empty,
                        currentDcValue ?? string.Empty,
                        possibleValues.ToList()));
                }

                currentSettingGuid = settingMatch.Groups[1].Value;
                currentSettingName = settingMatch.Groups[2].Value;
                possibleValues.Clear();
                currentAcValue = null;
                currentDcValue = null;
                continue;
            }

            var possibleMatch = Regex.Match(line, @"Possible Setting Index:\s+0x([0-9a-f]+)", RegexOptions.IgnoreCase);
            if (possibleMatch.Success)
            {
                var hexValue = possibleMatch.Groups[1].Value;
                var intValue = int.Parse(hexValue, NumberStyles.HexNumber);
                possibleValues.Add(intValue.ToString(CultureInfo.InvariantCulture));
                continue;
            }

            var acMatch = Regex.Match(line, @"Current AC Power Setting Index:\s+0x([0-9a-f]+)", RegexOptions.IgnoreCase);
            if (acMatch.Success)
            {
                var hexValue = acMatch.Groups[1].Value;
                var intValue = int.Parse(hexValue, NumberStyles.HexNumber);
                currentAcValue = intValue.ToString(CultureInfo.InvariantCulture);
                continue;
            }

            var dcMatch = Regex.Match(line, @"Current DC Power Setting Index:\s+0x([0-9a-f]+)", RegexOptions.IgnoreCase);
            if (dcMatch.Success)
            {
                var hexValue = dcMatch.Groups[1].Value;
                var intValue = int.Parse(hexValue, NumberStyles.HexNumber);
                currentDcValue = intValue.ToString(CultureInfo.InvariantCulture);
                continue;
            }
        }

        if (currentSettingGuid != null)
        {
            settings.Add(new PowerSetting(
                currentSettingGuid,
                currentSettingName ?? string.Empty,
                currentSubgroupGuid ?? string.Empty,
                currentSubgroupName ?? string.Empty,
                currentAcValue ?? string.Empty,
                currentDcValue ?? string.Empty,
                possibleValues.ToList()));
        }

        return settings;
    }
}
