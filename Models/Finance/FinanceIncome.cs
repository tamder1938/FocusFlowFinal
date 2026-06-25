using LiteDB;
using System;

namespace FocusFlowFinal.Models.Finance;

public class FinanceIncome
{
    [BsonId]
    public int Id { get; set; }
    public string? UserId { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; } = 0m;
    public string Note { get; set; } = string.Empty;
}
