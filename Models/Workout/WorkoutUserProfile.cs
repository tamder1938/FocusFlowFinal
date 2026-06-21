using LiteDB;
using System;

namespace FocusFlowFinal.Models.Workout;

public class WorkoutUserProfile
{
    [BsonId] public int    Id         { get; set; } = 1;
    public string          Gender     { get; set; } = "male";
    public double          BodyWeight { get; set; } = 80.0;
    public DateTime        UpdatedAt  { get; set; } = DateTime.Now;
}
