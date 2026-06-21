using System.Collections.Generic;

namespace FocusFlowFinal.Models.Media;

public class MediaTypeCount
{
    public MediaType Type  { get; set; }
    public int       Count { get; set; }
    public string    Label { get; set; } = string.Empty;
    public string    Color { get; set; } = "#5C8FD9";
}

public class MediaGenreCount
{
    public string Genre { get; set; } = string.Empty;
    public int    Count { get; set; }
}

public class MediaStatistics
{
    public int                     TotalCount         { get; set; }
    public int                     CompletedThisYear  { get; set; }
    public int                     PlannedCount       { get; set; }
    public int                     DroppedCount       { get; set; }
    public double                  TotalHours         { get; set; }
    public List<MediaItem>         TopRated           { get; set; } = new();
    public List<MediaTypeCount>    ByType             { get; set; } = new();
    public List<MediaGenreCount>   TopGenres          { get; set; } = new();
    public string                  FavoriteGenre      { get; set; } = string.Empty;
}
