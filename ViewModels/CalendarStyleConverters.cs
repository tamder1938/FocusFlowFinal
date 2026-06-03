using Avalonia.Data.Converters;
using Avalonia.Media;
using System;

namespace FocusFlowFinal.ViewModels;

public static class CalendarStyleConverters
{
    // Конвертер переводит HEX-строку вида "#3498db" в понятную для Avalonia UI кисть SolidColorBrush
    public static readonly IValueConverter StringToBrush =
        new FuncValueConverter<string, IBrush>(colorStr =>
        {
            if (string.IsNullOrEmpty(colorStr))
                return Brushes.Gray;
            try
            {
                return Brush.Parse(colorStr);
            }
            catch
            {
                return Brushes.Gray;
            }
        });
}
