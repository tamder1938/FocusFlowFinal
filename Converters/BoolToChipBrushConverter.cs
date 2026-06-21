using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace FocusFlowFinal.Converters;

public class BoolToChipBrushConverter : IValueConverter
{
    public static readonly BoolToChipBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.Parse("#3B82F6"), 0.18)
            : new SolidColorBrush(Colors.Transparent);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
