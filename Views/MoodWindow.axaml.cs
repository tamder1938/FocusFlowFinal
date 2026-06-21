using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using FocusFlowFinal.ViewModels;
using System.Linq;

namespace FocusFlowFinal.Views;

public partial class MoodWindow : Window
{
    private MoodViewModel? Vm => DataContext as MoodViewModel;

    public MoodWindow()
    {
        InitializeComponent();
    }

    // ── Выбор записи из списка ────────────────────────────────────────
    private void EntryItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: MoodListDisplayItem item })
            Vm?.SelectEntryCommand.Execute(item);
    }

    // ── Кнопка настроения ────────────────────────────────────────────
    private void MoodLevel_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: MoodLevelItem item })
            Vm?.SelectMoodLevelCommand.Execute(item.Level);
    }

    // ── Переключение активности ──────────────────────────────────────
    private void Activity_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ActivityDisplayItem item })
            Vm?.ToggleActivityCommand.Execute(item);
    }

    // ── Раскрытие категории ──────────────────────────────────────────
    private void CategoryHeader_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ActivityCategoryGroup group })
            group.IsExpanded = !group.IsExpanded;
    }

    // ── Добавить своё ────────────────────────────────────────────────
    private void ShowAddCustom_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ActivityCategoryGroup group })
            Vm?.ShowAddCustomActivityCommand.Execute(group);
    }

    private void ConfirmCustom_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ActivityCategoryGroup group })
            Vm?.ConfirmAddCustomActivityCommand.Execute(group);
    }

    // ── Фото ─────────────────────────────────────────────────────────
    private async void AddPhoto_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title           = "Выбрать фото",
            AllowMultiple   = true,
            FileTypeFilter  = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Изображения")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.webp" }
                }
            }
        });

        if (files?.Count > 0)
        {
            var paths = files.Select(f => f.TryGetLocalPath()).Where(p => p != null).Select(p => p!);
            Vm?.AddPhotoPaths(paths);
        }
    }

    private void RemovePhoto_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: string path })
            Vm?.RemovePhotoCommand.Execute(path);
    }

    // ── Фильтры списка ────────────────────────────────────────────────
    private void FilterAll_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.PeriodFilter = 0;
        SetFilterButtonStyle(0);
    }

    private void FilterMonth_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.PeriodFilter = 1;
        SetFilterButtonStyle(1);
    }

    private void FilterYear_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.PeriodFilter = 2;
        SetFilterButtonStyle(2);
    }

    private void SetFilterButtonStyle(int active)
    {
        var btns = new[] { FilterAllBtn, FilterMonthBtn, FilterYearBtn };
        for (int i = 0; i < btns.Length; i++)
        {
            if (btns[i] == null) continue;
            btns[i]!.Background = i == active ? GetBrush("AccentLightBrush") : GetBrush("CardBackgroundBrush");
            btns[i]!.Foreground = i == active ? GetBrush("AccentBrush") : GetBrush("SecondaryForegroundBrush");
        }
    }

    // ── Переключатель периода графика ─────────────────────────────────
    private void ChartWeek_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.ChartPeriod = 0;
        SetChartButtonStyle(0);
    }

    private void ChartMonth_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.ChartPeriod = 1;
        SetChartButtonStyle(1);
    }

    private void ChartYear_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.ChartPeriod = 2;
        SetChartButtonStyle(2);
    }

    private void SetChartButtonStyle(int active)
    {
        var btns = new[] { ChartWeekBtn, ChartMonthBtn, ChartYearBtn };
        for (int i = 0; i < btns.Length; i++)
        {
            if (btns[i] == null) continue;
            btns[i]!.Background = i == active ? GetBrush("AccentLightBrush") : GetBrush("CardBackgroundBrush");
            btns[i]!.Foreground = i == active ? GetBrush("AccentBrush") : GetBrush("SecondaryForegroundBrush");
        }
    }

    private IBrush? GetBrush(string key)
    {
        if (Application.Current?.Resources.TryGetResource(key, null, out var v) == true && v is IBrush b)
            return b;
        return null;
    }

    // ── Подтверждение удаления ────────────────────────────────────────
    private async void DeleteEntry_Confirm()
    {
        var vm = Vm;
        if (vm == null) return;

        var dialog = new Window
        {
            Width = 380, Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Title = vm.Loc["Mood_DeleteBtn"],
            Background = GetBrush("BackgroundBrush"),
            CanResize = false
        };

        var yes = new Button
        {
            Content    = vm.Loc["Yes"],
            Background = new SolidColorBrush(Avalonia.Media.Color.Parse("#DC2626")),
            Foreground = Brushes.White,
            Margin     = new Thickness(0, 0, 8, 0),
            Padding    = new Thickness(16, 6),
            CornerRadius = new CornerRadius(6)
        };
        var no = new Button
        {
            Content  = vm.Loc["No"],
            Padding  = new Thickness(16, 6),
            CornerRadius = new CornerRadius(6)
        };

        bool confirmed = false;
        yes.Click += (_, _) => { confirmed = true; dialog.Close(); };
        no.Click  += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = vm.Loc["Mood_DeleteConfirm"], FontSize = 14,
                    Foreground = GetBrush("ForegroundBrush"),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { yes, no } }
            }
        };

        await dialog.ShowDialog(this);
        if (confirmed) vm.DeleteEntryCommand.Execute(null);
    }
}
