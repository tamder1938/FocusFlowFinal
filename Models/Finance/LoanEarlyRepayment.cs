using LiteDB;
using System;

namespace FocusFlowFinal.Models.Finance;

public class LoanEarlyRepayment
{
    [BsonId]
    public int Id { get; set; }

    public int LoanId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;
}
