# Phase 5: Network, Sound, Affinity & Startup — Research

**Researcher:** gsd-phase-researcher
**Date:** 2026-10-03
**Status:** Complete

---

## 1. Network Page

### 1.1 Adapter Enumeration (PowerShell)

The ground truth confirms the real app uses `Get-NetAdapterBinding` for NIC offload toggles. We use `IProcessRunner` to execute PowerShell.

**PowerShell commands:**

```powershell
# List all physical adapters
Get-NetAdapter | Where-Object { $_.Status -eq 'Up' } | Select-Object Name, InterfaceDescription, ifIndex

# Get binding state for a specific adapter
Get-NetAdapterBinding -Name "Ethernet" -ComponentID ms_tcpip

# Enable/disable a binding
Enable-NetAdapterBinding -Name "Ethernet" -ComponentID ms_tcpip
Disable-NetAdapterBinding -Name "Ethernet" -ComponentID ms_tcpip
```

**Offload registry paths** (per-adapter, under `HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{GUID}`):

| Tweak | Value Name | Type | Default | Description |
|-------|-----------|------|---------|-------------|
| TCP Checksum Offload (IPv4) | `*TCPChecksumOffloadIPv4` | DWORD | 1 | 0=disabled, 1=enabled |
| TCP Checksum Offload (IPv6) | `*TCPChecksumOffloadIPv6` | DWORD | 1 | 0=disabled, 1=enabled |
| UDP Checksum Offload (IPv4) | `*UDPChecksumOffloadIPv4` | DWORD | 1 | 0=disabled, 1=enabled |
| UDP Checksum Offload (IPv6) | `*UDPChecksumOffloadIPv6` | DWORD | 1 | 0=disabled, 1=enabled |
| QoS Offload | `*QoSOffload` | DWORD | 1 | 0=disabled, 1=enabled |
| PM WiFi Rekey Offload | `*PMWiFiRekeyOffload` | DWORD | 0 | 0=disabled, 1=enabled |
| Large Send Offload v2 (IPv4) | `*LsoV2IPv4` | DWORD | 1 | 0=disabled, 1=enabled |
| Large Send Offload v2 (IPv6) | `*LsoV2IPv6` | DWORD | 1 | 0=disabled, 1=enabled |
| Receive Side Scaling | `*RSS` | DWORD | 1 | 0=disabled, 1=enabled |
| Receive Segment Coalescing | `*RscIPv4` | DWORD | 0 | 0=disabled, 1=enabled |

**Adapter GUID discovery:**
```powershell
# Get adapter GUIDs for registry path construction
Get-NetAdapter | Select-Object Name, InterfaceGuid
```

**Registry path format:**
```
HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{adapter-guid}
```

### 1.2 DNS Settings (Registry)

**Registry path:** `HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters`

| Setting | Value Name | Type | Default | Description |
|---------|-----------|------|---------|-------------|
| DNS Server | `NameServer` | String | (empty) | Comma-separated DNS servers |
| DNS Suffix | `Domain` | String | (empty) | Primary DNS suffix |
| DNS Search List | `SearchList` | String | (empty) | Semicolon-separated search suffixes |
| Enable DNS Over HTTPS | `EnableDoH` | DWORD | 0 | 0=disabled, 1=enabled |
| DNS Over HTTPS Server | `DoHServer` | String | (empty) | DoH server URL |

**Per-adapter DNS:** `HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{GUID}`
- `NameServer` (String) — per-adapter DNS
- `DhcpNameServer` (String) — DHCP-assigned DNS

### 1.3 NTP Settings (Registry)

**Registry path:** `HKLM\SYSTEM\CurrentControlSet\Services\W32Time\Parameters`

| Setting | Value Name | Type | Default | Description |
|---------|-----------|------|---------|-------------|
| NTP Server | `NtpServer` | String | time.windows.com,0x9 | Semicolon-separated NTP servers |
| Service Type | `Type` | String | NT5DS | NTP/NT5DS/NoSync |
| Special Poll Interval | `SpecialPollInterval` | DWORD | 3600 | Seconds between polls |

**W32Time service config:**
```powershell
# Get current NTP config
w32tm /query /configuration

# Set NTP server
w32tm /config /manualpeerlist:"pool.ntp.org,0x8" /syncfromflags:manual /update

# Restart W32Time service
Restart-Service w32time
```

