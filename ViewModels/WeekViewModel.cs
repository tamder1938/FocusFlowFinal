using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlowFinal.ViewModels;

public partial class WeekViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private DateTime _weekStart;
    [ObservableProperty] private string _weekRangeTitle = string.Empty;
    [ObservableProperty] private ObservableCollection<WeekDayItem> _weekDaysCustomCollection = new();
    [ObservableProperty] private ObservableCollection<string> _hourStrings = new();

    public WeekViewModel(IDatabaseService db)
    {
        _db = db;

        for (int i = 0; i < 24; i++)
        {
            HourStrings.Add($"{i:D2}:00");
        }

        Loc.PropertyChanged += (s, e) => RefreshWeek();
        GoToWeek(DateTime.Today);
    }

    public void GoToWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        WeekStart = date.AddDays(-1 * diff).Date;

        DateTime weekEnd = WeekStart.AddDays(6);
        WeekRangeTitle = $"{WeekStart:dd.MM.yyyy} - {weekEnd:dd.MM.yyyy}";

        RefreshWeek();
    }

    public void RefreshWeek()
    {
        try
        {
            // Создаём временный список, чтобы не очищать коллекцию до готовности
            var newItems = new System.Collections.Generic.List<WeekDayItem>();
            for (int i = 0; i < 7; i++)
            {
                var targetDay = WeekStart.AddDays(i);
                var dayItem = new WeekDayItem(targetDay);
                var dayEvents = _db.GetEventsForDisplay(targetDay).ToList();
                foreach (var ev in dayEvents)
                {
                    dayItem.Events.Add(ev);
                }
                newItems.Add(dayItem);
            }

            // Атомарная замена: очищаем и добавляем новые элементы
            WeekDaysCustomCollection.Clear();
            foreach (var item in newItems)
            {
                WeekDaysCustomCollection.Add(item);
            }

            System.Diagnostics.Debug.WriteLine($"WeekView: коллекция заполнена, кол-во = {WeekDaysCustomCollection.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WeekView ОШИБКА: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
        }
    }

    [RelayCommand] private void NextWeek() => GoToWeek(WeekStart.AddDays(7));
    [RelayCommand] private void PreviousWeek() => GoToWeek(WeekStart.AddDays(-7));
}