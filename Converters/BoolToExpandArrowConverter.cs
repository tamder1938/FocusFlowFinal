using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FocusFlowFinal.Converters;

public class BoolToExpandArrowConverter : IValueConverter
{
    public static readonly BoolToExpandArrowConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "▼" : "▶";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