### 1.4 NetworkService Design

```csharp
public interface INetworkService
{
    Task<List<NetworkAdapter>> GetAdaptersAsync();
    Task<List<OffloadTweak>> GetOffloadTweaksAsync(string adapterGuid);
    Task ApplyOffloadTweakAsync(string adapterGuid, string valueName, int value);
    Task<DnsSettings> GetDnsSettingsAsync();
    Task SetDnsSettingsAsync(DnsSettings settings);
    Task<NtpSettings> GetNtpSettingsAsync();
    Task SetNtpSettingsAsync(NtpSettings settings);
}
```

**Models:**
```csharp
public record NetworkAdapter(string Name, string Guid, string Description, bool IsUp);
public record OffloadTweak(string ValueName, string DisplayName, int CurrentValue, int DefaultValue);
public record DnsSettings(string NameServer, string Domain, string SearchList, bool EnableDoh, string DohServer);
public record NtpSettings(string NtpServer, string Type, int SpecialPollInterval);
```

### 1.5 Integration with Existing Patterns

- Offload tweaks use `RegistryTweakService` + `TweakCatalog` + `TweakList` (D-03)
- `NetworkService` wraps PowerShell for adapter enumeration (D-01)
- DNS/NTP use `RegistryTweakService` directly (D-02)
- `IProcessRunner` from Phase 4 is reused for PowerShell calls

---

## 2. Sound Page

### 2.1 WASAPI COM Interop

The ground truth confirms WASAPI for audio device enumeration and volume control.

**Required COM interfaces:**

```csharp
// IMMDeviceEnumerator — enumerates audio endpoints
[ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDeviceEnumerator
{
    int EnumAudioEndpoints(EDataFlow dataFlow, DeviceState stateMask, out IMMDeviceCollection devices);
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
    // ... other methods
}

// IMMDevice — represents an audio endpoint
[ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDevice
{
    int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object iface);
    int OpenPropertyStore(int stgmAccess, out IPropertyStore properties);
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    int GetState(out DeviceState state);
}

// IAudioEndpointVolume — volume control
[ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioEndpointVolume
{
    int RegisterControlChangeNotify(IAudioEndpointVolumeCallback callback);
    int UnregisterControlChangeNotify(IAudioEndpointVolumeCallback callback);
    int GetChannelCount(out int count);
    int SetMasterVolumeLevel(float level, Guid eventContext);
    int SetMasterVolumeLevelScalar(float level, Guid eventContext);
    int GetMasterVolumeLevel(out float level);
    int GetMasterVolumeLevelScalar(out float level);
    int SetChannelVolumeLevel(int channel, float level, Guid eventContext);
    int SetChannelVolumeLevelScalar(int channel, float level, Guid eventContext);
    int GetChannelVolumeLevel(int channel, out float level);
    int GetChannelVolumeLevelScalar(int channel, out float level);
    int SetMute(bool mute, Guid eventContext);
    int GetMute(out bool mute);
    int GetVolumeStepInfo(out int step, out int stepCount);
    int VolumeStepUp(Guid eventContext);
    int VolumeStepDown(Guid eventContext);
    int QueryHardwareSupport(out int hardwareSupport);
    int GetVolumeRange(out float min, out float max, out float step);
}

// IAudioSessionManager2 — per-app volume
[ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioSessionManager2
{
    int GetAudioSessionControl(ref Guid sessionGuid, int streamFlags, out IAudioSessionControl2 sessionControl);
    int GetSimpleAudioVolume(ref Guid sessionGuid, int streamFlags, out ISimpleAudioVolume audioVolume);
    // ... other methods
}
```

**Enumerate devices:**
```csharp
var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
IMMDeviceCollection devices;
enumerator.EnumAudioEndpoints(EDataFlow.eRender, DeviceState.Active, out devices);
int count;
devices.GetCount(out count);
for (int i = 0; i < count; i++)
{
    IMMDevice device;
    devices.Item(i, out device);
    string id;
    device.GetId(out id);
    // Get friendly name from property store
    IPropertyStore props;
    device.OpenPropertyStore(0, out props);
    // PKEY_Device_FriendlyName = {a45c254e-df1c-4efd-8020-67d146a850e0} 14
    PROPVARIANT varName;
    props.GetValue(ref PKEY_Device_FriendlyName, out varName);
    // ...
}
```

