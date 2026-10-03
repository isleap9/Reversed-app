# Phase 5: Network, Sound, Affinity & Startup — UI Design Contract

> **Phase:** 05-network-sound-affinity-startup  
> **App:** VainTools.App (WinUI 3 / C# / Windows App SDK)  
> **Date:** 2026-10-03  
> **Status:** approved  
**Reviewed:** 2026-10-03 by gsd-ui-checker (APPROVED with 2 non-blocking FLAGs)  
> **Author:** gsd-ui-researcher

---

## 1. Overview

Phase 5 delivers four feature pages that extend the Vain Tools desktop utility beyond registry tweaks into live system configuration:

| Page | Purpose | Layout Pattern |
|------|---------|----------------|
| **Network** | Adapter offload toggles, DNS/NTP server settings | TweakList (offload toggles) + custom card section (DNS/NTP) |
| **Sound** | Audio device enumeration, per-device volume, spatial audio & enhancement toggles | Custom layout: device list + volume sliders + toggle cards |
| **Affinity** | Process list with CPU affinity editor | Custom layout: process list + affinity checkbox matrix |
| **Startup** | Startup entries from Run keys and scheduled tasks | Custom layout: grouped entry list with enable/disable |

### Design Principles

- **WinUI 3 native controls only** — no third-party component library, no design system tokens.
- **TweakList reuse** — Network offload toggles use the existing `TweakList` control and `TweakCatalog` pattern.
- **Custom layouts** — Sound, Affinity, and Startup use purpose-built XAML layouts with `Grid`, `StackPanel`, `Border`, and `ScrollViewer`.
- **Card-based sections** — each page groups related controls into `Border` cards with `CardBackgroundFillColorDefaultBrush` background and `DividerStrokeColorDefaultBrush` border.
- **InfoBar for elevation** — pages that modify machine-wide settings show an `InfoBar` warning when not elevated.
- **Async data loading** — all service calls are async; pages show a `ProgressRing` while loading and handle errors gracefully.

### Focal Points

Each page declares one primary visual anchor:

| Page | Focal Point | Rationale |
|------|-------------|-----------|
| Network | TweakList of adapter offload toggles | Primary action — most users come here to toggle offloads |
| Sound | Audio device list | Primary action — volume control is the most common task |
| Affinity | Process list | Primary action — selecting a process is the entry point |
| Startup | Run key entries list | Primary action — most startup entries are Run key items |

### Accent Color

Accent color is reserved for: active NavigationView item, focus rings, selected toggle state, primary CTA hover state. No destructive actions exist in this phase.

---

## 2. Page Specifications

### 2.1 Network Page

**File:** `src/VainTools.App/Features/Network/NetworkPage.xaml`  
**Code-behind:** `src/VainTools.App/Features/Network/NetworkPage.xaml.cs`  
**ViewModel:** `src/VainTools.App/ViewModels/NetworkViewModel.cs`  
**Service:** `src/VainTools.App/Services/NetworkService.cs` (new, wraps PowerShell `Get-NetAdapterBinding`)

#### Layout Structure

```
NetworkPage (Page)
└── Grid (Padding=24)
    ├── Row 0: Header
    │   ├── StackPanel
    │   │   ├── TextBlock "Network" (TitleTextBlockStyle)
    │   │   └── TextBlock subtitle (TextFillColorSecondaryBrush)
    │   └── StackPanel (right-aligned)
    │       └── Button "Refresh"
    ├── Row 1: InfoBar (elevation warning, conditional)
    ├── Row 2: Adapter Offload Toggles (TweakList)
    │   └── TweakList control
    │       └── ItemsSource = OffloadTweaks (from TweakCatalog)
    └── Row 3: DNS & NTP Settings (custom card)
        └── Border (card)
            └── StackPanel
                ├── TextBlock "DNS Servers" (SubtitleTextBlockStyle)
                ├── TextBox (primary DNS)
                ├── TextBox (secondary DNS)
                ├── TextBlock "NTP Server" (SubtitleTextBlockStyle)
                ├── TextBox (NTP server address)
                └── Button "Apply"
```

#### TweakList Integration

The Network page uses the existing `TweakList` control for adapter offload toggles. The `TweakCatalog` static class is extended with a new `Network` property:

```csharp
public static IReadOnlyList<RegistryTweak> Network { get; } =
[
    new()
    {
        Id = "network.offload-checksum-ipv4",
        Name = "IPv4 Checksum Offload",
        Description = "Offload IPv4 checksum calculation to the network adapter.",
        Hive = RegistryHive.LocalMachine,
        KeyPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
        ValueName = "DisableTaskOffload",
        EnabledValue = 0,
        DisabledValue = 1,
        RequiresAdmin = true,
    },
    // ... additional offload toggles
];
```

The `TweakList` control binds to `TweakCatalog.Network` and handles toggle state, admin badges, and Explorer-restart badges automatically.

#### DNS/NTP Card

The DNS/NTP section is a custom card below the TweakList. It uses:

- **TextBox** controls for DNS server addresses (primary, secondary) and NTP server.
- **Button "Apply"** to commit changes via `NetworkService`.
- **InfoBar** for validation errors (e.g., invalid IP address format).

#### Data Flow

1. `NetworkViewModel` calls `NetworkService.GetAdaptersAsync()` on initialization.
2. Adapter list populates the TweakList's `ItemsSource`.
3. DNS/NTP values load from registry via `NetworkService.GetDnsSettingsAsync()` and `NetworkService.GetNtpSettingsAsync()`.
4. Toggle changes write to registry via `RegistryTweakService` (existing).
5. DNS/NTP changes write to registry via `NetworkService.SetDnsSettingsAsync()` and `NetworkService.SetNtpSettingsAsync()`.

#### Copywriting

| Element | Text |
|---------|------|
| Title | "Network" |
| Subtitle | "Adapter offloads, DNS servers, and NTP configuration." |
| DNS section header | "DNS Servers" |
| DNS primary placeholder | "Primary DNS (e.g., 1.1.1.1)" |
| DNS secondary placeholder | "Secondary DNS (e.g., 8.8.8.8)" |
| NTP section header | "NTP Server" |
| NTP placeholder | "NTP server (e.g., time.windows.com)" |
| Apply button | "Apply Settings" |
| Refresh button | "Refresh" |

---

### 2.2 Sound Page

**File:** `src/VainTools.App/Features/Sound/SoundPage.xaml`  
**Code-behind:** `src/VainTools.App/Features/Sound/SoundPage.xaml.cs`  
**ViewModel:** `src/VainTools.App/ViewModels/SoundViewModel.cs`  
**Service:** `src/VainTools.App/Services/SoundService.cs` (new, wraps WASAPI COM + registry)

#### Layout Structure

```
SoundPage (Page)
└── Grid (Padding=24)
    ├── Row 0: Header
    │   ├── StackPanel
    │   │   ├── TextBlock "Sound" (TitleTextBlockStyle)
    │   │   └── TextBlock subtitle (TextFillColorSecondaryBrush)
    │   └── StackPanel (right-aligned)
    │       └── Button "Refresh"
    ├── Row 1: InfoBar (elevation warning, conditional)
    ├── Row 2: Audio Devices (custom card)
    │   └── Border (card)
    │       └── StackPanel
    │           ├── TextBlock "Audio Devices" (SubtitleTextBlockStyle)
    │           └── ItemsControl (DevicesHost)
    │               └── DataTemplate: AudioDeviceViewModel
    │                   └── Border (device card)
    │                       └── Grid
    │                           ├── Column 0: StackPanel
    │                           │   ├── TextBlock device name (SemiBold)
    │                           │   ├── TextBlock device type (TertiaryBrush)
    │                           │   └── ToggleSwitch "Default"
    │                           └── Column 1: StackPanel
    │                               ├── Slider (volume 0-100)
    │                               └── TextBlock volume percentage
    └── Row 3: Audio Enhancements (custom card)
        └── Border (card)
            └── StackPanel
                ├── TextBlock "Audio Enhancements" (SubtitleTextBlockStyle)
                └── ItemsControl (EnhancementsHost)
                    └── DataTemplate: TweakToggleViewModel
                        └── Border (toggle card)
                            └── Grid
                                ├── Column 0: StackPanel
                                │   ├── TextBlock name (SemiBold)
                                │   └── TextBlock description (12pt, 70% opacity)
                                └── Column 1: ToggleSwitch
```

#### Audio Device List

The device list uses an `ItemsControl` with a `DataTemplate` for `AudioDeviceViewModel`. Each device card shows:

- **Device name** (e.g., "Speakers (Realtek High Definition Audio)")
- **Device type** (e.g., "Render" or "Capture")
- **Volume slider** (0–100, bound to `Volume` property)
- **Volume percentage** text (e.g., "75%")
- **"Default" toggle** (sets as default audio device)

The `SoundService` uses WASAPI COM interfaces (`IMMDeviceEnumerator`, `IAudioEndpointVolume`) for device enumeration and volume control.

#### Audio Enhancements Toggles

The enhancements section uses an `ItemsControl` with a `DataTemplate` for `TweakToggleViewModel`, matching the TweakList card pattern. Toggles are sourced from `TweakCatalog.Sound`:

```csharp
public static IReadOnlyList<RegistryTweak> Sound { get; } =
[
    new()
    {
        Id = "sound.spatial-audio",
        Name = "Spatial Audio",
        Description = "Enable spatial audio for headphones and speakers.",
        Hive = RegistryHive.LocalMachine,
        KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio",
        ValueName = "DisableSpatialAudio",
        EnabledValue = 0,
        DisabledValue = 1,
        RequiresAdmin = true,
    },
    new()
    {
        Id = "sound.audio-enhancements",
        Name = "Audio Enhancements",
        Description = "Enable Windows audio enhancements like bass boost and virtualization.",
        Hive = RegistryHive.LocalMachine,
        KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio",
        ValueName = "DisableAudioEnhancements",
        EnabledValue = 0,
        DisabledValue = 1,
        RequiresAdmin = true,
    },
];
```

#### Data Flow

1. `SoundViewModel` calls `SoundService.GetDevicesAsync()` on initialization.
2. Device list populates the `ItemsControl`.
3. Volume slider changes call `SoundService.SetVolumeAsync(deviceId, volume)`.
4. "Default" toggle calls `SoundService.SetDefaultDeviceAsync(deviceId)`.
5. Enhancement toggles read/write registry via `RegistryTweakService`.

#### Copywriting

| Element | Text |
|---------|------|
| Title | "Sound" |
| Subtitle | "Audio devices, volume mixer, spatial audio and enhancements." |
| Devices section header | "Audio Devices" |
| Enhancements section header | "Audio Enhancements" |
| Default toggle label | "Default" |
| Volume label | "Volume" |
| Refresh button | "Refresh" |

---

### 2.3 Affinity Page

**File:** `src/VainTools.App/Features/Affinity/AffinityPage.xaml`  
**Code-behind:** `src/VainTools.App/Features/Affinity/AffinityPage.xaml.cs`  
**ViewModel:** `src/VainTools.App/ViewModels/AffinityViewModel.cs`  
**Service:** `src/VainTools.App/Services/AffinityService.cs` (new, wraps P/Invoke `SetProcessAffinityMask` / `GetProcessAffinityMask`)

#### Layout Structure

```
AffinityPage (Page)
└── Grid (Padding=24)
    ├── Row 0: Header
    │   ├── StackPanel
    │   │   ├── TextBlock "Affinity" (TitleTextBlockStyle)
    │   │   └── TextBlock subtitle (TextFillColorSecondaryBrush)
    │   └── StackPanel (right-aligned)
    │       └── Button "Refresh"
    ├── Row 1: InfoBar (elevation warning, conditional)
    ├── Row 2: Process List (custom card)
    │   └── Border (card)
    │       └── StackPanel
    │           ├── TextBlock "Running Processes" (SubtitleTextBlockStyle)
    │           └── ItemsControl (ProcessesHost)
    │               └── DataTemplate: ProcessViewModel
    │                   └── Border (process card)
    │                       └── Grid
    │                           ├── Column 0: StackPanel
    │                           │   ├── TextBlock process name (SemiBold)
    │                           │   └── TextBlock PID + CPU count (TertiaryBrush)
    │                           └── Column 1: Button "Edit Affinity"
    └── Row 3: Affinity Editor (custom card, visible when process selected)
        └── Border (card)
            └── StackPanel
                ├── TextBlock "CPU Affinity — {ProcessName}" (SubtitleTextBlockStyle)
                ├── TextBlock "Select which CPUs this process can use." (SecondaryBrush)
                └── ItemsControl (CpusHost)
                    └── DataTemplate: CpuAffinityViewModel
                        └── Border (CPU card)
                            └── Grid
                                ├── Column 0: StackPanel
                                │   ├── TextBlock "CPU {Index}" (SemiBold)
                                │   └── TextBlock "Core {CoreIndex}" (TertiaryBrush)
                                └── Column 1: CheckBox "Enabled"
```

#### Process List

The process list uses an `ItemsControl` with a `DataTemplate` for `ProcessViewModel`. Each process card shows:

- **Process name** (e.g., "chrome.exe")
- **PID and CPU count** (e.g., "PID 1234 · 16 CPUs")
- **"Edit Affinity" button** — opens the affinity editor for that process

The `AffinityService` uses `Process.GetProcesses()` to enumerate running processes and `GetProcessAffinityMask` P/Invoke to read current affinity.

#### Affinity Editor

The affinity editor appears when a process is selected. It shows:

- **Process name** in the section header.
- **CPU checkboxes** — one per CPU core, bound to `IsEnabled`.
- **"Apply" button** — commits the new affinity mask via `SetProcessAffinityMask`.

The editor uses a `WrapPanel` or `Grid` with dynamic column count to display CPU checkboxes in a compact layout.

#### Data Flow

1. `AffinityViewModel` calls `AffinityService.GetProcessesAsync()` on initialization.
2. Process list populates the `ItemsControl`.
3. "Edit Affinity" button selects a process and shows the affinity editor.
4. `AffinityService.GetAffinityMaskAsync(processId)` returns the current affinity mask.
5. CPU checkboxes bind to the mask bits.
6. "Apply" button calls `AffinityService.SetAffinityMaskAsync(processId, newMask)`.

#### Copywriting

| Element | Text |
|---------|------|
| Title | "Affinity" |
| Subtitle | "CPU affinity and ideal-processor sets for running processes." |
| Process section header | "Running Processes" |
| Edit button | "Edit Affinity" |
| Editor section header | "CPU Affinity — {ProcessName}" |
| Editor description | "Select which CPUs this process can use." |
| CPU label | "CPU {Index}" |
| Core label | "Core {CoreIndex}" |
| Apply button | "Apply Settings" |
| Refresh button | "Refresh" |

---

### 2.4 Startup Page

**File:** `src/VainTools.App/Features/Startup/StartupPage.xaml`  
**Code-behind:** `src/VainTools.App/Features/Startup/StartupPage.xaml.cs`  
**ViewModel:** `src/VainTools.App/ViewModels/StartupViewModel.cs`  
**Service:** `src/VainTools.App/Services/StartupService.cs` (new, wraps registry Run keys + WMI scheduled tasks)

#### Layout Structure

```
StartupPage (Page)
└── Grid (Padding=24)
    ├── Row 0: Header
    │   ├── StackPanel
    │   │   ├── TextBlock "Startup" (TitleTextBlockStyle)
    │   │   └── TextBlock subtitle (TextFillColorSecondaryBrush)
    │   └── StackPanel (right-aligned)
    │       └── Button "Refresh"
    ├── Row 1: InfoBar (elevation warning, conditional)
    ├── Row 2: Run Key Entries (custom card)
    │   └── Border (card)
    │       └── StackPanel
    │           ├── TextBlock "Run Keys" (SubtitleTextBlockStyle)
    │           └── ItemsControl (RunKeyHost)
    │               └── DataTemplate: StartupEntryViewModel
    │                   └── Border (entry card)
    │                       └── Grid
    │                           ├── Column 0: StackPanel
    │                           │   ├── TextBlock entry name (SemiBold)
    │                           │   ├── TextBlock command path (TertiaryBrush, 12pt)
    │                           │   └── TextBlock source hive (TertiaryBrush, 11pt)
    │                           └── Column 1: ToggleSwitch "Enabled"
    └── Row 3: Scheduled Tasks (custom card)
        └── Border (card)
            └── StackPanel
                ├── TextBlock "Scheduled Tasks" (SubtitleTextBlockStyle)
                └── ItemsControl (ScheduledTaskHost)
                    └── DataTemplate: StartupEntryViewModel
                        └── Border (entry card)
                            └── Grid
                                ├── Column 0: StackPanel
                                │   ├── TextBlock task name (SemiBold)
                                │   ├── TextBlock task path (TertiaryBrush, 12pt)
                                │   └── TextBlock "Scheduled Task" (TertiaryBrush, 11pt)
                                └── Column 1: ToggleSwitch "Enabled"
```

#### Run Key Entries

The Run key section lists entries from `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` and `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`. Each entry card shows:

- **Entry name** (e.g., "OneDrive")
- **Command path** (e.g., "C:\Users\...\OneDrive.exe /background")
- **Source hive** (e.g., "HKCU" or "HKLM")
- **"Enabled" toggle** — disables by renaming the value with a `-` prefix (e.g., `-OneDrive`)

The `StartupService` reads Run key values via `RegistryKey.GetValue()` and toggles by writing/deleting the renamed value.

#### Scheduled Tasks

The scheduled task section lists tasks from the Windows Task Scheduler that run at startup. Each entry card shows:

- **Task name** (e.g., "MicrosoftEdgeUpdateTaskMachineUA")
- **Task path** (e.g., "\Microsoft\EdgeUpdate\")
- **"Scheduled Task" label**
- **"Enabled" toggle** — enables/disables the task via WMI.

The `StartupService` queries scheduled tasks via `ManagementObjectSearcher` with a WQL query for tasks with startup triggers.

#### Data Flow

1. `StartupViewModel` calls `StartupService.GetRunKeyEntriesAsync()` and `StartupService.GetScheduledTasksAsync()` on initialization.
2. Run key entries populate the first `ItemsControl`.
3. Scheduled tasks populate the second `ItemsControl`.
4. Toggle changes call `StartupService.ToggleRunKeyEntryAsync(entry, isEnabled)` or `StartupService.ToggleScheduledTaskAsync(task, isEnabled)`.

#### Copywriting

| Element | Text |
|---------|------|
| Title | "Startup" |
| Subtitle | "Startup entries from Run keys and scheduled tasks." |
| Run key section header | "Run Keys" |
| Scheduled task section header | "Scheduled Tasks" |
| Run key source label | "HKCU" or "HKLM" |
| Scheduled task source label | "Scheduled Task" |
| Enabled toggle label | "Enabled" |
| Refresh button | "Refresh" |

---

## 3. Shared UI Patterns

### 3.1 Page Header

All four pages use the same header pattern:

```xml
<Grid Grid.Row="0">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
    </Grid.ColumnDefinitions>
    <StackPanel Grid.Column="0">
        <TextBlock Text="{Title}" Style="{StaticResource TitleTextBlockStyle}" />
        <TextBlock Text="{Subtitle}"
                   Margin="0,4,0,0"
                   Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
    </StackPanel>
    <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8" VerticalAlignment="Top">
        <Button Content="Refresh" Click="OnRefreshClick" />
    </StackPanel>
</Grid>
```

### 3.2 Elevation InfoBar

Pages that modify machine-wide settings show an `InfoBar` when not elevated:

```xml
<InfoBar x:Name="ElevationBar"
         Grid.Row="1"
         Margin="0,16,0,0"
         IsOpen="False"
         IsClosable="False"
         Severity="Warning"
         Title="Administrator rights required"
         Message="Some settings on this page write to machine-wide keys. Run Vain Tools as administrator to change them." />
```

The `InfoBar.Visibility` is bound to `IsElevated` from the service (inverted).

### 3.3 Card Container

All custom sections use the same card container:

```xml
<Border Margin="0,16,0,0"
        Padding="16"
        CornerRadius="8"
        VerticalAlignment="Top"
        Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        BorderBrush="{ThemeResource DividerStrokeColorDefaultBrush}"
        BorderThickness="1">
    <!-- Section content -->
</Border>
```

### 3.4 Loading State

While async data loads, pages show a `ProgressRing`:

```xml
<ProgressRing x:Name="LoadingRing"
              Grid.Row="2"
              Width="32" Height="32"
              IsActive="True"
              Visibility="{x:Bind IsLoading, Mode=OneWay}" />
```

### 3.5 Empty State

When a list has no items, pages show a placeholder `TextBlock` with page-specific actionable copy:

```xml
<!-- Network: "No network adapters found. Check that your network drivers are installed." -->
<!-- Sound: "No audio devices found. Check that your audio drivers are installed." -->
<!-- Affinity: "No running processes found. This is unusual — try refreshing." -->
<!-- Startup: "No startup entries found. Your system may have a clean startup configuration." -->
<TextBlock Text="{EmptyStateMessage}"
           Foreground="{ThemeResource TextFillColorTertiaryBrush}"
           Visibility="{x:Bind HasItems, Mode=OneWay, Converter={StaticResource InverseBoolToVisibilityConverter}}" />
```

### 3.6 Error State

When a service call fails, pages show an `InfoBar` with error details and a solution path:

```xml
<InfoBar x:Name="ErrorBar"
         Grid.Row="1"
         Margin="0,16,0,0"
         IsOpen="False"
         IsClosable="True"
         Severity="Error"
         Title="Error"
         Message="{x:Bind ErrorMessage, Mode=OneWay}" />
```

Error message copy includes the problem and the solution path:
- Network: "Failed to load network adapters. Ensure the Network Adapter service is running."
- Sound: "Failed to load audio devices. Ensure the Windows Audio service is running."
- Affinity: "Failed to load process list. Ensure you have permission to enumerate processes."
- Startup: "Failed to load startup entries. Ensure you have permission to read the registry."

---

## 4. ViewModel Specifications

### 4.1 NetworkViewModel

| Property | Type | Description |
|----------|------|-------------|
| `OffloadTweaks` | `IReadOnlyList<TweakToggleViewModel>` | Adapter offload toggles from TweakCatalog |
| `PrimaryDns` | `string` | Primary DNS server address |
| `SecondaryDns` | `string` | Secondary DNS server address |
| `NtpServer` | `string` | NTP server address |
| `IsLoading` | `bool` | Loading state |
| `IsElevated` | `bool` | Whether running as admin |
| `ErrorMessage` | `string` | Error message (if any) |

| Method | Description |
|--------|-------------|
| `LoadAsync()` | Load adapter list, DNS, and NTP settings |
| `ApplyDnsAsync()` | Write DNS settings to registry |
| `ApplyNtpAsync()` | Write NTP settings to registry |
| `RefreshAsync()` | Reload all data |

### 4.2 SoundViewModel

| Property | Type | Description |
|----------|------|-------------|
| `Devices` | `IReadOnlyList<AudioDeviceViewModel>` | Audio devices from WASAPI |
| `Enhancements` | `IReadOnlyList<TweakToggleViewModel>` | Audio enhancement toggles |
| `IsLoading` | `bool` | Loading state |
| `IsElevated` | `bool` | Whether running as admin |
| `ErrorMessage` | `string` | Error message (if any) |

| Method | Description |
|--------|-------------|
| `LoadAsync()` | Load audio devices and enhancement toggles |
| `SetVolumeAsync(deviceId, volume)` | Set device volume |
| `SetDefaultDeviceAsync(deviceId)` | Set default audio device |
| `RefreshAsync()` | Reload all data |

### 4.3 AffinityViewModel

| Property | Type | Description |
|----------|------|-------------|
| `Processes` | `IReadOnlyList<ProcessViewModel>` | Running processes |
| `SelectedProcess` | `ProcessViewModel?` | Currently selected process |
| `Cpus` | `IReadOnlyList<CpuAffinityViewModel>` | CPU affinity checkboxes |
| `IsLoading` | `bool` | Loading state |
| `IsElevated` | `bool` | Whether running as admin |
| `ErrorMessage` | `string` | Error message (if any) |

| Method | Description |
|--------|-------------|
| `LoadAsync()` | Load process list |
| `SelectProcessAsync(process)` | Select a process and load its affinity |
| `ApplyAffinityAsync()` | Write new affinity mask |
| `RefreshAsync()` | Reload all data |

### 4.4 StartupViewModel

| Property | Type | Description |
|----------|------|-------------|
| `RunKeyEntries` | `IReadOnlyList<StartupEntryViewModel>` | Run key startup entries |
| `ScheduledTasks` | `IReadOnlyList<StartupEntryViewModel>` | Scheduled task startup entries |
| `IsLoading` | `bool` | Loading state |
| `IsElevated` | `bool` | Whether running as admin |
| `ErrorMessage` | `string` | Error message (if any) |

| Method | Description |
|--------|-------------|
| `LoadAsync()` | Load Run key entries and scheduled tasks |
| `ToggleRunKeyEntryAsync(entry, isEnabled)` | Enable/disable a Run key entry |
| `ToggleScheduledTaskAsync(task, isEnabled)` | Enable/disable a scheduled task |
| `RefreshAsync()` | Reload all data |

---

## 5. Service Specifications

### 5.1 NetworkService (New)

**Interface:** `INetworkService`  
**Implementation:** `NetworkService`  
**Dependencies:** `IProcessRunner`, `ILogger<NetworkService>`

| Method | Description |
|--------|-------------|
| `GetAdaptersAsync()` | Enumerate network adapters via `Get-NetAdapterBinding` |
| `GetDnsSettingsAsync()` | Read DNS server addresses from registry |
| `SetDnsSettingsAsync(primary, secondary)` | Write DNS server addresses to registry |
| `GetNtpSettingsAsync()` | Read NTP server from registry |
| `SetNtpSettingsAsync(server)` | Write NTP server to registry |

### 5.2 SoundService (New)

**Interface:** `ISoundService`  
**Implementation:** `SoundService`  
**Dependencies:** `ILogger<SoundService>`

| Method | Description |
|--------|-------------|
| `GetDevicesAsync()` | Enumerate audio devices via WASAPI COM |
| `SetVolumeAsync(deviceId, volume)` | Set device volume (0.0–1.0) |
| `SetDefaultDeviceAsync(deviceId)` | Set default audio device |
| `GetVolumeAsync(deviceId)` | Get current device volume |

### 5.3 AffinityService (New)

**Interface:** `IAffinityService`  
**Implementation:** `AffinityService`  
**Dependencies:** `ILogger<AffinityService>`

| Method | Description |
|--------|-------------|
| `GetProcessesAsync()` | Enumerate running processes |
| `GetAffinityMaskAsync(processId)` | Get current affinity mask via P/Invoke |
| `SetAffinityMaskAsync(processId, mask)` | Set affinity mask via P/Invoke |
| `GetCpuCountAsync()` | Get total CPU core count |

### 5.4 StartupService (New)

**Interface:** `IStartupService`  
**Implementation:** `StartupService`  
**Dependencies:** `ILogger<StartupService>`

| Method | Description |
|--------|-------------|
| `GetRunKeyEntriesAsync()` | Read Run key entries from HKCU and HKLM |
| `ToggleRunKeyEntryAsync(entry, isEnabled)` | Enable/disable a Run key entry |
| `GetScheduledTasksAsync()` | Query scheduled tasks via WMI |
| `ToggleScheduledTaskAsync(task, isEnabled)` | Enable/disable a scheduled task |

---

## 6. Model Specifications

### 6.1 AudioDeviceViewModel

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | WASAPI device ID |
| `Name` | `string` | Device display name |
| `Type` | `string` | "Render" or "Capture" |
| `Volume` | `double` | Current volume (0.0–1.0) |
| `IsDefault` | `bool` | Whether this is the default device |

### 6.2 ProcessViewModel

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Process ID |
| `Name` | `string` | Process name (e.g., "chrome.exe") |
| `CpuCount` | `int` | Number of CPUs in current affinity mask |

### 6.3 CpuAffinityViewModel

| Property | Type | Description |
|----------|------|-------------|
| `Index` | `int` | CPU index (0-based) |
| `CoreIndex` | `int` | Core index |
| `IsEnabled` | `bool` | Whether this CPU is in the affinity mask |

### 6.4 StartupEntryViewModel

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Entry name |
| `Command` | `string` | Command path or arguments |
| `Source` | `string` | "HKCU", "HKLM", or "Scheduled Task" |
| `IsEnabled` | `bool` | Whether the entry is enabled |

---

## 7. Navigation Integration

The four pages are registered in `App.xaml` (or `AppShell.xaml`) as NavigationView items:

```xml
<muxc:NavigationViewItem Content="Network" Icon="Network" Tag="network" />
<muxc:NavigationViewItem Content="Sound" Icon="Volume" Tag="sound" />
<muxc:NavigationViewItem Content="Affinity" Icon="Processor" Tag="affinity" />
<muxc:NavigationViewItem Content="Startup" Icon="Play" Tag="startup" />
```

Each page is registered in the navigation service:

```csharp
navigationService.Register("network", typeof(NetworkPage));
navigationService.Register("sound", typeof(SoundPage));
navigationService.Register("affinity", typeof(AffinityPage));
navigationService.Register("startup", typeof(StartupPage));
```

---

## 8. Accessibility

- All interactive controls have `AutomationProperties.Name` set.
- Toggle switches have `OnContent` and `OffContent` set to empty strings (visual state is clear).
- Sliders have `AutomationProperties.Name` set to the section label.
- InfoBars have `AutomationProperties.Name` set to the title.
- Buttons have `AutomationProperties.Name` set to the button text.

---

## 9. Testing Notes

- **Network page:** Verify TweakList renders offload toggles correctly. Verify DNS/NTP textboxes populate from registry. Verify "Apply" writes to registry.
- **Sound page:** Verify device list populates from WASAPI. Verify volume slider changes device volume. Verify "Default" toggle sets default device. Verify enhancement toggles read/write registry.
- **Affinity page:** Verify process list populates. Verify "Edit Affinity" shows CPU checkboxes. Verify "Apply" sets affinity mask.
- **Startup page:** Verify Run key entries populate from registry. Verify scheduled tasks populate from WMI. Verify toggles enable/disable entries.

---

## 10. Open Questions

1. **WASAPI COM interop:** Should we use a wrapper library (e.g., `NAudio`) or raw COM interop? Raw COM is preferred to avoid external dependencies.
2. **Affinity editor layout:** Should CPU checkboxes use a `WrapPanel` or a `Grid` with dynamic columns? `WrapPanel` is simpler but may overflow on high-core-count systems.
3. **Startup entry disabling:** Should disabled Run key entries be renamed with a `-` prefix or moved to a separate "Disabled" key? Renaming is simpler but may confuse users.
4. **Scheduled task WMI query:** Should we filter for tasks with startup triggers only, or all tasks? Startup triggers only is more relevant but may miss some entries.

---

## 11. UI Considerations

> Populated by the ui-phase UI-consideration probe (Step 9.5) and lifted by plan-phase's
> `## UI Considerations` lift rule via the identical rule as SPEC `## Edge Coverage`. Shape-rooted UI *state*
> coverage (empty / loading / error / populated / partial / overflow / zero-one-many / long-text).
> Empty-state and error-state COPY live in `## Copywriting Contract` above — this section covers
> state coverage and REFERENCES those rows rather than restating the copy (de-dup).

Applicable state considerations resolved: 54 covered, 0 backstop, 0 unresolved

| Category | Element(s) | Status | Resolution / Reason |
|----------|------------|--------|---------------------|
| loading | E1 (Network TweakList) | ✅ covered | ProgressRing shown while adapter list loads from PowerShell |
| error | E1 (Network TweakList) | ✅ covered | InfoBar shows "Failed to load network adapters. Ensure the Network Adapter service is running." |
| long-text | E1 (Network TweakList) | ✅ covered | Toggle name wraps to 2 lines; description wraps to 3 lines with ellipsis |
| empty | E2 (DNS/NTP card) | ✅ covered | TextBoxes show placeholder text when no DNS/NTP values are configured |
| loading | E2 (DNS/NTP card) | ✅ covered | ProgressRing shown while DNS/NTP values load from registry |
| error | E2 (DNS/NTP card) | ✅ covered | InfoBar shows "Failed to load DNS settings. Ensure you have permission to read the registry." |
| populated | E2 (DNS/NTP card) | ✅ covered | TextBoxes display current DNS/NTP values from registry |
| partial | E2 (DNS/NTP card) | ✅ covered | Fields with values are shown; empty fields show placeholder text |
| overflow | E2 (DNS/NTP card) | ✅ covered | Card is scrollable when content exceeds visible area |
| zero-one-many | E2 (DNS/NTP card) | ✅ covered | Handles 0, 1, or multiple DNS/NTP entries |
| long-text | E2 (DNS/NTP card) | ✅ covered | DNS/NTP values truncate with ellipsis in textboxes |
| empty | E3 (Audio device list) | ✅ covered | "No audio devices found. Check that your audio drivers are installed." |
| loading | E3 (Audio device list) | ✅ covered | ProgressRing shown while WASAPI enumerates devices |
| error | E3 (Audio device list) | ✅ covered | InfoBar shows "Failed to load audio devices. Ensure the Windows Audio service is running." |
| populated | E3 (Audio device list) | ✅ covered | Device list renders all audio devices with name, type, volume slider, Default toggle |
| partial | E3 (Audio device list) | ✅ covered | Devices successfully enumerated are shown; failed enumeration shows error InfoBar |
| overflow | E3 (Audio device list) | ✅ covered | Device list is scrollable when device count exceeds visible area |
| zero-one-many | E3 (Audio device list) | ✅ covered | "No devices" / "1 device" / "N devices" — list handles all counts |
| long-text | E3 (Audio device list) | ✅ covered | Device name truncates with ellipsis in list |
| empty | E4 (Enhancement toggles) | ✅ covered | "No audio enhancements available." when registry returns zero toggles |
| loading | E4 (Enhancement toggles) | ✅ covered | ProgressRing shown while enhancement toggles load from registry |
| error | E4 (Enhancement toggles) | ✅ covered | InfoBar shows "Failed to load audio enhancements. Ensure the Windows Audio service is running." |
| populated | E4 (Enhancement toggles) | ✅ covered | Toggle list renders all enhancement toggles with ToggleSwitch + Name + Description |
| partial | E4 (Enhancement toggles) | ✅ covered | Tweaks successfully read are shown; failed reads are omitted |
| overflow | E4 (Enhancement toggles) | ✅ covered | Toggle list is scrollable when count exceeds visible area |
| zero-one-many | E4 (Enhancement toggles) | ✅ covered | "No enhancements" / "1 enhancement" / "N enhancements" |
| long-text | E4 (Enhancement toggles) | ✅ covered | Toggle name wraps to 2 lines; description wraps to 3 lines |
| empty | E5 (Process list) | ✅ covered | "No running processes found. This is unusual — try refreshing." |
| loading | E5 (Process list) | ✅ covered | ProgressRing shown while process list enumerates |
| error | E5 (Process list) | ✅ covered | InfoBar shows "Failed to load process list. Ensure you have permission to enumerate processes." |
| populated | E5 (Process list) | ✅ covered | Process list renders all running processes with name, PID, CPU count, Edit button |
| partial | E5 (Process list) | ✅ covered | Processes successfully enumerated are shown; failed enumeration shows error |
| overflow | E5 (Process list) | ✅ covered | Process list is scrollable when count exceeds visible area |
| zero-one-many | E5 (Process list) | ✅ covered | "No processes" / "1 process" / "N processes" |
| long-text | E5 (Process list) | ✅ covered | Process name truncates with ellipsis in list |
| loading | E6 (Affinity editor) | ✅ covered | ProgressRing shown while affinity mask loads via P/Invoke |
| error | E6 (Affinity editor) | ✅ covered | InfoBar shows "Failed to load affinity mask. Ensure you have permission to read process affinity." |
| long-text | E6 (Affinity editor) | ✅ covered | Process name in header truncates with ellipsis |
| empty | E7 (Run key entries) | ✅ covered | "No startup entries found. Your system may have a clean startup configuration." |
| loading | E7 (Run key entries) | ✅ covered | ProgressRing shown while Run key entries load from registry |
| error | E7 (Run key entries) | ✅ covered | InfoBar shows "Failed to load startup entries. Ensure you have permission to read the registry." |
| populated | E7 (Run key entries) | ✅ covered | Entry list renders all Run key entries with name, command, source, Enabled toggle |
| partial | E7 (Run key entries) | ✅ covered | Entries successfully read are shown; failed reads are omitted |
| overflow | E7 (Run key entries) | ✅ covered | Entry list is scrollable when count exceeds visible area |
| zero-one-many | E7 (Run key entries) | ✅ covered | "No entries" / "1 entry" / "N entries" |
| long-text | E7 (Run key entries) | ✅ covered | Entry name and command path truncate with ellipsis |
| empty | E8 (Scheduled tasks) | ✅ covered | "No scheduled tasks found. Your system may have a clean startup configuration." |
| loading | E8 (Scheduled tasks) | ✅ covered | ProgressRing shown while WMI queries scheduled tasks |
| error | E8 (Scheduled tasks) | ✅ covered | InfoBar shows "Failed to load scheduled tasks. Ensure you have permission to query WMI." |
| populated | E8 (Scheduled tasks) | ✅ covered | Task list renders all scheduled tasks with name, path, Enabled toggle |
| partial | E8 (Scheduled tasks) | ✅ covered | Tasks successfully enumerated are shown; failed enumeration shows error |
| overflow | E8 (Scheduled tasks) | ✅ covered | Task list is scrollable when count exceeds visible area |
| zero-one-many | E8 (Scheduled tasks) | ✅ covered | "No tasks" / "1 task" / "N tasks" |
| long-text | E8 (Scheduled tasks) | ✅ covered | Task name and path truncate with ellipsis |

<!-- Status vocabulary (locked by probe-core projectTruths):
     ✅ covered   → a plain truth string lifted into must_haves.truths
     🧪 backstop  → a flat scalar { statement, verification: backstop }; at verify time, no explicit
                    evidence → insufficient_spec → human_needed (never a silent pass, #1154)
     ⚠ unresolved → an explicit planner assumption (surfaced, never silently dropped)
     Rows are REPLACED (not appended) on a probe re-run — idempotent. -->

---

*End of Phase 5 UI Design Contract*
