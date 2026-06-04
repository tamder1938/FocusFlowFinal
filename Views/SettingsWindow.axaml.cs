using Avalonia.Controls;
using Avalonia.Styling;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;

namespace FocusFlowFinal.Views;

public partial class SettingsWindow : Window
{
    private int currentTheme = 0;

    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
        SetupEventHandlers();
        HighlightActiveThemeCard();
    }

    private void LoadSettings()
    {
        var settings = AppSettings.Load();
        currentTheme = settings.ThemeMode;
        LanguageCombo.SelectedIndex = settings.Language == "English" ? 1 : 0;
        SystemNotifToggle.IsChecked = settings.SystemNotifications;
        SoundNotifToggle.IsChecked = settings.SoundNotifications;
    }

    private void SetupEventHandlers()
    {
        // Навигация по вкладкам
        GeneralNavBtn.Click += (s, e) => SwitchTab(0);
        NotificationsNavBtn.Click += (s, e) => SwitchTab(1);
        HotkeysNavBtn.Click += (s, e) => SwitchTab(2);
        DataNavBtn.Click += (s, e) => SwitchTab(3);

        // Выбор темы (используем PointerPressed, чтобы избежать двойного клика)
        LightThemeCard.PointerPressed += (s, e) => SetTheme(0);
        DarkThemeCard.PointerPressed += (s, e) => SetTheme(1);
        AutoThemeCard.PointerPressed += (s, e) => SetTheme(2);

        // Кнопки нижней панели
        CancelBtn.Click += (s, e) => Close();
        SaveBtn.Click += SaveSettings;
        ExportBtn.Click += ExportData;
        ClearAllBtn.Click += ClearAllData;
        CloseButton.Click += (s, e) => Close();
    }

    private void SwitchTab(int tabIndex)
    {
        var navBtns = new[] { GeneralNavBtn, NotificationsNavBtn, HotkeysNavBtn, DataNavBtn };
        foreach (var btn in navBtns) btn.Classes.Remove("active");
        navBtns[tabIndex].Classes.Add("active");

        GeneralPanel.IsVisible = (tabIndex == 0);
        NotificationsPanel.IsVisible = (tabIndex == 1);
        HotkeysPanel.IsVisible = (tabIndex == 2);
        DataPanel.IsVisible = (tabIndex == 3);
    }

    private void SetTheme(int themeMode)
    {
        currentTheme = themeMode;
        var app = App.Current as App;
        if (app != null)
        {
            ThemeVariant variant = themeMode switch
            {
                0 => ThemeVariant.Light,
                1 => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
            app.RequestedThemeVariant = variant;
            // Если у вас есть метод ApplyTheme в App, можете вызвать его
        }
        HighlightActiveThemeCard();
    }

    private void HighlightActiveThemeCard()
    {
        var cards = new[] { LightThemeCard, DarkThemeCard, AutoThemeCard };
        foreach (var card in cards) card.Classes.Remove("active");
        if (currentTheme >= 0 && currentTheme <= 2)
            cards[currentTheme].Classes.Add("active");
    }

    private async void SaveSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var settings = AppSettings.Load();
        settings.ThemeMode = currentTheme;
        settings.Language = LanguageCombo.SelectedIndex == 1 ? "English" : "Русский";
        settings.SystemNotifications = SystemNotifToggle.IsChecked ?? true;
        settings.SoundNotifications = SoundNotifToggle.IsChecked ?? false;
        settings.Save();

        LocalizationService.Instance.CurrentLanguage = settings.Language;
        Close();
    }

    private async void ExportData(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Используем StorageProvider в Avalonia 11+
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Экспорт данных",
            DefaultExtension = "json",
            SuggestedFileName = "FocusFlow_backup.json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON файл") { Patterns = new[] { "*.json" } }
            }
        });

        if (file != null)
        {
            // Здесь должна быть логика экспорта данных (сериализация задач, событий и т.д.)
            // Пример: var data = new { Tasks = _db.GetAllTasks(), Events = ... };
            // var json = System.Text.Json.JsonSerializer.Serialize(data);
            // await file.OpenWriteAsync(); и запись.
            // Пока просто покажем сообщение:
            var msg = new TextBlock { Text = "Функция экспорта будет реализована в следующей версии." };
            var dialog = new Window { Content = msg, Width = 300, Height = 150 };
            await dialog.ShowDialog(this);
        }
    }

    private async void ClearAllData(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Простое окно подтверждения (без внешних библиотек)
        var confirm = new Window
        {
            Title = "Подтверждение очистки",
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Grid
            {
                RowDefinitions = new RowDefinitions("*, Auto"),
                Children =
                {
                    new TextBlock
                    {
                        Text = "Вы уверены, что хотите удалить все данные? Это действие необратимо.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Avalonia.Thickness(10),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 10,
                        Margin = new Avalonia.Thickness(10),
                        Children =
                        {
                            new Button { Content = "Да", Width = 80, Background = Avalonia.Media.Brushes.Red, Foreground = Avalonia.Media.Brushes.White },
                            new Button { Content = "Нет", Width = 80 }
                        }
                    }
                }
            }
        };

        Button? yesBtn = null, noBtn = null;
        foreach (var child in ((StackPanel)((Grid)confirm.Content).Children[1]).Children)
        {
            if (child is Button btn)
            {
                if (btn.Content?.ToString() == "Да") yesBtn = btn;
                if (btn.Content?.ToString() == "Нет") noBtn = btn;
            }
        }

        var tcs = new TaskCompletionSource<bool>();
        yesBtn!.Click += (_, _) => { tcs.SetResult(true); confirm.Close(); };
        noBtn!.Click += (_, _) => { tcs.SetResult(false); confirm.Close(); };

        await confirm.ShowDialog(this);
        var result = await tcs.Task;

        if (result)
        {
            // Здесь должна быть логика очистки всех данных
            // Например: _db.ClearAllData();
            var msg = new TextBlock { Text = "Очистка данных будет реализована в следующей версии." };
            var dialog = new Window { Content = msg, Width = 300, Height = 150 };
            await dialog.ShowDialog(this);
        }
    }
}