**Get volume:**
```csharp
var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
IMMDevice endpoint;
enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out endpoint);
object obj;
endpoint.Activate(ref IID_IAudioEndpointVolume, CLSCTX.ALL, null, out obj);
var volume = (IAudioEndpointVolume)obj;
float level;
volume.GetMasterVolumeLevelScalar(out level);
bool mute;
volume.GetMute(out mute);
```

**Set volume:**
```csharp
volume.SetMasterVolumeLevelScalar(0.5f, Guid.Empty); // 50%
volume.SetMute(true, Guid.Empty);
```

**COM class:**
```csharp
[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
public class MMDeviceEnumerator { }
```

### 2.2 Spatial Audio (Registry)

**Registry path:** `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Audio`

| Setting | Value Name | Type | Default | Description |
|---------|-----------|------|---------|-------------|
| Spatial Audio | `SpatialAudioEnabled` | DWORD | 0 | 0=disabled, 1=enabled |
| Spatial Audio Type | `SpatialAudioType` | DWORD | 0 | 0=Windows Sonic, 1=Dolby Atmos, 2=DTS Headphone:X |

**Per-device spatial audio:**
```
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Audio\DeviceProperties\{device-guid}
```
- `SpatialAudioEnabled` (DWORD)

### 2.3 Audio Enhancements (Registry)

**Registry path:** `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Audio`

| Setting | Value Name | Type | Default | Description |
|---------|-----------|------|---------|-------------|
| Audio Enhancements | `AudioEnhancementsEnabled` | DWORD | 1 | 0=disabled, 1=enabled |
| Loudness Equalization | `LoudnessEqualization` | DWORD | 0 | 0=disabled, 1=enabled |
| Bass Boost | `BassBoost` | DWORD | 0 | 0=disabled, 1=enabled |
| Virtual Surround | `VirtualSurround` | DWORD | 0 | 0=disabled, 1=enabled |
| Room Correction | `RoomCorrection` | DWORD | 0 | 0=disabled, 1=enabled |

### 2.4 SoundService Design

```csharp
public interface ISoundService
{
    Task<List<AudioDevice>> GetDevicesAsync();
    Task<VolumeInfo> GetVolumeInfoAsync(string deviceId);
    Task SetVolumeAsync(string deviceId, float level);
    Task SetMuteAsync(string deviceId, bool mute);
    Task<SpatialAudioSettings> GetSpatialAudioSettingsAsync();
    Task SetSpatialAudioSettingsAsync(SpatialAudioSettings settings);
    Task<AudioEnhancementSettings> GetEnhancementSettingsAsync();
    Task SetEnhancementSettingsAsync(AudioEnhancementSettings settings);
}
```

**Models:**
```csharp
public record AudioDevice(string Id, string Name, bool IsDefault, bool IsActive);
public record VolumeInfo(float Level, bool IsMute, float Min, float Max);
public record SpatialAudioSettings(bool Enabled, int Type);
public record AudioEnhancementSettings(bool Enabled, bool LoudnessEqualization, bool BassBoost, bool VirtualSurround, bool RoomCorrection);
```

### 2.5 COM Interop Gotchas

- **Threading:** WASAPI COM objects must be created on an STA thread. Use `[STAThread]` or `Task.Run` with `Thread.SetApartmentState(ApartmentState.STA)`.
- **Lifetime:** Always `Marshal.ReleaseComObject` on COM objects to avoid leaks.
- **Activation:** `IMMDevice.Activate` requires `CLSID_IAudioEndpointVolume` GUID: `5CDF2C82-841E-4546-9722-0CF74078229A`.
- **Property keys:** Use `PKEY_Device_FriendlyName` (`{a45c254e-df1c-4efd-8020-67d146a850e0}` 14) for device names.
- **Error handling:** Check `HRESULT` return values; throw on failure.

---

## 3. Affinity Page

### 3.1 P/Invoke Signatures

