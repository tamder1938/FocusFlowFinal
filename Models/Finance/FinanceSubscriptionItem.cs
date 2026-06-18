using LiteDB;
using System;

namespace FocusFlowFinal.Models.Finance;

public enum FinanceBillingCycle
{
    Monthly,
    Yearly
}

public class FinanceSubscriptionItem
{
    [BsonId]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public FinanceBillingCycle BillingCycle { get; set; } = FinanceBillingCycle.Monthly;
    public DateTime NextBillingDate { get; set; } = DateTime.Today;
    public string Category { get; set; } = string.Empty;
    public bool IsPaid { get; set; }

    /// <summary>Минут до NextBillingDate, за которые показывать уведомление. 0 = выключено.</summary>
    public int NotificationOffsetMinutes { get; set; } = 0;
}
