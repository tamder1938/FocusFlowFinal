using LiteDB;
using System;

namespace FocusFlowFinal.Models.Sound;

public class UserSound
{
    [BsonId] public int    Id          { get; set; }
    public string?         UserId      { get; set; }
    public string          DisplayName { get; set; } = string.Empty;
    public string          FilePath    { get; set; } = string.Empty;
    public string          Icon        { get; set; } = "🎵";
    public string          Color       { get; set; } = "#8B8B9E";
    public DateTime        CreatedAt   { get; set; } = DateTime.Now;
}
