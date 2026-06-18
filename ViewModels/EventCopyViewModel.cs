using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

/// <summary>Ячейка мини-календаря для выбора дат копирования событий.</summary>
public partial class CalendarDayCell : ObservableObject
{
    public DateTime Date        { get; init; }
    public int      DayNumber   { get; init; }
    public bool     IsCurrentMonth { get; init; }
    public bool     IsToday     => Date.Date == DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CellOpacity))]
    private bool _isSelected;

    public double CellOpacity => IsCurrentMonth ? 1.0 : 0.35;

    // Нельзя выбирать прошлые даты
    public bool CanSelect => Date.Date >= DateTime.Today;
}

public partial class EventCopyViewModel : ObservableObject
{
    private readonly CalendarEvent _sourceEvent;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthYearTitle))]
    private int _displayYear;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthYearTitle))]
    private int _displayMonth;

    [ObservableProperty] private ObservableCollection<CalendarDayCell> _days = new();

    [ObservableProperty] private int _selectedCount;

    public string MonthYearTitle
    {
        get
        {
            var loc = LocalizationService.Instance;
            string[] monthKeys = { "January","February","March","April","May","June","July","August","September","October","November","December" };
            return $"{loc[monthKeys[DisplayMonth - 1]]} {DisplayYear}";
        }
    }

    public List<DateTime> SelectedDates { get; } = new();

    public EventCopyViewModel(CalendarEvent sourceEvent)
    {
        _sourceEvent = sourceEvent;
        _displayYear  = DateTime.Today.Year;
        _displayMonth = DateTime.Today.Month;
        RebuildCalendar();
    }

    [RelayCommand]
    private void PrevMonth()
    {
        var dt = new DateTime(DisplayYear, DisplayMonth, 1).AddMonths(-1);
        DisplayYear  = dt.Year;
        DisplayMonth = dt.Month;
        RebuildCalendar();
    }

    [RelayCommand]
    private void NextMonth()
    {
        var dt = new DateTime(DisplayYear, DisplayMonth, 1).AddMonths(1);
        DisplayYear  = dt.Year;
        DisplayMonth = dt.Month;
        RebuildCalendar();
    }

    [RelayCommand]
    private void ToggleDay(CalendarDayCell? cell)
    {
        if (cell == null || !cell.CanSelect || !cell.IsCurrentMonth) return;
        cell.IsSelected = !cell.IsSelected;
        var date = cell.Date.Date;
        if (cell.IsSelected)
            SelectedDates.Add(date);
        else
            SelectedDates.RemoveAll(d => d.Date == date);
        SelectedCount = SelectedDates.Count;
    }

    [RelayCommand]
    private void Apply()
    {
        // SelectedDates актуален через ToggleDay — просто закрываем окно
        CloseWindow(true);
    }

    [RelayCommand]
    private void Cancel() => CloseWindow(false);

    private void RebuildCalendar()
    {
        Days.Clear();
        var firstDay = new DateTime(DisplayYear, DisplayMonth, 1);

        // Понедельник = 0, воскресенье = 6
        int startOffset = ((int)firstDay.DayOfWeek + 6) % 7;

        // Ячейки предыдущего месяца
        for (int i = startOffset - 1; i >= 0; i--)
        {
            var d = firstDay.AddDays(-i - 1);
            Days.Add(new CalendarDayCell { Date = d, DayNumber = d.Day, IsCurrentMonth = false });
        }

        // Ячейки текущего месяца
        int daysInMonth = DateTime.DaysInMonth(DisplayYear, DisplayMonth);
        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(DisplayYear, DisplayMonth, d);
            // Восстанавливаем ранее выбранные даты
            bool wasSelected = SelectedDates.Any(s => s.Date == date);
            Days.Add(new CalendarDayCell { Date = date, DayNumber = d, IsCurrentMonth = true, IsSelected = wasSelected });
        }

        // Дополняем до полных рядов (до 42 ячеек = 6 строк × 7 колонок)
        while (Days.Count < 42)
        {
            var d = firstDay.AddMonths(1).AddDays(Days.Count - startOffset - daysInMonth);
            Days.Add(new CalendarDayCell { Date = d, DayNumber = d.Day, IsCurrentMonth = false });
        }

        SelectedCount = Days.Count(d => d.IsSelected);
    }

    private static void CloseWindow(bool result)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var win = desktop.Windows.LastOrDefault(w => w.DataContext is EventCopyViewModel);
            win?.Close(result);
        }
    }
}
