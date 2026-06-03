using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FocusFlowFinal.Converters;

public class ColorEqualityConverter : IValueConverter
{
    public static ColorEqualityConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selectedColor = value as string;
        var buttonColor = parameter as string;
        return selectedColor != null && buttonColor != null && selectedColor.Equals(buttonColor, StringComparison.OrdinalIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}