using System;
using System.Threading.Tasks;
using System.Windows;

namespace VainTools.Services;

/// <summary>
/// Interface for screenshot functionality.
/// </summary>
public interface IScreenshotService
{
    /// <summary>
    /// Captures a region of the screen.
    /// </summary>
    /// <param name="x">The x coordinate of the top-left corner.</param>
    /// <param name="y">The y coordinate of the top-left corner.</param>
    /// <param name="width">The width of the region.</param>
    /// <param name="height">The height of the region.</param>
    /// <returns>The captured image data.</returns>
    Task<byte[]> CaptureRegionAsync(int x, int y, int width, int height);

    /// <summary>
    /// Captures the full screen.
    /// </summary>
    /// <returns>The captured image data.</returns>
    Task<byte[]> CaptureFullScreenAsync();

    /// <summary>
    /// Gets the current output path for screenshots.
    /// </summary>
    Task<string> GetOutputPathAsync();

    /// <summary>
    /// Sets the output path for screenshots.
    /// </summary>
    /// <param name="path">The path where screenshots will be saved.</param>
    Task SetOutputPathAsync(string path);

    /// <summary>
    /// Gets the current output format.
    /// </summary>
    /// <returns>The file extension (e.g., ".png", ".jpg").</returns>
    Task<string> GetOutputFormatAsync();

    /// <summary>
    /// Sets the output format for screenshots.
    /// </summary>
    /// <param name="format">The file extension (e.g., ".png", ".jpg").</param>
    Task SetOutputFormatAsync(string format);

    /// <summary>
    /// Saves the screenshot data to a file.
    /// </summary>
    /// <param name="imageData">The image data to save.</param>
    /// <param name="fileName">The file name (optional, generates one if not provided).</param>
    /// <returns>The full path to the saved file.</returns>
    Task<string> SaveScreenshotAsync(byte[] imageData, string? fileName = null);
}