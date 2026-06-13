namespace FocusFlowFinal.Models;

public class EventDisplayItem
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Color { get; set; }
    public double Top { get; set; }
    public double Height { get; set; }
    public double Left { get; set; } = 4.0;
    public double Width { get; set; } = 250.0;
    public string? TimeLabel { get; set; }
    public CalendarEvent? OriginalEvent { get; set; }
}