```csharp
public static class NativeMethods
{
    // Process affinity
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(
        ProcessAccessFlags dwDesiredAccess,
        bool bInheritHandle,
        int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetProcessAffinityMask(
        IntPtr hProcess,
        out IntPtr lpProcessAffinityMask,
        out IntPtr lpSystemAffinityMask);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetProcessAffinityMask(
        IntPtr hProcess,
        IntPtr dwProcessAffinityMask);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    // Process enumeration
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool EnumProcesses(
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4)] int[] lpidProcess,
        int cb,
        out int lpcbNeeded);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool EnumProcessModules(
        IntPtr hProcess,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.SysInt)] IntPtr[] lphModule,
        int cb,
        out int lpcbNeeded);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern int GetModuleBaseName(
        IntPtr hProcess,
        IntPtr hModule,
        [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpBaseName,
        int nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int GetProcessImageFileName(
        IntPtr hProcess,
        [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpImageFileName,
        int nSize);

    // For getting process name without OpenProcess
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(
        ProcessAccessFlags dwDesiredAccess,
        bool bInheritHandle,
        int dwProcessId);
}

[Flags]
public enum ProcessAccessFlags : uint
{
    QueryInformation = 0x0400,
    QueryLimitedInformation = 0x1000,
    Read = 0x0010,
    Write = 0x0020,
    AllAccess = 0x001F0FFF
}
```

### 3.2 Process Enumeration Approach

```csharp
public List<ProcessInfo> GetProcesses()
{
    var processes = new List<ProcessInfo>();
    int[] pids = new int[1024];
    int bytesReturned;
    
    if (!NativeMethods.EnumProcesses(pids, pids.Length * sizeof(int), out bytesReturned))
        throw new Win32Exception(Marshal.GetLastWin32Error());
    
    int count = bytesReturned / sizeof(int);
    for (int i = 0; i < count; i++)
    {
        int pid = pids[i];
        if (pid == 0) continue; // Skip System Idle Process
        
        var handle = NativeMethods.OpenProcess(
            ProcessAccessFlags.QueryInformation | ProcessAccessFlags.Read,
            false, pid);
        
        if (handle == IntPtr.Zero) continue;
        
        try
        {
            // Get process name
            var sb = new StringBuilder(1024);
            if (NativeMethods.GetModuleBaseName(handle, IntPtr.Zero, sb, sb.Capacity) > 0)
            {
                string name = sb.ToString();
                // Get affinity
                IntPtr processAffinity, systemAffinity;
                if (NativeMethods.GetProcessAffinityMask(handle, out processAffinity, out systemAffinity))
                {
                    processes.Add(new ProcessInfo(pid, name, processAffinity.ToInt64(), systemAffinity.ToInt64()));
                }
            }
        }
        finally
        {
            NativeMethods.CloseHandle(handle);
        }
    }
    
    return processes;
}
```

### 3.3 Set Affinity

```csharp
public void SetAffinity(int pid, ulong affinityMask)
{
    var handle = NativeMethods.OpenProcess(
        ProcessAccessFlags.QueryInformation | ProcessAccessFlags.Read,
        false, pid);
    
    if (handle == IntPtr.Zero)
        throw new Win32Exception(Marshal.GetLastWin32Error());
    
    try
    {
        if (!NativeMethods.SetProcessAffinityMask(handle, new IntPtr((long)affinityMask)))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    finally
    {
        NativeMethods.CloseHandle(handle);
    }
}
```

### 3.4 AffinityService Design

```csharp
public interface IAffinityService
{
    Task<List<ProcessInfo>> GetProcessesAsync();
    Task<ulong> GetProcessAffinityAsync(int pid);
    Task<ulong> GetSystemAffinityAsync();
    Task SetProcessAffinityAsync(int pid, ulong affinityMask);
}
```

**Models:**
```csharp
public record ProcessInfo(int Id, string Name, ulong AffinityMask, ulong SystemAffinityMask);
```

### 3.5 Affinity Gotchas

- **64-bit processes:** On 64-bit Windows, `IntPtr` is 64-bit. Use `IntPtr` for affinity masks, not `int`.
- **System processes:** PID 0 (System Idle) and PID 4 (System) cannot have affinity changed. Skip them.
- **Access denied:** Some processes require elevation. Handle `Win32Exception` gracefully.
- **Affinity mask bits:** Each bit represents a CPU core. Bit 0 = CPU 0, bit 1 = CPU 1, etc.
- **System affinity:** The system affinity mask shows which CPUs are available. Process affinity must be a subset.

---

## 4. Startup Page

### 4.1 Run-Key Registry Paths

**HKCU (current user):**
```
HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce
```

