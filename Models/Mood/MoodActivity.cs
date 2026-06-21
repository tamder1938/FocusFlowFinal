using System;

namespace FocusFlowFinal.Models.Mood;

public class MoodActivity
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;       // emoji
    public bool IsCustom { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
