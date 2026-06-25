using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Models.Mood;

public class MoodEntry
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public int Level { get; set; } = 3;                    // 1=Ужасно … 5=Супер
    public List<int> ActivityIds { get; set; } = new();
    public string Comment { get; set; } = string.Empty;
    public List<string> PhotoFiles { get; set; } = new();  // имена файлов в MoodPhotos/
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
