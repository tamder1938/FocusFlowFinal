using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FocusFlowFinal.Converters;

public class HasDurationClassConverter : IValueConverter
{
    public static readonly HasDurationClassConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int minutes && minutes > 0)
            return "has-duration";
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}