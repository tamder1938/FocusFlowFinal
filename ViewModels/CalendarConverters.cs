using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FocusFlowFinal.Models;

namespace FocusFlowFinal.ViewModels;

public static class CalendarConverters
{
    // Задачи сдвигаем вправо на 130 пикселей, события оставляем слева (4 пикселя)
    public static readonly IValueConverter TaskIdToLeft =
        new FuncValueConverter<int?, double>(taskId => taskId.HasValue && taskId.Value > 0 ? 130.0 : 4.0);

    // Ширина задачи 120 пикселей, обычного события — 240 пикселей
    public static readonly IValueConverter TaskIdToWidth =
        new FuncValueConverter<int?, double>(taskId => taskId.HasValue && taskId.Value > 0 ? 120.0 : 240.0);

    // У задач синий текст (#1E40AF), у событий — белый
    public static readonly IValueConverter TaskIdToForeground =
        new FuncValueConverter<int?, IBrush>(taskId => taskId.HasValue && taskId.Value > 0
            ? SolidColorBrush.Parse("#1E40AF") : Brushes.White);

    // У задач левый маркер толщиной 2 пикселя, у событий — 0
    public static readonly IValueConverter TaskIdToBorderThickness =
        new FuncValueConverter<int?, Thickness>(taskId => taskId.HasValue && taskId.Value > 0
            ? new Thickness(2, 0, 0, 0) : new Thickness(0));

    // У задач нежный пастельный фон (#EFF6FF), у событий — их собственный цвет
    public static readonly IValueConverter ItemToBackground =
        new FuncValueConverter<EventDisplayItem, IBrush>(item =>
            item?.OriginalEvent?.TaskId.HasValue == true && item.OriginalEvent.TaskId.Value > 0
                ? SolidColorBrush.Parse("#EFF6FF")
                : (item?.Color != null ? SolidColorBrush.Parse(item.Color) : Brushes.Gray));
}
