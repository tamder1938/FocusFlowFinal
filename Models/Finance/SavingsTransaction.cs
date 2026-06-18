using LiteDB;
using System;

namespace FocusFlowFinal.Models.Finance;

public class SavingsTransaction
{
    [BsonId]
    public int Id { get; set; }

    public int AccountId { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>Сумма: положительная = пополнение/проценты, отрицательная = снятие.</summary>
    public decimal Amount { get; set; }

    /// <summary>"deposit" | "withdrawal" | "interest"</summary>
    public string Type { get; set; } = "deposit";

    public string Note { get; set; } = string.Empty;
}
