using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;

namespace VainTools.Modules;

public static class ScreenshotsModule
{
    private static readonly string ScreenshotsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "VainScreenshots");

    static ScreenshotsModule()
    {
        if (!Directory.Exists(ScreenshotsDir))
            Directory.CreateDirectory(ScreenshotsDir);
    }

    public static string CaptureScreenshot()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var filename = $"screenshot_{timestamp}.png";
        var path = Path.Combine(ScreenshotsDir, filename);

        // TODO: Implement actual screenshot capture using Win32 API
        // For now, create a placeholder file
        File.WriteAllText(path, "placeholder");

        return path;
    }

    public static List<string> GetScreenshots()
    {
        if (!Directory.Exists(ScreenshotsDir))
            return new List<string>();

        return Directory.GetFiles(ScreenshotsDir, "*.png")
            .OrderByDescending(f => File.GetCreationTime(f))
            .ToList();
    }

    public static void DeleteScreenshot(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}