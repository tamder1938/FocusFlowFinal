using FocusFlowFinal.Models.YearStats;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public interface IYearStatisticsService
{
    YearSummaryData         GetYearSummary(int year);
    DayActivityData         GetDayActivity(DateTime date);
    IReadOnlyList<HeatCell> GetHeatmap(int year);
}
