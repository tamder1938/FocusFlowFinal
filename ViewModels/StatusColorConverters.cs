using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace FocusFlowFinal.ViewModels;

/// <summary>
/// Статические конвертеры, преобразующие флаг <c>ExportStatusIsError</c>
/// в цвет фона/текста плашки статуса экспорта (зелёный / красный).
/// Используются как {x:Static} в SettingsWindow.axaml.
/// </summary>
public static class StatusColorConverters
{
    public static readonly IValueConverter BgConverter = new FuncValueConverter<bool, Color>(
        isError => isError ? Color.Parse("#FEE2E2") : Color.Parse("#D1FAE5"));

    public static readonly IValueConverter FgConverter = new FuncValueConverter<bool, Color>(
        isError => isError ? Color.Parse("#991B1B") : Color.Parse("#065F46"));
}
