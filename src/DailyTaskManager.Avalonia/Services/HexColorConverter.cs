using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DailyTaskManager.Avalonia.Services;

/// <summary>Turns a #RRGGBB text into a brush (transparent when empty or invalid)</summary>
public class HexColorConverter : IValueConverter
{
    public static readonly HexColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && text.Length > 0 && Color.TryParse(text, out var color)) return new SolidColorBrush(color);
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
