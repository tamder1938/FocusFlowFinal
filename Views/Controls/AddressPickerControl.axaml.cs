using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FocusFlowFinal.Models;
using FocusFlowFinal.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FocusFlowFinal.Views.Controls;

/// <summary>
/// Поле ввода адреса с автодополнением Яндекс.Карт, мини-картой и ссылкой.
/// SelectedLocation — двусторонний StyledProperty; родитель привязывает его к VM.
/// </summary>
public partial class AddressPickerControl : UserControl
{
    // ── StyledProperty: выбранное место ─────────────────────────────────
    public static readonly StyledProperty<PlaceLocation?> SelectedLocationProperty =
        AvaloniaProperty.Register<AddressPickerControl, PlaceLocation?>(
            nameof(SelectedLocation), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public PlaceLocation? SelectedLocation
    {
        get => GetValue(SelectedLocationProperty);
        set => SetValue(SelectedLocationProperty, value);
    }

    // ── Локализация ──────────────────────────────────────────────────────
    public LocalizationService Loc => LocalizationService.Instance;

    // ── Состояние ────────────────────────────────────────────────────────
    private IYandexMapsService? _maps;
    private DispatcherTimer?    _debounce;
    private CancellationTokenSource _cts = new();

    private List<SuggestItem> _suggestions = new();
    private bool _suppressTextChange;

    public AddressPickerControl()
    {
        InitializeComponent();
        DataContext = this;

        // Resolve service
        _maps = ((App?)Avalonia.Application.Current)?.Services
            ?.GetService<IYandexMapsService>();

        // Debounce timer
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _debounce.Tick += async (_, _) =>
        {
            _debounce.Stop();
            await FetchSuggestionsAsync();
        };

        // Show hint if no API keys
        Loaded += (_, _) => UpdateNoApiHint();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedLocationProperty)
            OnSelectedLocationChanged(change.GetNewValue<PlaceLocation?>());
    }

    private void UpdateNoApiHint()
    {
        if (NoApiHint != null)
            NoApiHint.IsVisible = _maps?.IsConfigured == false;
    }

    // ── Реакция на внешнее изменение SelectedLocation ───────────────────
    private void OnSelectedLocationChanged(PlaceLocation? loc)
    {
        if (loc == null)
        {
            if (AddressBox != null && !_suppressTextChange)
            {
                _suppressTextChange = true;
                AddressBox.Text = string.Empty;
                _suppressTextChange = false;
            }
            SetMapVisible(false);
            if (ClearBtn != null) ClearBtn.IsVisible = false;
            if (OpenMapsBtn != null) OpenMapsBtn.IsVisible = false;
        }
        else
        {
            if (AddressBox != null && AddressBox.Text != loc.DisplayName)
            {
                _suppressTextChange = true;
                AddressBox.Text = loc.DisplayName;
                _suppressTextChange = false;
            }
            if (ClearBtn != null) ClearBtn.IsVisible = true;
            if (OpenMapsBtn != null) OpenMapsBtn.IsVisible = loc.HasCoordinates;
            LoadMapImage(loc.CachedMapPath);
        }
    }

