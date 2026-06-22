using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FocusFlowFinal.Models;

namespace FocusFlowFinal.ViewModels;

public partial class ThemeTileItem : ObservableObject
{
    public AppTheme Theme { get; init; }
    public string Name { get; init; } = "";
    public string ThemeTag { get; init; } = "";
    public SolidColorBrush AccentBrush { get; init; } = new(Color.Parse("#2F6FED"));
    public SolidColorBrush LightBrush  { get; init; } = new(Color.Parse("#EAF1FE"));

    [ObservableProperty] private bool _isSelected;
}
