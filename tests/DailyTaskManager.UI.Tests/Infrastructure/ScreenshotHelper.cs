using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;

namespace DailyTaskManager.UI.Tests.Infrastructure;

/// <summary>
/// Simplified screenshot capture utility for UI tests using System.Drawing only
/// </summary>
public static class ScreenshotHelper
{
    #region Win32 API for Screenshot Capture

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const uint SRCCOPY = 0x00CC0020;

    #endregion

    /// <summary>
    /// Captures a screenshot of the entire screen
    /// </summary>
    public static Bitmap CaptureScreen()
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        return CaptureScreenRegion(bounds);
    }

    /// <summary>
    /// Captures a screenshot of a specific region of the screen
    /// </summary>
    public static Bitmap CaptureScreenRegion(Rectangle region)
    {
        var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(region.X, region.Y, 0, 0, region.Size, CopyPixelOperation.SourceCopy);
        }
        
        return bitmap;
    }

    /// <summary>
    /// Captures a screenshot of a specific window using its handle
    /// </summary>
    public static Bitmap CaptureWindow(IntPtr windowHandle)
    {
        if (!GetWindowRect(windowHandle, out RECT windowRect))
        {
            throw new InvalidOperationException("Could not get window bounds");
        }

        int width = windowRect.Right - windowRect.Left;
        int height = windowRect.Bottom - windowRect.Top;

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("Invalid window dimensions");
        }

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var graphics = Graphics.FromImage(bitmap);
        var hdcBitmap = graphics.GetHdc();

        try
        {
            var hdcWindow = GetWindowDC(windowHandle);
            try
            {
                BitBlt(hdcBitmap, 0, 0, width, height, hdcWindow, 0, 0, SRCCOPY);
            }
            finally
            {
                ReleaseDC(windowHandle, hdcWindow);
            }
        }
        finally
        {
            graphics.ReleaseHdc(hdcBitmap);
            graphics.Dispose();
        }

        return bitmap;
    }

    /// <summary>
    /// Captures a screenshot of an AutomationElement (FlaUI window or element)
    /// </summary>
    public static Bitmap CaptureElement(AutomationElement element)
    {
        var bounds = element.BoundingRectangle;
        var region = new Rectangle((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height);
        return CaptureScreenRegion(region);
    }

    /// <summary>
    /// Captures a screenshot of a Window element (FlaUI)
    /// </summary>
    public static Bitmap CaptureWindow(Window window)
    {
        try
        {
            // Try to use the window handle if available
            if (window.Properties.NativeWindowHandle.IsSupported)
            {
                var handle = new IntPtr(window.Properties.NativeWindowHandle.Value);
                return CaptureWindow(handle);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to capture window via handle: {ex.Message}. Falling back to bounds capture.");
        }

        // Fallback to capturing by bounds
        return CaptureElement(window);
    }

    /// <summary>
    /// Saves a screenshot to the specified path with proper error handling
    /// </summary>
    public static void SaveScreenshot(Bitmap bitmap, string filePath, ImageFormat? format = null)
    {
        format ??= ImageFormat.Png;
        
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bitmap.Save(filePath, format);
            Console.WriteLine($"📸 Screenshot saved: {filePath} ({bitmap.Width}x{bitmap.Height})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to save screenshot to {filePath}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Takes a full screenshot and saves it to the specified path
    /// </summary>
    public static void TakeFullScreenshot(string filePath)
    {
        using var bitmap = CaptureScreen();
        SaveScreenshot(bitmap, filePath);
    }

    /// <summary>
    /// Takes a screenshot of a window and saves it to the specified path
    /// </summary>
    public static void TakeWindowScreenshot(Window window, string filePath)
    {
        using var bitmap = CaptureWindow(window);
        SaveScreenshot(bitmap, filePath);
    }

    /// <summary>
    /// Takes a screenshot of an element and saves it to the specified path
    /// </summary>
    public static void TakeElementScreenshot(AutomationElement element, string filePath)
    {
        using var bitmap = CaptureElement(element);
        SaveScreenshot(bitmap, filePath);
    }

    /// <summary>
    /// Simple pixel-by-pixel comparison of two images
    /// </summary>
    public static double CompareScreenshots(string screenshotPath1, string screenshotPath2, string? diffOutputPath = null)
    {
        if (!File.Exists(screenshotPath1) || !File.Exists(screenshotPath2))
        {
            throw new FileNotFoundException("One or both screenshot files do not exist");
        }

        using var image1 = new Bitmap(screenshotPath1);
        using var image2 = new Bitmap(screenshotPath2);

        if (image1.Width != image2.Width || image1.Height != image2.Height)
        {
            throw new ArgumentException("Images must have the same dimensions for comparison");
        }

        int totalPixels = image1.Width * image1.Height;
        int differentPixels = 0;
        
        Bitmap? diffImage = null;
        if (!string.IsNullOrEmpty(diffOutputPath))
        {
            diffImage = new Bitmap(image1.Width, image1.Height);
        }

        try
        {
            for (int y = 0; y < image1.Height; y++)
            {
                for (int x = 0; x < image1.Width; x++)
                {
                    var pixel1 = image1.GetPixel(x, y);
                    var pixel2 = image2.GetPixel(x, y);

                    if (pixel1.ToArgb() != pixel2.ToArgb())
                    {
                        differentPixels++;
                        diffImage?.SetPixel(x, y, Color.Red);
                    }
                    else
                    {
                        // Keep original pixel but make it slightly transparent
                        var dimmed = Color.FromArgb(128, pixel1.R, pixel1.G, pixel1.B);
                        diffImage?.SetPixel(x, y, dimmed);
                    }
                }
            }

            // Save difference image if path provided
            if (diffImage != null && !string.IsNullOrEmpty(diffOutputPath))
            {
                var directory = Path.GetDirectoryName(diffOutputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                diffImage.Save(diffOutputPath, ImageFormat.Png);
                Console.WriteLine($"📊 Difference image saved: {diffOutputPath}");
            }
        }
        finally
        {
            diffImage?.Dispose();
        }

        double differencePercentage = (double)differentPixels / totalPixels;
        Console.WriteLine($"📊 Screenshot comparison: {differentPixels}/{totalPixels} pixels different ({differencePercentage:P2})");
        
        return differencePercentage;
    }

    /// <summary>
    /// Creates a side-by-side comparison image of two screenshots
    /// </summary>
    public static void CreateComparisonImage(string screenshot1Path, string screenshot2Path, string outputPath, string? title1 = null, string? title2 = null)
    {
        using var image1 = new Bitmap(screenshot1Path);
        using var image2 = new Bitmap(screenshot2Path);

        int maxHeight = Math.Max(image1.Height, image2.Height);
        int totalWidth = image1.Width + image2.Width + 10; // 10px separator

        using var comparisonImage = new Bitmap(totalWidth, maxHeight);
        using var graphics = Graphics.FromImage(comparisonImage);
        
        // Fill with white background
        graphics.Clear(Color.White);
        
        // Add first image
        graphics.DrawImage(image1, 0, 0);
        
        // Add separator line
        using var grayBrush = new SolidBrush(Color.Gray);
        graphics.FillRectangle(grayBrush, image1.Width + 2, 0, 6, maxHeight);
        
        // Add second image
        graphics.DrawImage(image2, image1.Width + 10, 0);

        // Save comparison image
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        comparisonImage.Save(outputPath, ImageFormat.Png);
        Console.WriteLine($"📊 Comparison image created: {outputPath}");
    }

    /// <summary>
    /// Generates a descriptive filename for screenshots based on test context and timestamp
    /// </summary>
    public static string GenerateScreenshotFileName(string testName, string stepDescription, string extension = "png")
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
        var sanitizedTestName = SanitizeFileName(testName);
        var sanitizedStep = SanitizeFileName(stepDescription);
        
        return $"{timestamp}_{sanitizedTestName}_{sanitizedStep}.{extension}";
    }

    /// <summary>
    /// Sanitizes a string to be used as a filename
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var result = fileName;
        
        foreach (var invalidChar in invalidChars)
        {
            result = result.Replace(invalidChar, '_');
        }
        
        // Also replace spaces and some other characters for cleaner filenames
        result = result.Replace(' ', '_')
                      .Replace(':', '_')
                      .Replace('?', '_')
                      .Replace('!', '_');
        
        // Limit length
        if (result.Length > 100)
        {
            result = result.Substring(0, 100);
        }
        
        return result;
    }

    /// <summary>
    /// Creates a screenshot with basic rectangle annotations
    /// </summary>
    public static void CreateAnnotatedScreenshot(string sourceImagePath, string outputPath, params ScreenshotAnnotation[] annotations)
    {
        using var sourceImage = new Bitmap(sourceImagePath);
        using var annotatedImage = new Bitmap(sourceImage);
        using var graphics = Graphics.FromImage(annotatedImage);
        
        foreach (var annotation in annotations)
        {
            var color = annotation.Color.ToLowerInvariant() switch
            {
                "red" => Color.Red,
                "green" => Color.Green,
                "blue" => Color.Blue,
                "orange" => Color.Orange,
                "yellow" => Color.Yellow,
                _ => Color.Red
            };
            
            using var pen = new Pen(color, annotation.Thickness);
            
            switch (annotation.Type)
            {
                case TestAnnotationType.Rectangle:
                    graphics.DrawRectangle(pen, annotation.X, annotation.Y, annotation.Width, annotation.Height);
                    break;
                    
                case TestAnnotationType.Circle:
                    graphics.DrawEllipse(pen, annotation.X, annotation.Y, annotation.Width, annotation.Height);
                    break;
            }
        }
        
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        annotatedImage.Save(outputPath, ImageFormat.Png);
        Console.WriteLine($"📝 Annotated screenshot created: {outputPath}");
    }

    /// <summary>
    /// Waits for a window to stabilize (no visual changes) before taking screenshot
    /// </summary>
    public static async Task<Bitmap> CaptureStableWindowAsync(Window window, int stabilityTimeMs = 1000, int maxWaitMs = 10000)
    {
        var startTime = DateTime.Now;
        Bitmap? previousCapture = null;
        
        while ((DateTime.Now - startTime).TotalMilliseconds < maxWaitMs)
        {
            using var currentCapture = CaptureWindow(window);
            
            if (previousCapture != null)
            {
                // Simple comparison - check if images are identical
                bool identical = true;
                if (previousCapture.Width == currentCapture.Width && previousCapture.Height == currentCapture.Height)
                {
                    // Quick sample-based comparison for performance
                    var samplePoints = new[]
                    {
                        new Point(0, 0),
                        new Point(currentCapture.Width / 2, currentCapture.Height / 2),
                        new Point(currentCapture.Width - 1, currentCapture.Height - 1),
                        new Point(currentCapture.Width / 4, currentCapture.Height / 4),
                        new Point(3 * currentCapture.Width / 4, 3 * currentCapture.Height / 4)
                    };
                    
                    foreach (var point in samplePoints)
                    {
                        if (previousCapture.GetPixel(point.X, point.Y).ToArgb() != 
                            currentCapture.GetPixel(point.X, point.Y).ToArgb())
                        {
                            identical = false;
                            break;
                        }
                    }
                }
                else
                {
                    identical = false;
                }
                
                if (identical)
                {
                    Console.WriteLine($"📸 Window stabilized after {(DateTime.Now - startTime).TotalMilliseconds:F0}ms");
                    return CaptureWindow(window);
                }
            }
            
            previousCapture?.Dispose();
            previousCapture = new Bitmap(currentCapture);
            
            await Task.Delay(stabilityTimeMs / 5); // Check 5 times during stability period
        }
        
        previousCapture?.Dispose();
        Console.WriteLine($"⚠️ Window did not stabilize within {maxWaitMs}ms, taking final screenshot");
        return CaptureWindow(window);
    }
}

/// <summary>
/// Represents an annotation to be drawn on a screenshot
/// </summary>
public class ScreenshotAnnotation
{
    public TestAnnotationType Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string Color { get; set; } = "Red";
    public float Thickness { get; set; } = 2f;
    public string? Text { get; set; }
}

/// <summary>
/// Types of annotations that can be added to screenshots
/// </summary>
public enum TestAnnotationType
{
    Rectangle,
    Circle,
    Arrow,
    Text
}