    // ── TextChanged: debounce suggest ────────────────────────────────────
    private void AddressBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressTextChange) return;
        var text = AddressBox?.Text ?? string.Empty;

        // Если пользователь вручную стёр текст — очищаем выбор
        if (string.IsNullOrWhiteSpace(text))
        {
            SelectedLocation = null;
            CloseSuggestPopup();
            _debounce?.Stop();
            return;
        }

        if (ClearBtn != null) ClearBtn.IsVisible = true;

        if (text.Length < 3 || _maps?.IsConfigured == false)
        {
            CloseSuggestPopup();
            return;
        }

        _debounce?.Stop();
        _debounce?.Start();
    }

    // ── Fetch suggestions ────────────────────────────────────────────────
    private async Task FetchSuggestionsAsync()
    {
        var text = AddressBox?.Text ?? string.Empty;
        if (text.Length < 3 || _maps == null) return;

        _cts.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            _suggestions = await _maps.GetSuggestionsAsync(text, ct);
        }
        catch (OperationCanceledException) { return; }
        catch { _suggestions = new List<SuggestItem>(); }

        if (ct.IsCancellationRequested) return;
        ShowSuggestions();
    }

    private void ShowSuggestions()
    {
        if (SuggestList == null || SuggestPopup == null) return;

        // Build items manually (can't use DataTemplate with records cleanly in code-behind)
        var panel = new StackPanel();
        foreach (var s in _suggestions)
        {
            var btn = new Button
            {
                Background             = Avalonia.Media.Brushes.Transparent,
                HorizontalAlignment    = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Padding                = new Thickness(8, 5),
                CornerRadius           = new CornerRadius(4),
                Tag                    = s
            };
            var sp = new StackPanel { Spacing = 2 };
            sp.Children.Add(new TextBlock
            {
                Text         = s.Title,
                FontSize     = 13,
                FontWeight   = Avalonia.Media.FontWeight.Medium,
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                Foreground   = (Avalonia.Media.IBrush?)Application.Current?.Resources["ForegroundBrush"]
                               ?? Avalonia.Media.Brushes.Black
            });
            if (!string.IsNullOrWhiteSpace(s.Subtitle))
            {
                sp.Children.Add(new TextBlock
                {
                    Text         = s.Subtitle,
                    FontSize     = 11,
                    TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
                    TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                    Foreground   = (Avalonia.Media.IBrush?)Application.Current?.Resources["SecondaryForegroundBrush"]
                                   ?? Avalonia.Media.Brushes.Gray
                });
            }
            btn.Content = sp;
            btn.Click += SuggestItem_Click_Handler;
            panel.Children.Add(btn);
        }

        SuggestList.Content = panel;
        SuggestPopup.IsOpen     = _suggestions.Count > 0;
    }

    private void SuggestItem_Click(object? sender, RoutedEventArgs e) { }  // placeholder — handled by SuggestItem_Click_Handler

    private async void SuggestItem_Click_Handler(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: SuggestItem item }) return;
        CloseSuggestPopup();
        await SelectSuggestion(item);
    }

    private async Task SelectSuggestion(SuggestItem item)
    {
        _suppressTextChange = true;
        if (AddressBox != null) AddressBox.Text = item.FullAddress;
        _suppressTextChange = false;

        if (ClearBtn   != null) ClearBtn.IsVisible   = true;

        var loc = new PlaceLocation { DisplayName = item.FullAddress };

        // Геокодируем
        if (_maps != null)
        {
            var geo = await _maps.GeocodeAsync(item.FullAddress);
            if (geo != null)
            {
                loc.Latitude  = geo.Latitude;
                loc.Longitude = geo.Longitude;
                if (!string.IsNullOrWhiteSpace(geo.DisplayName))
                    loc.DisplayName = geo.DisplayName;

                // Скачиваем статическую карту
                var mapPath = await _maps.DownloadStaticMapAsync(geo.Latitude, geo.Longitude);
                loc.CachedMapPath = mapPath;
                LoadMapImage(mapPath);
            }
        }

        if (OpenMapsBtn != null) OpenMapsBtn.IsVisible = loc.HasCoordinates;
        SelectedLocation = loc;
    }

    // ── Клавиша Escape — закрыть список ──────────────────────────────────
    private void AddressBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) CloseSuggestPopup();
    }

    // ── Кнопка × — очистить ──────────────────────────────────────────────
    private void ClearBtn_Click(object? sender, RoutedEventArgs e)
    {
        SelectedLocation = null;
        CloseSuggestPopup();
    }

    // ── Открыть в браузере ────────────────────────────────────────────────
    private void OpenMapsBtn_Click(object? sender, RoutedEventArgs e)
    {
        var url = SelectedLocation?.YandexMapsUrl;
        if (string.IsNullOrEmpty(url)) return;
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    // ── Вспомогательные ─────────────────────────────────────────────────
    private void CloseSuggestPopup()
    {
        if (SuggestPopup != null) SuggestPopup.IsOpen = false;
    }

    private void LoadMapImage(string? path)
    {
        if (MapContainer == null || MapImage == null) return;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            SetMapVisible(false);
            return;
        }
        try
        {
            using var stream = File.OpenRead(path);
            MapImage.Source = new Bitmap(stream);
            SetMapVisible(true);
        }
        catch { SetMapVisible(false); }
    }

    private void SetMapVisible(bool visible)
    {
        if (MapContainer != null) MapContainer.IsVisible = visible;
    }
}
