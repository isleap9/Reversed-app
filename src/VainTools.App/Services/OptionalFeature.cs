namespace VainTools.App.Services;

/// <summary>
/// A Windows optional feature reported by DISM.
/// </summary>
public sealed record OptionalFeature(string Name, string State)
{
    /// <summary>
    /// True only when DISM reports the plain "Enabled" state. Intermediate states
    /// ("EnablePending", "DisabledWithPayloadRemoved") are intentionally NOT treated as
    /// enabled, so the UI never claims a change landed before DISM confirms it.
    /// </summary>
    public bool IsEnabled => string.Equals(State, "Enabled", StringComparison.OrdinalIgnoreCase);
}
