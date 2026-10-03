using System.Xml.Serialization;

namespace VainTools.App.Models;

/// <summary>
/// The value types a <see cref="VainProfileSetting"/> can carry.
/// These strings are the exact values written by Vain Toolbox.
/// </summary>
public enum VainValueType
{
    Unknown = 0,
    Dword,
    Binary,
    AnsiString,
}

/// <summary>
/// A single NVIDIA driver setting inside a <see cref="VainProfile"/>.
/// </summary>
public sealed class VainProfileSetting
{
    /// <summary>Human-readable setting name, e.g. "Low Latency Mode".</summary>
    [XmlElement("SettingNameInfo")]
    public string SettingNameInfo { get; set; } = string.Empty;

    /// <summary>NVIDIA DRS setting id (hex, e.g. "0x0000F00D").</summary>
    [XmlElement("SettingID")]
    public string SettingId { get; set; } = string.Empty;

    /// <summary>The value as written by the driver settings API.</summary>
    [XmlElement("SettingValue")]
    public string SettingValue { get; set; } = string.Empty;

    /// <summary>
    /// Value type as a raw string, kept verbatim so an unknown type round-trips
    /// instead of being silently dropped.
    /// </summary>
    [XmlElement("ValueType")]
    public string ValueTypeRaw { get; set; } = string.Empty;

    [XmlIgnore]
    public VainValueType ValueType =>
        Enum.TryParse<VainValueType>(ValueTypeRaw, ignoreCase: true, out var parsed)
            ? parsed
            : VainValueType.Unknown;
}

/// <summary>
/// One profile inside a <c>.vain</c> export.
/// </summary>
public sealed class VainProfile
{
    /// <summary>
    /// Executables this profile applies to. The element name is misspelled
    /// ("Executeables") in the real format and must stay that way to stay compatible.
    /// </summary>
    [XmlArray("Executeables")]
    [XmlArrayItem("string")]
    public List<string> Executables { get; set; } = new();

    [XmlArray("Settings")]
    [XmlArrayItem("ProfileSetting")]
    public List<VainProfileSetting> Settings { get; set; } = new();
}

/// <summary>
/// The root of a <c>.vain</c> file: an array of profiles.
/// </summary>
[XmlRoot("ArrayOfProfile", Namespace = "")]
public sealed class VainProfileDocument
{
    [XmlElement("Profile")]
    public List<VainProfile> Profiles { get; set; } = new();

    /// <summary>Total settings across every profile.</summary>
    [XmlIgnore]
    public int TotalSettings => Profiles.Sum(p => p.Settings.Count);

    /// <summary>Settings whose value type is importable as a DWORD.</summary>
    [XmlIgnore]
    public int DwordSettingCount =>
        Profiles.Sum(p => p.Settings.Count(s => s.ValueType == VainValueType.Dword));
}
