using FocusFlowFinal.Models.Media;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Services;

public class MediaRepository : IMediaRepository
{
    private readonly IDatabaseService _db;
    public MediaRepository(IDatabaseService db) => _db = db;

    public IEnumerable<MediaItem> GetAll() => _db.GetAllMediaItems();

    public IEnumerable<MediaItem> GetFiltered(MediaFilter f)
    {
        var all = _db.GetAllMediaItems();
        if (f.Type.HasValue)
            all = all.Where(x => x.Type == f.Type.Value);
        if (f.Status.HasValue)
            all = all.Where(x => x.Status == f.Status.Value);
        if (!string.IsNullOrWhiteSpace(f.Query))
        {
            var q = f.Query.ToLower();
            all = all.Where(x =>
                x.Title.ToLower().Contains(q) ||
                x.OriginalTitle.ToLower().Contains(q));
        }
        return all;
    }

    public MediaItem? GetById(int id) => _db.GetMediaItemById(id);
    public int  Upsert(MediaItem item) => _db.UpsertMediaItem(item);
    public void Delete(int id)         => _db.DeleteMediaItem(id);

    public IEnumerable<string> GetAllGenres() =>
        _db.GetAllMediaItems()
           .SelectMany(x => x.Genres)
           .Distinct()
           .OrderBy(g => g)
           .ToList();

    public MediaStatistics GetStatistics()
    {
        var all  = _db.GetAllMediaItems().ToList();
        var year = System.DateTime.Today.Year;

        var typeColors = new Dictionary<MediaType, string>
        {
            [MediaType.Movie]  = "#5C8FD9",
            [MediaType.Series] = "#A8D88A",
            [MediaType.Anime]  = "#E08A3C",
            [MediaType.Book]   = "#8AB7D9",
            [MediaType.Manga]  = "#DC4F4F",
        };
        var typeLabels = new Dictionary<MediaType, string>
        {
            [MediaType.Movie]  = "Фильм",
            [MediaType.Series] = "Сериал",
            [MediaType.Anime]  = "Аниме",
            [MediaType.Book]   = "Книга",
            [MediaType.Manga]  = "Манга",
        };

        var byType = System.Enum.GetValues<MediaType>()
            .Select(t => new MediaTypeCount
            {
                Type  = t,
                Count = all.Count(x => x.Type == t),
                Label = typeLabels[t],
                Color = typeColors[t]
            })
            .Where(x => x.Count > 0)
            .ToList();

        var topGenres = all
            .SelectMany(x => x.Genres)
            .GroupBy(g => g)
            .Select(g => new MediaGenreCount { Genre = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToList();

        return new MediaStatistics
        {
            TotalCount        = all.Count,
            CompletedThisYear = all.Count(x => x.Status == MediaStatus.Completed
                                            && x.CompletedAt?.Year == year),
            PlannedCount      = all.Count(x => x.Status == MediaStatus.Planned),
            DroppedCount      = all.Count(x => x.Status == MediaStatus.Dropped),
            TotalHours        = all.Sum(x => x.TotalHours),
            TopRated          = all.Where(x => x.Status == MediaStatus.Completed && x.OverallScore > 0)
                                   .OrderByDescending(x => x.OverallScore)
                                   .Take(5)
                                   .ToList(),
            ByType            = byType,
            TopGenres         = topGenres,
            FavoriteGenre     = topGenres.FirstOrDefault()?.Genre ?? string.Empty
        };
    }
}
