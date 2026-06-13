using Avalonia.Data.Converters;
using FocusFlowFinal.ViewModels;
using System;

namespace FocusFlowFinal.Converters;

/// <summary>
/// Статические конвертеры для подсветки активной кнопки фильтра
/// «Все / Активные / Готовые» (Проблема 12) в TaskListView.axaml.
/// </summary>
public static class CompletionFilterConverter
{
    public static readonly IValueConverter IsAll =
        new FuncValueConverter<CompletionFilter, bool>(f => f == CompletionFilter.All);

    public static readonly IValueConverter IsActive =
        new FuncValueConverter<CompletionFilter, bool>(f => f == CompletionFilter.Active);

    public static readonly IValueConverter IsDone =
        new FuncValueConverter<CompletionFilter, bool>(f => f == CompletionFilter.Done);
}

/// <summary>true, если строка не null и не пустая (для IsVisible описания задачи).</summary>
public static class NullOrEmptyToBoolConverter
{
    public static readonly IValueConverter IsNotNullOrEmpty =
        new FuncValueConverter<string?, bool>(s => !string.IsNullOrEmpty(s));
}