**HKLM (all users):**
```
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce
HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run
```

**Value format:** `REG_SZ` — value name is the app name, value data is the command line.

**Example:**
```
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
  "SecurityHealth" = "C:\Windows\System32\SecurityHealthSystray.exe"
  "OneDrive" = "\"C:\Users\user\AppData\Local\Microsoft\OneDrive\OneDrive.exe\" /background"
```

### 4.2 Run-Key Enumeration

```csharp
public List<StartupEntry> GetRunKeyEntries()
{
    var entries = new List<StartupEntry>();
    
    // HKCU Run
    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
    {
        if (key != null)
        {
            foreach (string valueName in key.GetValueNames())
            {
                entries.Add(new StartupEntry(
                    valueName,
                    key.GetValue(valueName)?.ToString() ?? "",
                    "HKCU\\Run",
                    StartupSource.RunKey));
            }
        }
    }
    
    // HKLM Run
    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
    {
        if (key != null)
        {
            foreach (string valueName in key.GetValueNames())
            {
                entries.Add(new StartupEntry(
                    valueName,
                    key.GetValue(valueName)?.ToString() ?? "",
                    "HKLM\\Run",
                    StartupSource.RunKey));
            }
        }
    }
    
    // HKLM WOW6432Node Run
    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"))
    {
        if (key != null)
        {
            foreach (string valueName in key.GetValueNames())
            {
                entries.Add(new StartupEntry(
                    valueName,
                    key.GetValue(valueName)?.ToString() ?? "",
                    "HKLM\\WOW6432Node\\Run",
                    StartupSource.RunKey));
            }
        }
    }
    
    return entries;
}
```

### 4.3 Run-Key Modification

```csharp
public void AddRunKeyEntry(string path, string name, string command)
{
    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
    {
        key.SetValue(name, command, RegistryValueKind.String);
    }
}

public void RemoveRunKeyEntry(string path, string name)
{
    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
    {
        key?.DeleteValue(name, false);
    }
}
```

### 4.4 Scheduled Tasks (WMI)

**WMI query for scheduled tasks with startup triggers:**

```csharp
using System.Management;

public List<ScheduledTaskInfo> GetStartupScheduledTasks()
{
    var tasks = new List<ScheduledTaskInfo>();
    
    // Query for tasks with startup triggers
    var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\TaskScheduler");
    scope.Connect();
    
    // Get all tasks
    var query = new ObjectQuery("SELECT * FROM MSFT_ScheduledTask");
    var searcher = new ManagementObjectSearcher(scope, query);
    
    foreach (ManagementObject task in searcher.Get())
    {
        string name = task["TaskName"]?.ToString() ?? "";
        string path = task["TaskPath"]?.ToString() ?? "";
        string state = task["State"]?.ToString() ?? "";
        string enabled = task["Enabled"]?.ToString() ?? "";
        
        // Check if task has startup trigger
        var triggerQuery = new ObjectQuery(
            $"ASSOCIATORS OF {{MSFT_ScheduledTask.TaskName='{name.Replace("'", "\\'")}',TaskPath='{path.Replace("'", "\\'")}'}} " +
            "WHERE AssocClass = MSFT_TaskTrigger");
        
        var triggerSearcher = new ManagementObjectSearcher(scope, triggerQuery);
        foreach (ManagementObject trigger in triggerSearcher.Get())
        {
            string triggerType = trigger["TriggerType"]?.ToString() ?? "";
            // TriggerType 8 = BootTrigger, 9 = LogonTrigger
            if (triggerType == "8" || triggerType == "9")
            {
                tasks.Add(new ScheduledTaskInfo(name, path, state, enabled, triggerType));
                break;
            }
        }
    }
    
    return tasks;
}
```

**Alternative: Use TaskScheduler COM API (more reliable):**

```csharp
using Microsoft.Win32.TaskScheduler;

public List<ScheduledTaskInfo> GetStartupScheduledTasks()
{
    var tasks = new List<ScheduledTaskInfo>();
    
    using (TaskService ts = new TaskService())
    {
        foreach (Task task in ts.RootFolder.Tasks)
        {
            foreach (Trigger trigger in task.Definition.Triggers)
            {
                if (trigger is BootTrigger || trigger is LogonTrigger)
                {
                    tasks.Add(new ScheduledTaskInfo(
                        task.Name,
                        task.Path,
                        task.State.ToString(),
                        task.Definition.Settings.Enabled.ToString(),
                        trigger.GetType().Name));
                    break;
                }
            }
        }
    }
    
    return tasks;
}
```

