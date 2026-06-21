using System;

namespace FocusFlowFinal.Models.Media;

public class MediaEpisode
{
    public int       EpisodeNumber { get; set; }
    public string    Title         { get; set; } = string.Empty;
    public int       DurationMin   { get; set; } = 25;
    public bool      IsWatched     { get; set; }
    public DateTime? WatchedAt     { get; set; }
}
