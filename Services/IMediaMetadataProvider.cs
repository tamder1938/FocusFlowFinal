using FocusFlowFinal.Models.Media;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public class MediaSearchResult
{
    public string    Title         { get; set; } = string.Empty;
    public string    OriginalTitle { get; set; } = string.Empty;
    public int?      Year          { get; set; }
    public string?   PosterUrl     { get; set; }
    public string    Description   { get; set; } = string.Empty;
}

// Заглушка — будущая интеграция с TMDB/MAL/Shikimori
public interface IMediaMetadataProvider
{
    Task<List<MediaSearchResult>> SearchAsync(string query, MediaType type);
}

public class NullMediaMetadataProvider : IMediaMetadataProvider
{
    public Task<List<MediaSearchResult>> SearchAsync(string query, MediaType type)
        => Task.FromResult(new List<MediaSearchResult>());
}
