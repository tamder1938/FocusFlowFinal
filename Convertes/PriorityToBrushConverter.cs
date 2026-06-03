using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FocusFlowFinal.Converters;

public class PriorityToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int priority)
        {
            return priority switch
            {
                0 => new SolidColorBrush(Color.Parse("#EF4444")), // высокий
                1 => new SolidColorBrush(Color.Parse("#F59E0B")), // средний
                2 => new SolidColorBrush(Color.Parse("#10B981")), // низкий
                _ => new SolidColorBrush(Color.Parse("#9CA3AF"))
            };
        }
        return new SolidColorBrush(Color.Parse("#9CA3AF"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}