**Enable/disable scheduled task:**
```csharp
using (TaskService ts = new TaskService())
{
    Task task = ts.GetTask(taskName);
    if (task != null)
    {
        task.Definition.Settings.Enabled = false; // or true
        task.RegisterChanges();
    }
}
```

### 4.5 StartupService Design

```csharp
public interface IStartupService
{
    Task<List<StartupEntry>> GetRunKeyEntriesAsync();
    Task AddRunKeyEntryAsync(string name, string command, bool isLocalMachine);
    Task RemoveRunKeyEntryAsync(string name, bool isLocalMachine);
    Task<List<ScheduledTaskInfo>> GetScheduledTasksAsync();
    Task EnableScheduledTaskAsync(string taskName, bool enable);
}
```

**Models:**
```csharp
public record StartupEntry(string Name, string Command, string Source, StartupSource SourceType);
public record ScheduledTaskInfo(string Name, string Path, string State, string Enabled, string TriggerType);

public enum StartupSource
{
    RunKey,
    RunOnceKey,
    ScheduledTask
}
```

### 4.6 Startup Gotchas

- **HKLM requires elevation:** Writing to `HKLM` requires admin rights. Use `RegistryTweakService.IsElevated` check.
- **WOW6432Node:** On 64-bit Windows, 32-bit apps write to `WOW6432Node`. Check both paths.
- **RunOnce keys:** `RunOnce` entries are deleted after execution. They may be empty.
- **WMI namespace:** `MSFT_ScheduledTask` is in `\\.\root\Microsoft\Windows\TaskScheduler` (Windows 8+). For older Windows, use `Win32_ScheduledJob`.
- **TaskScheduler COM:** The `Microsoft.Win32.TaskScheduler` NuGet package is more reliable than raw WMI. Consider adding it as a dependency.
- **Task path:** Task names may contain backslashes. Escape them properly in WMI queries.

---

## 5. Code Patterns to Reuse

### 5.1 Service Registration (App.xaml.cs)

```csharp
// In App.xaml.cs, add to the existing DI registration:
services.AddSingleton<INetworkService, NetworkService>();
services.AddSingleton<ISoundService, SoundService>();
services.AddSingleton<IAffinityService, AffinityService>();
services.AddSingleton<IStartupService, StartupService>();
```

### 5.2 RegistryTweakService Usage

```csharp
// For registry-based tweaks (offloads, DNS, NTP, spatial audio, enhancements):
var tweak = new RegistryTweak(
    keyPath: @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{GUID}",
    valueName: "*TCPChecksumOffloadIPv4",
    valueType: RegistryValueKind.DWord,
    defaultValue: 1,
    disabledValue: 0);

await _registryTweakService.ApplyTweakAsync(tweak);
```

### 5.3 IProcessRunner Usage

```csharp
// For PowerShell calls (adapter enumeration, NTP config):
var result = await _processRunner.RunAsync(
    "powershell.exe",
    "-NoProfile -Command \"Get-NetAdapter | Select-Object Name, InterfaceGuid | ConvertTo-Json\"");
```

### 5.4 TweakCatalog Registration

```csharp
// In TweakCatalog.cs, add new tweaks:
new RegistryTweak(
    "Network",
    "TCP Checksum Offload (IPv4)",
    @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{GUID}",
    "*TCPChecksumOffloadIPv4",
    RegistryValueKind.DWord,
    1,
    0),
```

### 5.5 ViewModel Pattern

```csharp
// Follow PowerEditorViewModel pattern:
public partial class NetworkPageViewModel : ObservableObject
{
    private readonly INetworkService _networkService;
    private readonly IRegistryTweakService _registryTweakService;

    public NetworkPageViewModel(
        INetworkService networkService,
        IRegistryTweakService registryTweakService)
    {
        _networkService = networkService;
        _registryTweakService = registryTweakService;
    }

    [ObservableProperty]
    private List<NetworkAdapter> _adapters = [];

    [ObservableProperty]
    private bool _isLoading;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Adapters = await _networkService.GetAdaptersAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 5.6 Page Code-Behind Pattern

```csharp
// Follow existing page scaffold pattern:
public sealed partial class NetworkPage : Page
{
    public NetworkPageViewModel ViewModel { get; }

