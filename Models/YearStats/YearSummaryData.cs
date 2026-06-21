using System.Collections.Generic;

namespace FocusFlowFinal.Models.YearStats;

public class TaskYearStats
{
    public int    TotalCreated    { get; set; }
    public int    TotalCompleted  { get; set; }
    public double CompletionPct   => TotalCreated > 0 ? (double)TotalCompleted / TotalCreated * 100 : 0;
    public List<(string Name, string Color, int Count)> Top3Projects { get; set; } = new();
}

public class FocusYearStats
{
    public double    TotalHours               { get; set; }
    public double    AvgHoursPerDay           { get; set; }
    public System.DateTime? MostProductiveDay { get; set; }
    public double    MostProductiveDayHours   { get; set; }
    public string    MostProductiveMonth      { get; set; } = string.Empty;
    public double    MostProductiveMonthHours { get; set; }
    public double[]  MonthlyHours             { get; set; } = new double[12];
}

public class EventYearStats
{
    public int    TotalEvents     { get; set; }
    public double TotalHours      { get; set; }
    public List<(string Name, int Count)> Top3Categories { get; set; } = new();
}

public class HabitYearStats
{
    public int    ActiveHabits          { get; set; }
    public int    LongestStreak         { get; set; }
    public string LongestStreakHabit    { get; set; } = string.Empty;
    public double AvgCompletionPercent  { get; set; }
    public List<(string Name, double Percent)> Top3Stable { get; set; } = new();
}

public class WorkoutYearStats
{
    public int    TotalSessions    { get; set; }
    public double TotalTonnageTons { get; set; }
    public double TotalHours       { get; set; }
    public string FavoriteExercise { get; set; } = string.Empty;
}

public class MoodYearStats
{
    public System.Collections.Generic.Dictionary<int, int> Distribution { get; set; } = new();
    public string BestMonth          { get; set; } = string.Empty;
    public double BestMonthGoodPct   { get; set; }
    public int    TotalEntries       { get; set; }
}

public class NotesYearStats
{
    public int    TotalNotes    { get; set; }
    public List<(string Tag, int Count)> Top3Tags { get; set; } = new();
    public int    DaysWithNotes { get; set; }
}

public class MediaYearStats
{
    public int    CompletedMovies { get; set; }
    public int    CompletedSeries { get; set; }
    public int    CompletedAnime  { get; set; }
    public int    CompletedBooks  { get; set; }
    public int    CompletedManga  { get; set; }
    public double AvgScore        { get; set; }
    public List<(string Title, double Score, string? PosterPath)> Top3 { get; set; } = new();
}

public class YearSummaryData
{
    public int            Year     { get; set; }
    public TaskYearStats    Tasks    { get; set; } = new();
    public FocusYearStats   Focus    { get; set; } = new();
    public EventYearStats   Events   { get; set; } = new();
    public HabitYearStats   Habits   { get; set; } = new();
    public WorkoutYearStats Workouts { get; set; } = new();
    public MoodYearStats    Mood     { get; set; } = new();
    public NotesYearStats   Notes    { get; set; } = new();
    public MediaYearStats   Media    { get; set; } = new();
}
