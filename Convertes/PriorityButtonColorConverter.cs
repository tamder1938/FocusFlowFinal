using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace FocusFlowFinal.Converters;

public class PriorityButtonColorConverter : IValueConverter
{
    public static PriorityButtonColorConverter High { get; } = new() { Priority = "High" };
    public static PriorityButtonColorConverter Medium { get; } = new() { Priority = "Medium" };
    public static PriorityButtonColorConverter Low { get; } = new() { Priority = "Low" };

    public string? Priority { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var theme = value as ThemeVariant;
        bool isDark = theme == ThemeVariant.Dark;

        return Priority switch
        {
            "High" => isDark ? new SolidColorBrush(Color.Parse("#B91C1C")) : new SolidColorBrush(Color.Parse("#EF4444")),
            "Medium" => isDark ? new SolidColorBrush(Color.Parse("#B45309")) : new SolidColorBrush(Color.Parse("#F59E0B")),
            "Low" => isDark ? new SolidColorBrush(Color.Parse("#047857")) : new SolidColorBrush(Color.Parse("#10B981")),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}