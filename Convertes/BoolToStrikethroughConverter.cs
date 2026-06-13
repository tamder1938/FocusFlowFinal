using Avalonia.Media;
using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FocusFlowFinal.Converters;

/// <summary>
/// true  → TextDecorations.Strikethrough (задача выполнена — зачёркнуть)
/// false → null (обычный текст)
/// </summary>
public class BoolToStrikethroughConverter : IValueConverter
{
    public static readonly BoolToStrikethroughConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TextDecorations.Strikethrough : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// true  → 0.45 (выполненная задача приглушена)
/// false → 1.0  (обычная задача)
/// </summary>
public class IsCompletedToOpacityConverter : IValueConverter
{
    public static readonly IsCompletedToOpacityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 0.7 : 1.0;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
