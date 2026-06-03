using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FocusFlowFinal.Views;

public class SelectableDateItem
{
    public DateTime Date { get; set; }
    public string DisplayText => Date.ToString("dd.MM.yyyy (dddd)", new CultureInfo("ru-RU"));
}

public class DeleteCustomDaysWindow : Window
{
    public List<DateTime>? SelectedDates { get; private set; } = new List<DateTime>();
    private readonly ListBox _listBox;

    public DeleteCustomDaysWindow()
    {
        Title = "Выбор дней для освобождения";
        Width = 360;
        Height = 440;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#F3F4F6");

        var mainGrid = new Grid { RowDefinitions = new RowDefinitions("*, Auto"), Margin = new Avalonia.Thickness(15) };

        _listBox = new ListBox
        {
            SelectionMode = SelectionMode.Multiple,
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#E5E7EB"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(4)
        };

        // НАДЕЖНОЕ РЕШЕНИЕ: Динамический шаблон с использованием стандартного Binding к IsSelected
        _listBox.ItemTemplate = new FuncDataTemplate<SelectableDateItem>((item, _) =>
        {
            var textBlock = new TextBlock
            {
                Text = item.DisplayText,
                Margin = new Avalonia.Thickness(8, 6),
                FontWeight = FontWeight.Medium
            };

            var border = new Border
            {
                CornerRadius = new Avalonia.CornerRadius(4),
                Child = textBlock
            };

            // Когда элемент добавляется в визуальное дерево, привязываем его цвета к состоянию ListBoxItem
            border.AttachedToVisualTree += (s, e) =>
            {
                if (border.Parent is ListBoxItem listItem)
                {
                    // 1. Привязка для фона Border (меняется на синий, если элемент выбран)
                    var bgBinding = new Binding
                    {
                        Source = listItem,
                        Path = "IsSelected",
                        Converter = new FuncValueConverter<bool, IBrush>(isSelected =>
                            isSelected ? Brush.Parse("#DBEAFE") : Brushes.Transparent)
                    };
                    border.Bind(Border.BackgroundProperty, bgBinding);

                    // 2. Привязка для цвета текста TextBlock (меняется на темно-синий при выборе)
                    var fgBinding = new Binding
                    {
                        Source = listItem,
                        Path = "IsSelected",
                        Converter = new FuncValueConverter<bool, IBrush>(isSelected =>
                            isSelected ? Brush.Parse("#1E40AF") : Brush.Parse("#111827"))
                    };
                    textBlock.Bind(TextBlock.ForegroundProperty, fgBinding);
                }
            };

            return border;
        });

        Grid.SetRow(_listBox, 0);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Avalonia.Thickness(0, 15, 0, 0)
        };
        Grid.SetRow(buttonPanel, 1);

        var btnCancel = new Button { Content = "Отмена", Padding = new Avalonia.Thickness(16, 6), Background = Brush.Parse("#E5E7EB") };
        btnCancel.Click += (s, e) => { SelectedDates = null; Close(); };

        var btnConfirm = new Button { Content = "Применить", Padding = new Avalonia.Thickness(16, 6), Background = Brush.Parse("#1A73E8"), Foreground = Brushes.White, FontWeight = FontWeight.Bold };
        btnConfirm.Click += (s, e) =>
        {
            if (_listBox.SelectedItems != null)
            {
                SelectedDates = _listBox.SelectedItems
                    .Cast<SelectableDateItem>()
                    .Select(i => i.Date.Date)
                    .ToList();
            }
            else
            {
                SelectedDates = new List<DateTime>();
            }
            Close();
        };

        buttonPanel.Children.Add(btnCancel);
        buttonPanel.Children.Add(btnConfirm);

        mainGrid.Children.Add(_listBox);
        mainGrid.Children.Add(buttonPanel);

        Content = mainGrid;
    }

    public DeleteCustomDaysWindow(List<DateTime> upcomingDates) : this()
    {
        if (_listBox != null)
        {
            var items = upcomingDates.Select(d => new SelectableDateItem { Date = d.Date }).ToList();
            _listBox.ItemsSource = items;
        }
    }
}
