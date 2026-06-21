using System;

namespace FocusFlowFinal.Models.Media;

public class MediaChapter
{
    public int       Volume        { get; set; }
    public int       ChapterNumber { get; set; }
    public string    Title         { get; set; } = string.Empty;
    public bool      IsRead        { get; set; }
    public DateTime? ReadAt        { get; set; }
}
