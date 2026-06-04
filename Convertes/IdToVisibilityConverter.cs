using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FocusFlowFinal.Converters;

public class IdToVisibilityConverter : IValueConverter
{
    public static readonly IdToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int id && parameter is string param)
        {
            if (param == "positive") return id > 0;
            if (param == "not0") return id != 0;
            if (int.TryParse(param, out int targetId)) return id == targetId;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}