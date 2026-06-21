using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public record SuggestItem(string Title, string Subtitle, string FullAddress);
public record GeocodeResult(string DisplayName, double Latitude, double Longitude);

public interface IYandexMapsService
{
    bool IsConfigured { get; }
    Task<List<SuggestItem>> GetSuggestionsAsync(string text, CancellationToken ct = default);
    Task<GeocodeResult?> GeocodeAsync(string address, CancellationToken ct = default);
    Task<string?> DownloadStaticMapAsync(double lat, double lon);
}
