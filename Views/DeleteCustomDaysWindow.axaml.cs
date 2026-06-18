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
    public List<DateTime>? SelectedDates { get; private set; } = new();
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
            SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle,
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#E5E7EB"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(4)
        };

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

            // Подписываемся после того как элемент встроен в визуальное дерево
            border.AttachedToVisualTree += (s, e) =>
            {
                // Идём вверх по визуальному дереву до ListBoxItem
                Control? ancestor = border.Parent as Control;
                while (ancestor != null && ancestor is not ListBoxItem)
                    ancestor = ancestor.Parent as Control;

                if (ancestor is not ListBoxItem listItem) return;

                var bgBinding = new Binding
                {
                    Source = listItem,
                    Path = "IsSelected",
                    Converter = new FuncValueConverter<bool, IBrush>(sel =>
                        sel ? Brush.Parse("#DBEAFE") : Brushes.Transparent)
                };
                border.Bind(Border.BackgroundProperty, bgBinding);

                var fgBinding = new Binding
                {
                    Source = listItem,
                    Path = "IsSelected",
                    Converter = new FuncValueConverter<bool, IBrush>(sel =>
                        sel ? Brush.Parse("#1E40AF") : Brush.Parse("#111827"))
                };
                textBlock.Bind(TextBlock.ForegroundProperty, fgBinding);
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

        var btnCancel = new Button
        {
            Content = "Отмена",
            Padding = new Avalonia.Thickness(16, 6),
            Background = Brush.Parse("#E5E7EB")
        };
        btnCancel.Click += (_, _) => { SelectedDates = null; Close(); };

        var btnConfirm = new Button
        {
            Content = "Применить",
            Padding = new Avalonia.Thickness(16, 6),
            Background = Brush.Parse("#1A73E8"),
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold
        };
        btnConfirm.Click += (_, _) =>
        {
            // OfType<> безопаснее Cast<> — не бросает исключение при несоответствии типа
            SelectedDates = _listBox.SelectedItems?
                .OfType<SelectableDateItem>()
                .Select(i => i.Date.Date)
                .ToList() ?? new List<DateTime>();
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
        var items = upcomingDates.Select(d => new SelectableDateItem { Date = d.Date }).ToList();
        _listBox.ItemsSource = items;
    }
}
