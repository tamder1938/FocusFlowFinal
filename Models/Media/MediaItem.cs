using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Models.Media;

public class MediaItem
{
    [BsonId] public int    Id             { get; set; }

    // Основное
    public string          Title          { get; set; } = string.Empty;
    public string          OriginalTitle  { get; set; } = string.Empty;
    public MediaType       Type           { get; set; } = MediaType.Movie;
    public MediaStatus     Status         { get; set; } = MediaStatus.Planned;
    public int?            Year           { get; set; }
    public List<string>    Genres         { get; set; } = new();
    public string          Description    { get; set; } = string.Empty;
    public string?         PosterPath     { get; set; }
    public string          Country        { get; set; } = string.Empty;

    // Фильм/сериал/аниме
    public string          Director       { get; set; } = string.Empty;
    public int             DurationMin    { get; set; }
    public bool            IsOngoing      { get; set; }
    public List<MediaSeason> Seasons      { get; set; } = new();

    // Книга/манга
    public string          Author         { get; set; } = string.Empty;
    public string          Artist         { get; set; } = string.Empty;
    public int             TotalPages     { get; set; }
    public int             CurrentPage    { get; set; }
    public string          ISBN           { get; set; } = string.Empty;
    public int             TotalVolumes   { get; set; }
    public int             TotalChapters  { get; set; }
    public List<MediaChapter> Chapters    { get; set; } = new();

    // Оценки
    public double          ScorePlot      { get; set; }
    public double          ScoreVisual    { get; set; }
    public double          ScorePersonal  { get; set; }
    public double          OverallScore   { get; set; }
    public bool            IsOverallManual { get; set; }
    public string          PersonalNote   { get; set; } = string.Empty;
    public string          Source         { get; set; } = string.Empty;

    // Даты
    public DateTime?       StartedAt      { get; set; }
    public DateTime?       CompletedAt    { get; set; }
    public DateTime        CreatedAt      { get; set; } = DateTime.Now;

    // ── Вычисляемые (BsonIgnore) ──────────────────────────────────────

    [BsonIgnore]
    public bool HasPoster => !string.IsNullOrEmpty(PosterPath);

    [BsonIgnore]
    public int WatchedEpisodes =>
        Seasons.Sum(s => s.Episodes.Count(e => e.IsWatched));

    [BsonIgnore]
    public int TotalEpisodes =>
        Seasons.Sum(s => s.Episodes.Count);

    [BsonIgnore]
    public bool AllEpisodesWatched =>
        TotalEpisodes > 0 && WatchedEpisodes == TotalEpisodes;

    [BsonIgnore]
    public double ProgressFraction =>
        TotalEpisodes > 0 ? (double)WatchedEpisodes / TotalEpisodes
        : TotalPages > 0  ? (double)CurrentPage / TotalPages
        : 0;

    [BsonIgnore]
    public string ProgressLabel =>
        Type is MediaType.Book
            ? $"{CurrentPage} / {TotalPages} стр."
            : TotalEpisodes > 0
                ? $"{WatchedEpisodes} / {TotalEpisodes} эп."
                : string.Empty;

    [BsonIgnore]
    public string TypeYearLabel =>
        $"{TypeLabel} · {Year?.ToString() ?? "—"}";

    [BsonIgnore]
    public string TypeLabel => Type switch
    {
        MediaType.Movie  => "Фильм",
        MediaType.Series => "Сериал",
        MediaType.Anime  => "Аниме",
        MediaType.Book   => "Книга",
        MediaType.Manga  => "Манга",
        _                => string.Empty
    };

    [BsonIgnore]
    public string StatusColor => Status switch
    {
        MediaStatus.Planned    => "#8B8B9E",
        MediaStatus.InProgress => "#3B82F6",
        MediaStatus.Completed  => "#22C55E",
        MediaStatus.Dropped    => "#F59E0B",
        _                      => "#8B8B9E"
    };

    [BsonIgnore]
    public string StarsDisplay
    {
        get
        {
            if (OverallScore <= 0) return string.Empty;
            double stars = OverallScore / 10.0 * 5.0;
            int full  = (int)Math.Floor(stars);
            int empty = 5 - full;
            return new string('★', full) + new string('☆', empty);
        }
    }

    // Расчёт часов
    [BsonIgnore]
    public double TotalHours =>
        Type is MediaType.Movie
            ? DurationMin / 60.0
            : Type is MediaType.Series or MediaType.Anime
                ? WatchedEpisodes * DurationMin / 60.0
                : TotalPages * 4.0 / 60.0;
}