    public NetworkPage(NetworkPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAsync();
    }
}
```

---

## 6. Test Approach

### 6.1 Test Project Structure

Tests go in `src/VainTools.Tests/` following existing patterns.

### 6.2 Mocking Strategy

| Service | Mock Approach |
|---------|--------------|
| `INetworkService` | Mock `IProcessRunner` for PowerShell calls; mock registry for DNS/NTP |
| `ISoundService` | Mock COM interfaces (`IMMDeviceEnumerator`, `IAudioEndpointVolume`) |
| `IAffinityService` | Mock `NativeMethods` via wrapper interface |
| `IStartupService` | Mock registry access; mock WMI/TaskScheduler |

### 6.3 Test Patterns (from existing tests)

```csharp
// Service test pattern (from ServicesTests.cs):
[Fact]
public async Task GetAdaptersAsync_ReturnsAdapters()
{
    // Arrange
    var mockProcessRunner = new Mock<IProcessRunner>();
    mockProcessRunner
        .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(new ProcessResult(0, "[{\"Name\":\"Ethernet\",\"InterfaceGuid\":\"{GUID}\"}]", ""));
    
    var service = new NetworkService(mockProcessRunner.Object);
    
    // Act
    var result = await service.GetAdaptersAsync();
    
    // Assert
    Assert.Single(result);
    Assert.Equal("Ethernet", result[0].Name);
}

