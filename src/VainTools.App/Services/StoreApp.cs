namespace VainTools.App.Services;

/// <summary>
/// An installable application reported by <c>winget search</c>.
/// </summary>
/// <param name="Name">Display name, the only field that may contain spaces.</param>
/// <param name="Id">Package id used by <c>winget install --id</c>.</param>
/// <param name="Version">
/// Version winget reports. The msstore source can report <c>Unknown</c>, so this is
/// never parsed as a number.
/// </param>
/// <param name="Source">Source that matched (for example <c>winget</c> or <c>msstore</c>).</param>
public sealed record StoreApp(string Name, string Id, string Version, string Source);
