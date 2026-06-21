using System;

namespace FocusFlowFinal.Models.YearStats;

public class HeatCell
{
    public DateTime Date          { get; set; }
    public int      Level         { get; set; } // 0–4
    public double   ActivityScore { get; set; }
}
