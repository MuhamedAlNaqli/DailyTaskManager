using System;

namespace DailyTaskManager.Infrastructure.Services;

internal enum ImageKind
{
    Unknown,
    Png,
    Jpeg
}

/// <summary>Reads just enough of a PNG or JPEG file to know its type and size, without an imaging library</summary>
internal static class ImageInfo
{
    public static ImageKind Detect(byte[] data)
    {
        if (data.Length > 24 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) return ImageKind.Png;
        if (data.Length > 4 && data[0] == 0xFF && data[1] == 0xD8) return ImageKind.Jpeg;
        return ImageKind.Unknown;
    }

    /// <summary>Pixel size of the image, or (0, 0) when it cannot be read</summary>
    public static (int Width, int Height) Size(byte[] data)
    {
        switch (Detect(data))
        {
            case ImageKind.Png:
                return (ReadInt(data, 16), ReadInt(data, 20));

            case ImageKind.Jpeg:
                var i = 2;
                while (i + 9 < data.Length)
                {
                    if (data[i] != 0xFF) { i++; continue; }

                    var marker = data[i + 1];
                    if (marker == 0xFF) { i++; continue; }
                    if (marker is 0xD8 or 0x01 || marker is >= 0xD0 and <= 0xD7) { i += 2; continue; }

                    var length = (data[i + 2] << 8) | data[i + 3];
                    // Start-of-frame markers carry the size (C4, C8 and CC are not frames)
                    if (marker is >= 0xC0 and <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
                    {
                        var height = (data[i + 5] << 8) | data[i + 6];
                        var width = (data[i + 7] << 8) | data[i + 8];
                        return (width, height);
                    }

                    i += 2 + length;
                }

                return (0, 0);

            default:
                return (0, 0);
        }
    }

    private static int ReadInt(byte[] data, int offset) =>
        (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
}
