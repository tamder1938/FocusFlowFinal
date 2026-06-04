using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FocusFlowFinal.Converters;

public class ProjectIconGeometryConverter : IValueConverter
{
    public static readonly ProjectIconGeometryConverter Instance = new();

    private static readonly StreamGeometry AllProjectsGeometry = StreamGeometry.Parse(
        "M4,4 h4 v4 h-4 z M4,10 h4 v4 h-4 z M10,4 h4 v4 h-4 z M10,10 h4 v4 h-4 z");

    private static readonly StreamGeometry NoProjectGeometry = StreamGeometry.Parse(
        "M12,2 C6.48,2 2,6.48 2,12 s4.48,10 10,10 10-4.48 10-10 S17.52,2 12,2 z M12,4 c4.41,0 8,3.59 8,8 0,1.79-0.59,3.42-1.58,4.75 L7.25,5.58 C8.58,4.59 10.21,4 12,4 z M4,12 c0-1.79 0.59-3.42 1.58-4.75 L16.75,18.42 C15.42,19.41 13.79,20 12,20 7.59,20 4,16.41 4,12 z");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int id)
        {
            if (id == -1) return AllProjectsGeometry;
            if (id == 0) return NoProjectGeometry;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}