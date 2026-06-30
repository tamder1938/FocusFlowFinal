using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using FocusFlowFinal.Converters;
using FocusFlowFinal.Models.Wishlist;
using FocusFlowFinal.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace FocusFlowFinal.Views;

public partial class WishlistEditorWindow : Window
{
    private Grid? _tableGrid;
    private WishlistEditorViewModel? _vm;

    public WishlistEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not WishlistEditorViewModel vm) return;
        _vm = vm;
        RebuildTable();
        vm.Rows.CollectionChanged += OnRowsChanged;
        vm.FiltersChanged += (_, _) => RebuildTable();
        vm.CondFormatsChanged += (_, _) => RebuildTable();
    }

    private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_vm == null) return;
        // When filters are active, always do full rebuild (new row may not match filter)
        if (_vm.HasActiveFilters || e.Action != NotifyCollectionChangedAction.Add || e.NewStartingIndex < 0)
            RebuildTable();
        else
            AppendRow(_vm, e.NewStartingIndex);
    }

    // ── Full rebuild ───────────────────────────────────────────────────────

    private void RebuildTable()
    {
        if (_vm == null) return;
        var vm = _vm;
        var cols = vm.Columns.ToList();

        var grid = BuildEmptyGrid(cols);

        AddHeaderRow(grid, cols);

        var displayRows = vm.GetDisplayRows().ToList();
        for (int r = 0; r < displayRows.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            AddDataRow(grid, displayRows[r], r + 1, cols, vm);
        }

        _tableGrid = grid;
        TableContainer.Children.Clear();
        TableContainer.Children.Add(grid);
    }

    private void AppendRow(WishlistEditorViewModel vm, int index)
    {
        if (_tableGrid == null) { RebuildTable(); return; }
        var cols = vm.Columns.ToList();
        _tableGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddDataRow(_tableGrid, vm.Rows[index], index + 1, cols, vm);
    }

    // ── Grid skeleton ──────────────────────────────────────────────────────

    private static Grid BuildEmptyGrid(List<WishlistColumn> cols)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // header

        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(40)));   // #
        foreach (var col in cols)
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(ColWidth(col.Type))));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(36)));   // delete

        return grid;
    }

    private static double ColWidth(WishlistColumnType t) => t switch
    {
        WishlistColumnType.Image  => 84,
        WishlistColumnType.Color  => 64,
        WishlistColumnType.Number => 92,
        WishlistColumnType.Date   => 130,
        _ => 160
    };

    // ── Header ─────────────────────────────────────────────────────────────

    private static void AddHeaderRow(Grid grid, List<WishlistColumn> cols)
    {
        // Background spanning all columns
        var bg = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F3F4F6")),
            Height = 36
        };
        Grid.SetRow(bg, 0);
        Grid.SetColumnSpan(bg, grid.ColumnDefinitions.Count);
        grid.Children.Add(bg);

        // "#" label
        Place(grid, HeaderLabel("#"), 0, 0);

        // Column labels
        for (int c = 0; c < cols.Count; c++)
        {
            var col = cols[c];
            var hdr = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Margin = new Thickness(6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            hdr.Children.Add(new TextBlock { Text = TypeEmoji(col.Type), VerticalAlignment = VerticalAlignment.Center });
            hdr.Children.Add(new TextBlock { Text = col.Name, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            Place(grid, hdr, 0, c + 1);
        }
    }

    private static TextBlock HeaderLabel(string text) => new TextBlock
    {
        Text = text,
        FontWeight = FontWeight.SemiBold,
        Margin = new Thickness(4, 0),
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center
    };

    // ── Data row ───────────────────────────────────────────────────────────

    private void AddDataRow(Grid grid, WishlistRowViewModel rowVm, int gridRow,
        List<WishlistColumn> cols, WishlistEditorViewModel vm)
    {
        // Row separator
        var sep = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#E5E7EB")),
            BorderThickness = new Thickness(0, 1, 0, 0)
        };
        Grid.SetRow(sep, gridRow);
        Grid.SetColumnSpan(sep, grid.ColumnDefinitions.Count);
        grid.Children.Add(sep);

        // Row number
        Place(grid, new TextBlock
        {
            Text = rowVm.Order.ToString(),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#9CA3AF")),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2)
        }, gridRow, 0);

        // Cells
        for (int c = 0; c < cols.Count; c++)
        {
            var col = cols[c];
            var cell = rowVm.GetCell(col.Id) ?? new WishlistCellViewModel(col.Id, col.Type, col.OptionsJson, null, null);
            var ctrl = BuildCellEditor(cell, rowVm, vm);

            // Wrap in Border so we can apply conditional format background
            var bgHex = vm.GetCellBackground(cell, col);
            IBrush? bg = null;
            if (!string.IsNullOrWhiteSpace(bgHex))
                try { bg = new SolidColorBrush(Color.Parse(bgHex)); } catch { }

            var cellContainer = new Border { Child = ctrl, Background = bg };
            Place(grid, cellContainer, gridRow, c + 1);
        }

        // Delete button
        var delBtn = new Button
        {
            Content = "✕",
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.Parse("#EF4444")),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4, 0),
            Cursor = new Cursor(StandardCursorType.Hand),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        delBtn.Click += (_, _) => vm.DeleteRowCommand.Execute(rowVm);
        Place(grid, delBtn, gridRow, cols.Count + 1);
    }

    // ── Cell editors ───────────────────────────────────────────────────────

    private Control BuildCellEditor(WishlistCellViewModel cell, WishlistRowViewModel row,
        WishlistEditorViewModel vm)
    {
        Control editor = cell.ColumnType switch
        {
            WishlistColumnType.Number   => BuildNumberEditor(cell),
            WishlistColumnType.Date     => BuildDateEditor(cell),
            WishlistColumnType.Dropdown => BuildDropdownEditor(cell),
            WishlistColumnType.Color    => BuildColorEditor(cell),
            WishlistColumnType.Link     => BuildLinkEditor(cell),
            WishlistColumnType.Image    => BuildImageEditor(cell),
            _                           => BuildTextEditor(cell)
        };
        editor.LostFocus += (_, _) => vm.SaveCell(cell, row.RowId);
        return editor;
    }

    private static TextBox BuildTextEditor(WishlistCellViewModel cell)
    {
        var tb = MakeTextBox();
        tb.Bind(TextBox.TextProperty, Bind2Way(cell, nameof(cell.Value)));
        return tb;
    }

    private static TextBox BuildNumberEditor(WishlistCellViewModel cell)
    {
        var tb = MakeTextBox();
        tb.Bind(TextBox.TextProperty, Bind2Way(cell, nameof(cell.Value)));
        tb.TextChanged += (_, _) =>
            tb.Foreground = !string.IsNullOrEmpty(tb.Text) && !double.TryParse(tb.Text, out _)
                ? Brushes.Red : null;
        return tb;
    }

    private static DatePicker BuildDateEditor(WishlistCellViewModel cell)
    {
        var dp = new DatePicker { Margin = new Thickness(2) };
        if (DateTimeOffset.TryParse(cell.Value, out var dto)) dp.SelectedDate = dto;
        dp.SelectedDateChanged += (_, _) => cell.Value = dp.SelectedDate?.ToString("yyyy-MM-dd");
        return dp;
    }

    private static ComboBox BuildDropdownEditor(WishlistCellViewModel cell)
    {
        var cb = new ComboBox { ItemsSource = cell.DropdownOptions, Margin = new Thickness(2) };
        if (!string.IsNullOrEmpty(cell.Value)) cb.SelectedItem = cell.Value;
        cb.SelectionChanged += (_, _) => cell.Value = cb.SelectedItem?.ToString();
        return cb;
    }

    private static Grid BuildColorEditor(WishlistCellViewModel cell)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("24, *"), Margin = new Thickness(4, 2) };

        var swatch = new Border
        {
            Width = 20, Height = 20, CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.Parse("#D1D5DB"))
        };
        swatch.Bind(Border.BackgroundProperty,
            new Binding(nameof(cell.CellBackground)) { Source = cell, Converter = HexToBrushConverter.Instance });

        var tb = MakeTextBox();
        tb.Watermark = "#RRGGBB";
        tb.Bind(TextBox.TextProperty, Bind2Way(cell, nameof(cell.Value)));

        Grid.SetColumn(swatch, 0);
        Grid.SetColumn(tb, 1);
        grid.Children.Add(swatch);
        grid.Children.Add(tb);
        return grid;
    }

    private static Grid BuildLinkEditor(WishlistCellViewModel cell)
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto"), Margin = new Thickness(2) };

        var urlBox = MakeTextBox();
        urlBox.Watermark = "URL (https://...)";
        urlBox.Bind(TextBox.TextProperty, Bind2Way(cell, nameof(cell.Value)));

        var captionBox = MakeTextBox();
        captionBox.Watermark = "Подпись";
        captionBox.FontSize = 11;
        captionBox.Foreground = new SolidColorBrush(Color.Parse("#6B7280"));
        captionBox.Bind(TextBox.TextProperty, Bind2Way(cell, nameof(cell.Extra)));

        Grid.SetRow(urlBox, 0);
        Grid.SetRow(captionBox, 1);
        grid.Children.Add(urlBox);
        grid.Children.Add(captionBox);
        return grid;
    }

    private Grid BuildImageEditor(WishlistCellViewModel cell)
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("52,Auto"), Margin = new Thickness(2) };

        var img = new Image { Width = 48, Height = 48, Stretch = Stretch.UniformToFill };
        img.Bind(Image.SourceProperty,
            new Binding(nameof(cell.Value)) { Source = cell, Converter = PathToBitmapConverter.Instance });
        Grid.SetRow(img, 0);
        grid.Children.Add(img);

        var row2 = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
        var pathBox = MakeTextBox();
        pathBox.Watermark = "Путь или URL";
        pathBox.FontSize = 11;
        pathBox.Bind(TextBox.TextProperty, Bind2Way(cell, nameof(cell.Value)));

        var browseBtn = new Button
        {
            Content = "📂",
            Padding = new Thickness(4, 0),
            FontSize = 13,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        browseBtn.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(browseBtn);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите изображение",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Изображения")
                    {
                        Patterns = new[] { "*.png","*.jpg","*.jpeg","*.gif","*.bmp","*.webp" }
                    }
                }
            });
            if (files.Count > 0)
                cell.Value = files[0].Path.LocalPath;
        };

        Grid.SetColumn(pathBox, 0);
        Grid.SetColumn(browseBtn, 1);
        row2.Children.Add(pathBox);
        row2.Children.Add(browseBtn);
        Grid.SetRow(row2, 1);
        grid.Children.Add(row2);
        return grid;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static void Place(Grid grid, Control ctrl, int row, int col)
    {
        Grid.SetRow(ctrl, row);
        Grid.SetColumn(ctrl, col);
        grid.Children.Add(ctrl);
    }

    private static TextBox MakeTextBox() => new TextBox
    {
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Padding = new Thickness(4, 2),
        MinHeight = 32
    };

    private static Binding Bind2Way(object source, string path) =>
        new Binding(path) { Source = source, Mode = BindingMode.TwoWay };

    private static string TypeEmoji(WishlistColumnType t) => t switch
    {
        WishlistColumnType.Text     => "T",
        WishlistColumnType.Number   => "#",
        WishlistColumnType.Date     => "📅",
        WishlistColumnType.Link     => "🔗",
        WishlistColumnType.Dropdown => "▾",
        WishlistColumnType.Image    => "🖼",
        WishlistColumnType.Color    => "🎨",
        _ => string.Empty
    };
}

// Inline converter: hex string → IBrush
file class HexToBrushConverter : Avalonia.Data.Converters.IValueConverter
{
    public static readonly HexToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            try { return new SolidColorBrush(Color.Parse(hex)); } catch { }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}
