using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace FocusFlowFinal.Converters;

public class HeatLevelToBrushConverter : IValueConverter
{
    public static readonly HeatLevelToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int level || Avalonia.Application.Current == null)
            return null;
        var key = $"HeatmapLevel{Math.Clamp(level, 0, 4)}";
        if (Avalonia.Application.Current.Resources.TryGetResource(key, null, out var res))
            return res as IBrush;
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
