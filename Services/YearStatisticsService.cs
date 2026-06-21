using FocusFlowFinal.Models;
using FocusFlowFinal.Models.Habits;
using FocusFlowFinal.Models.Media;
using FocusFlowFinal.Models.YearStats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Services;

public class YearStatisticsService : IYearStatisticsService
{
    private readonly IDatabaseService   _db;
    private readonly IWorkoutRepository _workout;

    private static readonly string[] MonthNamesRu =
    {
        "Январь","Февраль","Март","Апрель","Май","Июнь",
        "Июль","Август","Сентябрь","Октябрь","Ноябрь","Декабрь"
    };

    public YearStatisticsService(IDatabaseService db, IWorkoutRepository workout)
    {
        _db      = db;
        _workout = workout;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public YearSummaryData GetYearSummary(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end   = new DateTime(year + 1, 1, 1);

        return new YearSummaryData
        {
            Year     = year,
            Tasks    = BuildTaskStats(start, end),
            Focus    = BuildFocusStats(year, start, end),
            Events   = BuildEventStats(start, end),
            Habits   = BuildHabitStats(year, start, end),
            Workouts = BuildWorkoutStats(start, end),
            Mood     = BuildMoodStats(start, end),
            Notes    = BuildNotesStats(start, end),
            Media    = BuildMediaStats(year),
        };
    }

    public DayActivityData GetDayActivity(DateTime date)
    {
        var d    = date.Date;
        var next = d.AddDays(1);
        var data = new DayActivityData { Date = d };

        // Tasks completed on this date (by DueDate)
        data.CompletedTasks = _db.GetTasksByDate(d)
            .Where(t => t.IsCompleted)
            .Select(t => t.Title)
            .ToList();

        // Habits
        var habits      = _db.GetAllHabits().Where(h => !h.IsArchived).ToList();
        var completions = _db.GetCompletionsForPeriod(d, next).ToList();
        data.TotalHabits   = habits.Count;
        data.DoneHabits    = habits
            .Where(h => completions.Any(c => c.HabitId == h.Id && c.Status >= 2))
            .Select(h => h.Name).ToList();
        data.SkippedHabits = habits
            .Where(h => !completions.Any(c => c.HabitId == h.Id && c.Status >= 2))
            .Select(h => h.Name).ToList();

        // Events
        data.Events = _db.GetEventsForPeriod(d, next)
            .Select(e => e.Title)
            .ToList();

        // Pomodoro
        var sessions         = _db.GetSessionsForDate(d).Where(s => s.IsCompleted).ToList();
        data.PomodoroCount   = sessions.Count;
        data.PomodoroTotalMin = sessions.Sum(s => s.ActualMinutes);

        // Workout
        var workout = _workout.GetSessionsForPeriod(d, next).FirstOrDefault();
        if (workout != null)
        {
            data.HasWorkout       = true;
            data.WorkoutName      = workout.DayName;
            data.WorkoutExercises = workout.Exercises.Count;
            data.WorkoutTonnageKg = workout.TotalTonnage;
        }

        // Mood
        data.MoodLevel = _db.GetMoodEntriesForPeriod(d, next).FirstOrDefault()?.Level;

        // Note
        var note = _db.SearchNotes(null, null, d, next).FirstOrDefault();
        if (note != null)
        {
            data.HasNote     = true;
            data.NotePreview = note.MarkdownContent?.Length > 120
                ? note.MarkdownContent[..120] + "…"
                : note.MarkdownContent ?? string.Empty;
        }

        return data;
    }

    public IReadOnlyList<HeatCell> GetHeatmap(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end   = new DateTime(year + 1, 1, 1);

        // Pre-aggregate all data for the year
        var tasksByDay = _db.GetTasksForPeriod(start, end)
            .Where(t => t.IsCompleted && t.DueDate.HasValue)
            .GroupBy(t => t.DueDate!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var habitsByDay = _db.GetCompletionsForPeriod(start, end)
            .Where(c => c.Status >= 2)
            .GroupBy(c => c.Date.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var pomodoroByDay = _db.GetSessionsForPeriod(start, end)
            .Where(s => s.IsCompleted)
            .GroupBy(s => s.StartTime.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var eventsByDay = _db.GetEventsForPeriod(start, end)
            .GroupBy(e => e.Start.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var workoutsByDay = _workout.GetSessionsForPeriod(start, end)
            .GroupBy(s => s.StartedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var moodByDay = _db.GetMoodEntriesForPeriod(start, end)
            .GroupBy(e => e.Date.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var noteDates = _db.GetNoteDates(start, end);

        // Compute raw score per day
        int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
        var rawScores  = new List<(DateTime Date, double Score)>(daysInYear);

        for (var d = start; d < end; d = d.AddDays(1))
        {
            double s = 0;
            s += tasksByDay.GetValueOrDefault(d, 0);
            s += habitsByDay.GetValueOrDefault(d, 0);
            s += pomodoroByDay.GetValueOrDefault(d, 0);
            s += eventsByDay.GetValueOrDefault(d, 0);
            s += workoutsByDay.GetValueOrDefault(d, 0) * 2;
            if (moodByDay.ContainsKey(d)) s += 0.5;
            if (noteDates.Contains(d))    s += 0.5;
            rawScores.Add((d, s));
        }

        // Quantile thresholds from non-zero days
        var nonZeroSorted = rawScores
            .Where(x => x.Score > 0)
            .Select(x => x.Score)
            .OrderBy(x => x)
            .ToList();

        double q25 = Quantile(nonZeroSorted, 0.25);
        double q50 = Quantile(nonZeroSorted, 0.50);
        double q75 = Quantile(nonZeroSorted, 0.75);

        return rawScores.Select(x => new HeatCell
        {
            Date          = x.Date,
            ActivityScore = x.Score,
            Level         = x.Score <= 0   ? 0
                          : x.Score <= q25 ? 1
                          : x.Score <= q50 ? 2
                          : x.Score <= q75 ? 3
                                           : 4
        }).ToList();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private TaskYearStats BuildTaskStats(DateTime start, DateTime end)
    {
        var tasks    = _db.GetTasksForPeriod(start, end).ToList();
        var projects = _db.GetAllProjects().ToDictionary(p => p.Id);

        var top3 = tasks
            .GroupBy(t => t.ProjectId ?? 0)
            .Select(g =>
            {
                bool hasProj = g.Key > 0 && projects.TryGetValue(g.Key, out var p);
                return (
                    Name:  hasProj ? projects[g.Key].Name  : "Без проекта",
                    Color: hasProj ? projects[g.Key].Color : "#6B7280",
                    Count: g.Count()
                );
            })
            .OrderByDescending(x => x.Count)
            .Take(3)
            .ToList();

        return new TaskYearStats
        {
            TotalCreated   = tasks.Count,
            TotalCompleted = tasks.Count(t => t.IsCompleted),
            Top3Projects   = top3
        };
    }

    private FocusYearStats BuildFocusStats(int year, DateTime start, DateTime end)
    {
        var sessions = _db.GetSessionsForPeriod(start, end)
                         .Where(s => s.IsCompleted)
                         .ToList();

        var monthlyMin = new double[12];
        foreach (var s in sessions)
            monthlyMin[s.StartTime.Month - 1] += s.ActualMinutes;

        double totalHours = monthlyMin.Sum() / 60.0;
        int    daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;

        // Most productive day
        var byDay    = sessions.GroupBy(s => s.StartTime.Date).ToList();
        var bestDay  = byDay.MaxBy(g => g.Sum(s => s.ActualMinutes));
        DateTime? bestDayDate   = bestDay?.Key;
        double    bestDayHours  = bestDay != null ? bestDay.Sum(s => s.ActualMinutes) / 60.0 : 0;

        // Most productive month
        int bestMonthIndex = 0;
        for (int i = 1; i < 12; i++)
            if (monthlyMin[i] > monthlyMin[bestMonthIndex]) bestMonthIndex = i;

        return new FocusYearStats
        {
            TotalHours               = Math.Round(totalHours, 1),
            AvgHoursPerDay           = Math.Round(totalHours / daysInYear, 2),
            MostProductiveDay        = bestDayDate,
            MostProductiveDayHours   = Math.Round(bestDayHours, 1),
            MostProductiveMonth      = MonthNamesRu[bestMonthIndex],
            MostProductiveMonthHours = Math.Round(monthlyMin[bestMonthIndex] / 60.0, 1),
            MonthlyHours             = monthlyMin.Select(m => Math.Round(m / 60.0, 1)).ToArray()
        };
    }

    private EventYearStats BuildEventStats(DateTime start, DateTime end)
    {
        var events     = _db.GetEventsForPeriod(start, end).ToList();
        double totHours = events.Sum(e => Math.Max(0, (e.End - e.Start).TotalHours));

        // Group by color as proxy for category
        var top3 = events
            .GroupBy(e => e.Color)
            .Select(g => (Name: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(3)
            .ToList();

        return new EventYearStats
        {
            TotalEvents    = events.Count,
            TotalHours     = Math.Round(totHours, 1),
            Top3Categories = top3
        };
    }

    private HabitYearStats BuildHabitStats(int year, DateTime start, DateTime end)
    {
        var habits      = _db.GetAllHabits().Where(h => !h.IsArchived).ToList();
        var completions = _db.GetCompletionsForPeriod(start, end).ToList();
        int daysInYear  = DateTime.IsLeapYear(year) ? 366 : 365;

        // Per-habit stats + longest streak
        int    globalBest   = 0;
        string globalHabit  = "";

        var perHabit = habits.Select(h =>
        {
            var doneDays = completions
                .Where(c => c.HabitId == h.Id && c.Status >= 2)
                .Select(c => c.Date.Date)
                .ToHashSet();

            // Streak scan
            int streak = 0, best = 0;
            for (var d = start.Date; d < end; d = d.AddDays(1))
            {
                if (doneDays.Contains(d)) { streak++; if (streak > best) best = streak; }
                else streak = 0;
            }
            if (best > globalBest) { globalBest = best; globalHabit = h.Name; }

            double pct = daysInYear > 0 ? doneDays.Count * 100.0 / daysInYear : 0;
            return (h.Name, Percent: pct);
        }).ToList();

        double avgPct = perHabit.Count > 0 ? perHabit.Average(x => x.Percent) : 0;

        return new HabitYearStats
        {
            ActiveHabits         = habits.Count,
            LongestStreak        = globalBest,
            LongestStreakHabit   = globalHabit,
            AvgCompletionPercent = Math.Round(avgPct, 1),
            Top3Stable           = perHabit.OrderByDescending(x => x.Percent).Take(3).ToList()
        };
    }

    private WorkoutYearStats BuildWorkoutStats(DateTime start, DateTime end)
    {
        var sessions = _workout.GetSessionsForPeriod(start, end).ToList();

        double tonnageKg = sessions.Sum(s => s.TotalTonnage);
        double hours     = sessions.Sum(s => s.Duration.TotalHours);

        var fav = sessions
            .SelectMany(s => s.Exercises)
            .GroupBy(e => e.ExerciseName)
            .Select(g => (Name: g.Key, Sets: g.Sum(e => e.Sets.Count(s => s.IsCompleted))))
            .MaxBy(x => x.Sets);

        return new WorkoutYearStats
        {
            TotalSessions    = sessions.Count,
            TotalTonnageTons = Math.Round(tonnageKg / 1000.0, 2),
            TotalHours       = Math.Round(hours, 1),
            FavoriteExercise = fav.Name ?? "—"
        };
    }

    private MoodYearStats BuildMoodStats(DateTime start, DateTime end)
    {
        var entries = _db.GetMoodEntriesForPeriod(start, end).ToList();

        var dist = Enumerable.Range(1, 5)
            .ToDictionary(l => l, l => entries.Count(e => e.Level == l));

        string bestMonth = "";
        double bestPct   = 0;
        for (int m = 1; m <= 12; m++)
        {
            var mEntries = entries.Where(e => e.Date.Month == m).ToList();
            if (mEntries.Count == 0) continue;
            double pct = mEntries.Count(e => e.Level >= 4) * 100.0 / mEntries.Count;
            if (pct > bestPct) { bestPct = pct; bestMonth = MonthNamesRu[m - 1]; }
        }

        return new MoodYearStats
        {
            Distribution     = dist,
            BestMonth        = bestMonth,
            BestMonthGoodPct = Math.Round(bestPct, 1),
            TotalEntries     = entries.Count
        };
    }

    private NotesYearStats BuildNotesStats(DateTime start, DateTime end)
    {
        var notes = _db.SearchNotes(null, null, start, end).ToList();

        var top3 = notes
            .SelectMany(n => n.Tags)
            .GroupBy(t => t)
            .Select(g => (Tag: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(3)
            .ToList();

        int daysWithNotes = notes.Select(n => n.Date.Date).Distinct().Count();

        return new NotesYearStats
        {
            TotalNotes    = notes.Count,
            Top3Tags      = top3,
            DaysWithNotes = daysWithNotes
        };
    }

    private MediaYearStats BuildMediaStats(int year)
    {
        var items = _db.GetAllMediaItems()
            .Where(m => m.Status == MediaStatus.Completed
                     && m.CompletedAt.HasValue
                     && m.CompletedAt.Value.Year == year)
            .ToList();

        double avg = items.Where(m => m.OverallScore > 0).Select(m => m.OverallScore)
                         .DefaultIfEmpty(0).Average();

        return new MediaYearStats
        {
            CompletedMovies = items.Count(m => m.Type == MediaType.Movie),
            CompletedSeries = items.Count(m => m.Type == MediaType.Series),
            CompletedAnime  = items.Count(m => m.Type == MediaType.Anime),
            CompletedBooks  = items.Count(m => m.Type == MediaType.Book),
            CompletedManga  = items.Count(m => m.Type == MediaType.Manga),
            AvgScore        = Math.Round(avg, 1),
            Top3            = items
                .Where(m => m.OverallScore > 0)
                .OrderByDescending(m => m.OverallScore)
                .Take(3)
                .Select(m => (m.Title, m.OverallScore, m.PosterPath))
                .ToList()
        };
    }

    private static double Quantile(List<double> sorted, double q)
    {
        if (sorted.Count == 0) return 0;
        double pos = q * (sorted.Count - 1);
        int    lo  = (int)Math.Floor(pos);
        int    hi  = Math.Min(lo + 1, sorted.Count - 1);
        return sorted[lo] + (pos - lo) * (sorted[hi] - sorted[lo]);
    }
}
