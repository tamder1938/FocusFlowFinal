using System;
using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FocusFlowFinal.Converters;

public class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isCurrentMonth)
            return isCurrentMonth ? 1.0 : 0.4;
        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Конвертер: возвращает true, если в коллекции 7 и более элементов
/// </summary>
public class CollectionCountToBoolConverter : IValueConverter
{
    public static readonly CollectionCountToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ICollection collection)
            return collection.Count >= 7;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Конвертер: возвращает true, если в коллекции меньше 7 элементов
/// </summary>
public class CollectionCountToInverseBoolConverter : IValueConverter
{
    public static readonly CollectionCountToInverseBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ICollection collection)
            return collection.Count < 7;
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}