// Registry tweak test pattern (from RegistryTweakServiceTests.cs):
[Fact]
public async Task ApplyTweakAsync_WritesRegistryValue()
{
    // Arrange
    var mockRegistry = new Mock<IRegistryWrapper>();
    var service = new RegistryTweakService(mockRegistry.Object);
    var tweak = new RegistryTweak("Test", @"HKCU\Test", "Value", RegistryValueKind.DWord, 1, 0);
    
    // Act
    await service.ApplyTweakAsync(tweak);
    
    // Assert
    mockRegistry.Verify(x => x.SetValue(@"HKCU\Test", "Value", 0, RegistryValueKind.DWord));
}
```

### 6.4 Test Cases

**NetworkService:**
- `GetAdaptersAsync_ReturnsAdapters` — mock PowerShell JSON output
- `GetAdaptersAsync_HandlesEmptyList` — empty adapter list
- `GetOffloadTweaksAsync_ReturnsTweaks` — mock registry read
- `ApplyOffloadTweakAsync_WritesRegistry` — mock registry write
- `GetDnsSettingsAsync_ReturnsSettings` — mock registry read
- `SetDnsSettingsAsync_WritesRegistry` — mock registry write

**SoundService:**
- `GetDevicesAsync_ReturnsDevices` — mock COM enumerator
- `GetDevicesAsync_HandlesNoDevices` — empty device list
- `GetVolumeInfoAsync_ReturnsVolume` — mock COM volume
- `SetVolumeAsync_SetsVolume` — mock COM volume
- `SetMuteAsync_SetsMute` — mock COM volume

**AffinityService:**
- `GetProcessesAsync_ReturnsProcesses` — mock P/Invoke
- `GetProcessesAsync_SkipsSystemProcesses` — skip PID 0 and 4
- `GetProcessAffinityAsync_ReturnsMask` — mock P/Invoke
- `SetProcessAffinityAsync_SetsMask` — mock P/Invoke
- `SetProcessAffinityAsync_ThrowsOnAccessDenied` — handle Win32Exception

**StartupService:**
- `GetRunKeyEntriesAsync_ReturnsEntries` — mock registry
- `GetRunKeyEntriesAsync_IncludesHkcuAndHklm` — both hives
- `AddRunKeyEntryAsync_WritesRegistry` — mock registry write
- `RemoveRunKeyEntryAsync_DeletesValue` — mock registry delete
- `GetScheduledTasksAsync_ReturnsTasks` — mock WMI/TaskScheduler
- `EnableScheduledTaskAsync_UpdatesTask` — mock TaskScheduler

---

## 7. Risks and Gotchas

### 7.1 Network

- **PowerShell execution:** `Get-NetAdapterBinding` requires the `NetAdapter` module (built into Windows 8+). No additional install needed.
- **Adapter GUID changes:** Adapter GUIDs can change after driver updates or hardware changes. Always enumerate fresh.
- **Offload value semantics:** Some offload values are inverted (0=enabled, 1=disabled). Verify against ground truth.
- **Registry view:** On 64-bit Windows, use `RegistryView.Registry64` to avoid WOW64 redirection.

### 7.2 Sound

- **COM threading:** WASAPI requires STA thread. Use `Task.Run` with `Thread.SetApartmentState(ApartmentState.STA)`.
- **COM lifetime:** Always release COM objects. Use `Marshal.ReleaseComObject` in `finally` blocks.
- **Device state:** Devices can be unplugged/disabled. Check `DeviceState` before accessing.
- **Volume range:** Volume is 0.0 to 1.0 (scalar) or -95.5 dB to 0.0 dB (absolute). Use scalar for simplicity.
- **Spatial audio:** Spatial audio settings may not exist on all systems. Handle missing registry keys gracefully.

### 7.3 Affinity

- **64-bit affinity:** On 64-bit Windows, affinity masks are 64-bit. Use `IntPtr` or `ulong`, not `int`.
- **System processes:** PID 0 and PID 4 cannot be modified. Skip them.
- **Access denied:** Some processes require elevation. Handle `Win32Exception` with `NativeErrorCode == 5` (ACCESS_DENIED).
- **Affinity mask validity:** Process affinity must be a subset of system affinity. Validate before setting.
- **Process lifetime:** Processes may exit between enumeration and affinity set. Handle `InvalidOperationException`.

### 7.4 Startup

- **HKLM elevation:** Writing to `HKLM` requires admin rights. Check `IsElevated` before attempting.
- **WOW6432Node:** On 64-bit Windows, check both `HKLM\SOFTWARE\...` and `HKLM\SOFTWARE\WOW6432Node\...`.
- **RunOnce keys:** `RunOnce` entries are deleted after execution. They may be empty or missing.
- **WMI namespace:** `MSFT_ScheduledTask` requires Windows 8+. For older systems, use `Win32_ScheduledJob` (less reliable).
- **TaskScheduler COM:** The `Microsoft.Win32.TaskScheduler` NuGet package is more reliable than raw WMI. Consider adding it.
- **Task name escaping:** Task names may contain backslashes and quotes. Escape them properly in WMI queries.

### 7.5 General

- **Async all the way:** All service methods should be `async` to avoid blocking the UI thread.
- **Cancellation tokens:** Accept `CancellationToken` in all async methods for proper cancellation support.
- **Error handling:** Wrap all external calls (registry, COM, P/Invoke, WMI) in try-catch with meaningful error messages.
- **Logging:** Use `ILogger<T>` for all services to aid debugging.
- **MVVM compliance:** All ViewModels must use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm.

---

## 8. Implementation Order

1. **NetworkService** — PowerShell adapter enumeration + registry offloads (reuses existing patterns)
2. **SoundService** — WASAPI COM interop + registry toggles (new COM interop code)
3. **AffinityService** — P/Invoke process affinity (new P/Invoke code)
4. **StartupService** — Registry Run-keys + WMI scheduled tasks (reuses registry patterns)
5. **ViewModels** — One per page, following PowerEditorViewModel pattern
6. **Pages** — XAML + code-behind for each page
7. **Tests** — Unit tests for all services and view models
8. **Integration** — Wire up DI in App.xaml.cs, register tweaks in TweakCatalog

---

## 9. References

- **Ground Truth:** `.planning/research/VAIN-TOOLBOX-GROUND-TRUTH.md`
- **UI Spec:** `.planning/phases/05-network-sound-affinity-startup/05-UI-SPEC.md`
- **Requirements:** `.planning/REQUIREMENTS.md` (NET-01 through NET-04, SND-01 through SND-04, AFF-01 through AFF-03, STR-01 through STR-04)
- **Roadmap:** `.planning/ROADMAP.md`
- **Prior Phase Research:** `.planning/phases/04-security-performance-power/04-RESEARCH.md`
- **WASAPI Docs:** https://docs.microsoft.com/en-us/windows/win32/coreaudio/wasapi
- **TaskScheduler COM:** https://github.com/dahall/TaskScheduler
- **P/Invoke Reference:** https://www.pinvoke.net/

---

*Research completed: 2026-10-03*
