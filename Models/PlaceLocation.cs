using System;

namespace FocusFlowFinal.Models;

public class PlaceLocation
{
    public string DisplayName   { get; set; } = string.Empty;
    public double? Latitude     { get; set; }
    public double? Longitude    { get; set; }
    public string? CachedMapPath { get; set; }

    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

    public string YandexMapsUrl => HasCoordinates
        ? $"https://maps.yandex.ru/?pt={Longitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},{Latitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}&z=16"
        : $"https://maps.yandex.ru/?text={Uri.EscapeDataString(DisplayName)}";

    public string ShortDisplay =>
        DisplayName.Length > 50 ? DisplayName[..50] + "…" : DisplayName;
}
