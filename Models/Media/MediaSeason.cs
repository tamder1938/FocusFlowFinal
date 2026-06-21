using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Models.Media;

public class MediaSeason
{
    public int                  SeasonNumber { get; set; }
    public string               Title        { get; set; } = string.Empty;
    public List<MediaEpisode>   Episodes     { get; set; } = new();

    public int WatchedCount  => Episodes.Count(e => e.IsWatched);
    public int TotalCount    => Episodes.Count;
    public bool IsCompleted  => TotalCount > 0 && WatchedCount == TotalCount;
}
