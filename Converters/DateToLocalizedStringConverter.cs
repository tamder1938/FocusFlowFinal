using Avalonia.Data.Converters;
using FocusFlowFinal.Services;
using System;
using System.Globalization;

namespace FocusFlowFinal.Converters;

public class DateToLocalizedStringConverter : IValueConverter
{
    public static readonly DateToLocalizedStringConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fmt = parameter as string ?? "dd.MM.yy";
        var ci  = LocalizationService.Instance.GetCulture();

        return value switch
        {
            DateTime dt          => dt.ToString(fmt, ci),
            DateTimeOffset dto   => dto.DateTime.ToString(fmt, ci),
            _                    => value?.ToString() ?? string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
