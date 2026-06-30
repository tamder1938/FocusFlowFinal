using LiteDB;
using System;
using System.Linq;

namespace FocusFlowFinal.Models;

public class PlaceItem
{
    [BsonId] public int Id { get; set; }
    public string UserId    { get; set; } = "local";
    public string Name      { get; set; } = string.Empty;
    public string Category  { get; set; } = "Прочее";
    public string Status    { get; set; } = "WantToVisit"; // WantToVisit | Visited | Favorite
    public string Address   { get; set; } = string.Empty;
    public double? Latitude  { get; set; }
    public double? Longitude { get; set; }
    public string Notes     { get; set; } = string.Empty;
    public int Rating       { get; set; } // 0 = не оценено, 1-5
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? VisitedAt { get; set; }
    public bool IsDeleted   { get; set; }
    public Guid SyncId      { get; set; } = Guid.NewGuid();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonIgnore] public string StatusLabel => Status switch
    {
        "Visited"  => "✓ Посетил",
        "Favorite" => "⭐ Избранное",
        _          => "⏳ Хочу посетить"
    };

    [BsonIgnore] public string RatingText => Rating == 0 ? "" :
        new string('★', Rating) + new string('☆', 5 - Rating);

    [BsonIgnore] public bool HasAddress    => !string.IsNullOrWhiteSpace(Address);
    [BsonIgnore] public bool HasNotes      => !string.IsNullOrWhiteSpace(Notes);
    [BsonIgnore] public bool HasRating     => Rating > 0;
    [BsonIgnore] public bool IsNotFavorite => Status != "Favorite";
    [BsonIgnore] public bool IsNotVisited  => Status != "Visited";
    [BsonIgnore] public string FavoriteIcon => Status == "Favorite" ? "⭐ Убрать" : "⭐ Избранное";

    [BsonIgnore] public string NotesSnippet =>
        Notes.Length > 80 ? Notes[..80] + "…" : Notes;

    [BsonIgnore] public string VisitedLabel =>
        VisitedAt.HasValue ? $"Посещено {VisitedAt.Value:dd.MM.yyyy}" : string.Empty;
    [BsonIgnore] public bool HasVisitedDate => VisitedAt.HasValue && Status == "Visited";
}
