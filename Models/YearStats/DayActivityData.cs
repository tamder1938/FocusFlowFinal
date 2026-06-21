using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Models.YearStats;

public class DayActivityData
{
    public DateTime     Date              { get; set; }

    // Tasks
    public List<string> CompletedTasks   { get; set; } = new();

    // Habits
    public List<string> DoneHabits       { get; set; } = new();
    public List<string> SkippedHabits    { get; set; } = new();
    public int          TotalHabits      { get; set; }

    // Events
    public List<string> Events           { get; set; } = new();

    // Pomodoro
    public int          PomodoroCount    { get; set; }
    public int          PomodoroTotalMin { get; set; }

    // Workout
    public bool         HasWorkout       { get; set; }
    public string       WorkoutName      { get; set; } = string.Empty;
    public int          WorkoutExercises { get; set; }
    public double       WorkoutTonnageKg { get; set; }

    // Mood
    public int?         MoodLevel        { get; set; }

    // Note
    public bool         HasNote          { get; set; }
    public string       NotePreview      { get; set; } = string.Empty;
}
