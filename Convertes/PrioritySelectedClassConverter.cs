using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FocusFlowFinal.Converters;

public class PrioritySelectedClassConverter : IValueConverter
{
    public static readonly PrioritySelectedClassConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int priorityIndex && parameter is string paramStr && int.TryParse(paramStr, out int targetPriority))
        {
            return priorityIndex == targetPriority ? "selected" : null;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}