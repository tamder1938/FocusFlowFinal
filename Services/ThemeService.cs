using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using FocusFlowFinal.Models;

namespace FocusFlowFinal.Services;

public sealed class ThemeService
{
    public static ThemeService Instance { get; } = new();

    private AppTheme _currentTheme = AppTheme.Standard;
    private bool _isApplying;

    public AppTheme CurrentTheme => _currentTheme;

    public void ApplyTheme(AppTheme theme)
    {
        if (_isApplying) return;
        _isApplying = true;
        try
        {
            _currentTheme = theme;
            DoApply();
        }
        finally
        {
            _isApplying = false;
        }
    }

    public void ReapplyCurrentTheme() => ApplyTheme(_currentTheme);

    private void DoApply()
    {
        var app = Application.Current!;
        var r   = app.Resources;
        var p   = ThemeCatalog.Palettes[_currentTheme];
        bool isDark = app.RequestedThemeVariant == ThemeVariant.Dark;

        var accent   = Color.Parse(p.Accent);
        var main     = Color.Parse(p.Main);
        var light    = Color.Parse(p.Light);
        var dark     = Color.Parse(p.Dark);
        var onAccent = Color.Parse(p.OnAccent);

        // ── Accent brushes (всегда, независимо от light/dark) ───────────────
        Set(r, "AccentBrush",                   accent);
        Set(r, "AccentBackground",              accent);
        Set(r, "AccentHoverBrush",              dark);
        Set(r, "AccentHoverBrush2",             dark);
        Set(r, "AccentPressedBrush",            dark);
        Set(r, "OnAccentBrush",                 onAccent);
        Set(r, "NavActiveBg",                   accent);
        Set(r, "NavActiveFg",                   onAccent);
        Set(r, "SideNavButtonActiveBackground", accent);
        Set(r, "PrimaryActionBrush",            accent);
        Set(r, "PrimaryActionHoverBrush",       dark);

        // Мягкие акцентные подложки
        Set(r, "AccentSoftBrush",  Color.FromArgb(0x47, main.R, main.G, main.B));
        Set(r, "AccentMutedBrush", light);
        Set(r, "AccentLightBrush",  light);
        Set(r, "AccentLightBrush2", light);
        Set(r, "CardBorderBrush",  Color.FromArgb(0x66, main.R, main.G, main.B));

        // Текст с акцентом и служебные цвета
        Set(r, "AccentTextBrush",  isDark ? onAccent : dark);
        Set(r, "AccentTextBrush2", isDark ? onAccent : dark);
        Set(r, "HotkeyText",       isDark ? light : accent);
        Set(r, "HeaderForeground", isDark ? Color.Parse("#F1F3FF") : dark);

        // ── Light-mode: фон, поверхности, текст ─────────────────────────────
        // В dark-mode эти ключи уже заданы App.ApplyTheme — не перезаписываем.
        if (!isDark)
        {
            var white     = Color.FromRgb(0xFF, 0xFF, 0xFF);
            var borderC   = Color.FromArgb(0x66, main.R, main.G, main.B);
            var textSec   = Color.FromArgb(0xB3, dark.R, dark.G, dark.B);
            var textTer   = Color.FromArgb(0x7A, dark.R, dark.G, dark.B);
            var progressC = Color.FromArgb(0x40, main.R, main.G, main.B);

            Set(r, "PageBackgroundBrush",         light);
            Set(r, "WindowBackgroundBrush",        light);
            Set(r, "SurfaceBrush",                 white);
            Set(r, "MutedBackgroundBrush",         light);
            Set(r, "CardBackgroundBrush",          white);
            Set(r, "CardBackground",               white);
            Set(r, "CardSecondaryBackgroundBrush", light);
            Set(r, "SettingsWindowBackground",     light);
            Set(r, "SidePanelBackground",          light);
            Set(r, "InputBackgroundBrush",         white);
            Set(r, "ButtonBackgroundBrush",        white);
            Set(r, "ButtonForegroundBrush",        dark);
            Set(r, "NavContainerBg",               light);

            Set(r, "TextPrimaryBrush",   dark);
            Set(r, "TextSecondaryBrush", textSec);
            Set(r, "TextTertiaryBrush",  textTer);
            Set(r, "PrimaryTextBrush",   dark);
            Set(r, "SecondaryTextBrush", textSec);
            Set(r, "PrimaryText",        dark);
            Set(r, "SecondaryText",      textSec);

            Set(r, "BorderBrush",              borderC);
            Set(r, "CardBorder",               borderC);
            Set(r, "NavInactiveFg",            textSec);
            Set(r, "ProgressBackgroundBrush",  progressC);
        }
    }

    private static void Set(IResourceDictionary r, string key, Color c) =>
        r[key] = new SolidColorBrush(c);
}
