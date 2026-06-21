using FocusFlowFinal.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public class YandexMapsService : IYandexMapsService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    private readonly string _cacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusFlow", "MapCache");

    private AppSettings Settings => AppSettings.Load();

    // ── Keys with fallback to appsettings.json ──────────────────────────
    private string? SuggestKey  => Coalesce(Settings.YandexSuggestApiKey,  _fileConfig?.Suggest);
    private string? GeocoderKey => Coalesce(Settings.YandexGeocoderApiKey, _fileConfig?.Geocoder);
    private string? StaticKey   => Coalesce(Settings.YandexStaticApiKey,   _fileConfig?.Static);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SuggestKey) ||
        !string.IsNullOrWhiteSpace(GeocoderKey) ||
        !string.IsNullOrWhiteSpace(StaticKey);

    private static string? Coalesce(string? userKey, string? fileKey) =>
        !string.IsNullOrWhiteSpace(userKey) ? userKey :
        !string.IsNullOrWhiteSpace(fileKey) ? fileKey : null;

    // ── Optional appsettings.json config ────────────────────────────────
    private record FileConfig(string? Suggest, string? Geocoder, string? Static);
    private FileConfig? _fileConfig;

    public YandexMapsService()
    {
        Directory.CreateDirectory(_cacheDir);
        LoadFileConfig();
    }

    private void LoadFileConfig()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("YandexMaps", out var maps)) return;
            _fileConfig = new FileConfig(
                maps.TryGetProperty("SuggestApiKey",  out var s) ? s.GetString() : null,
                maps.TryGetProperty("GeocoderApiKey", out var g) ? g.GetString() : null,
                maps.TryGetProperty("StaticApiKey",   out var st) ? st.GetString() : null);
        }
        catch { }
    }

    // ── Suggest ──────────────────────────────────────────────────────────
    public async Task<List<SuggestItem>> GetSuggestionsAsync(string text, CancellationToken ct = default)
    {
        var key = SuggestKey;
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(text))
            return new List<SuggestItem>();

        try
        {
            var url = $"https://suggest-maps.yandex.ru/v1/suggest?apikey={Uri.EscapeDataString(key)}" +
                      $"&text={Uri.EscapeDataString(text)}&print_address=1&lang=ru_RU&results=5";
            var json = await _http.GetStringAsync(url, ct);
            return ParseSuggest(json);
        }
        catch (OperationCanceledException) { return new List<SuggestItem>(); }
        catch { return new List<SuggestItem>(); }
    }

    private static List<SuggestItem> ParseSuggest(string json)
    {
        var result = new List<SuggestItem>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results)) return result;
            foreach (var item in results.EnumerateArray())
            {
                var title    = item.TryGetProperty("title",    out var t) ? t.GetProperty("text").GetString() ?? "" : "";
                var subtitle = item.TryGetProperty("subtitle", out var s) ? s.GetProperty("text").GetString() ?? "" : "";
                var full     = item.TryGetProperty("address",  out var a) && a.TryGetProperty("formatted_address", out var f)
                    ? f.GetString() ?? title : title;
                result.Add(new SuggestItem(title, subtitle, full));
            }
        }
        catch { }
        return result;
    }

    // ── Geocode ──────────────────────────────────────────────────────────
    public async Task<GeocodeResult?> GeocodeAsync(string address, CancellationToken ct = default)
    {
        var key = GeocoderKey;
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(address))
            return null;

        try
        {
            var url = $"https://geocode-maps.yandex.ru/1.x/?format=json&apikey={Uri.EscapeDataString(key)}" +
                      $"&geocode={Uri.EscapeDataString(address)}&results=1";
            var json = await _http.GetStringAsync(url, ct);
            return ParseGeocode(json);
        }
        catch { return null; }
    }

    private static GeocodeResult? ParseGeocode(string json)
    {
        try
        {
            using var doc  = JsonDocument.Parse(json);
            var members = doc.RootElement
                .GetProperty("response")
                .GetProperty("GeoObjectCollection")
                .GetProperty("featureMember");
            if (members.GetArrayLength() == 0) return null;

            var obj      = members[0].GetProperty("GeoObject");
            var pos      = obj.GetProperty("Point").GetProperty("pos").GetString() ?? "";
            var parts    = pos.Split(' ');
            if (parts.Length < 2) return null;
            double lon   = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double lat   = double.Parse(parts[1], CultureInfo.InvariantCulture);

            string name  = obj.TryGetProperty("metaDataProperty", out var meta) &&
                           meta.TryGetProperty("GeocoderMetaData", out var gm) &&
                           gm.TryGetProperty("Address", out var addr) &&
                           addr.TryGetProperty("formatted", out var fmt)
                ? fmt.GetString() ?? obj.GetProperty("name").GetString() ?? ""
                : obj.GetProperty("name").GetString() ?? "";

            return new GeocodeResult(name, lat, lon);
        }
        catch { return null; }
    }

    // ── Static Map ───────────────────────────────────────────────────────
    public async Task<string?> DownloadStaticMapAsync(double lat, double lon)
    {
        var key = StaticKey;
        if (string.IsNullOrWhiteSpace(key)) return null;

        string latStr = lat.ToString("F6", CultureInfo.InvariantCulture);
        string lonStr = lon.ToString("F6", CultureInfo.InvariantCulture);
        string fileName = $"{latStr}_{lonStr}.png".Replace('.', '_').Replace('-', 'm');
        string filePath = Path.Combine(_cacheDir, fileName);

        if (File.Exists(filePath)) return filePath;

        try
        {
            string ll  = $"{lonStr},{latStr}";
            var url = $"https://static-maps.yandex.ru/v1?ll={ll}&z=16&size=600,360" +
                      $"&pt={ll},pm2blm&apikey={Uri.EscapeDataString(key)}";
            var bytes = await _http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(filePath, bytes);
            return filePath;
        }
        catch { return null; }
    }
}
