using LiteDB;
using System;

namespace FocusFlowFinal.Models.Finance;

public class FinanceLoan
{
    [BsonId]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DownPayment { get; set; } = 0m;
    public decimal InterestRate { get; set; }
    public int TermMonths { get; set; }
    public int TermYears { get; set; }
    public decimal MonthlyPayment { get; set; }
    public decimal RemainingBalance { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? NextPaymentDate { get; set; }

    /// <summary>Минут до NextPaymentDate, за которые показывать уведомление. 0 = выключено.</summary>
    public int NotificationOffsetMinutes { get; set; } = 0;

    /// <summary>Автовычисление ежемесячного платежа по формуле аннуитета.</summary>
    public void RecalculateMonthlyPayment()
    {
        decimal principal = TotalAmount - DownPayment;
        if (principal <= 0) { MonthlyPayment = 0; return; }

        int months = TermMonths > 0 ? TermMonths : TermYears * 12;
        if (months <= 0) { MonthlyPayment = 0; return; }

        if (InterestRate <= 0)
        {
            MonthlyPayment = principal / months;
            return;
        }
        double r = (double)InterestRate / 100.0 / 12.0;
        double p = (double)principal;
        double n = months;
        MonthlyPayment = (decimal)(p * r * Math.Pow(1 + r, n) / (Math.Pow(1 + r, n) - 1));
    }
}
