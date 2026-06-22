using System.Collections.Generic;
using FocusFlowFinal.Models;

namespace FocusFlowFinal.Services;

public static class ThemeCatalog
{
    public static readonly IReadOnlyDictionary<AppTheme, ThemePalette> Palettes =
        new Dictionary<AppTheme, ThemePalette>
        {
            [AppTheme.Standard]   = new("Стандартная",   "#EAF1FE", "#6E9BF0", "#2F6FED", "#173A87", "#FFFFFF"),
            [AppTheme.Cyan]       = new("Голубой",        "#E0F7FA", "#4DD0E1", "#00ACC1", "#006064", "#FFFFFF"),
            [AppTheme.LightGreen] = new("Светло-зелёный", "#E8F5E9", "#81C784", "#43A047", "#1B5E20", "#FFFFFF"),
            [AppTheme.Purple]     = new("Фиолетовый",     "#F3E5F5", "#CE93D8", "#8E24AA", "#4A148C", "#FFFFFF"),
            [AppTheme.Pink]       = new("Розовый",        "#FDF2F8", "#F48FB1", "#D81B60", "#880E4F", "#FFFFFF"),
            [AppTheme.Yellow]     = new("Жёлтый",         "#FFFDE7", "#FFD54F", "#FFB300", "#E65100", "#212121"),
            [AppTheme.Red]        = new("Красный",        "#FFEBEE", "#EF5350", "#D32F2F", "#B71C1C", "#FFFFFF"),
            [AppTheme.DarkGray]   = new("Тёмно-серый",   "#F5F5F5", "#9E9E9E", "#424242", "#212121", "#FFFFFF"),
        };
}
