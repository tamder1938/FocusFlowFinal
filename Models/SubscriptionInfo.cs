using System;

namespace FocusFlowFinal.Models;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.3, 6): информация о подписке пользователя.
/// Заполняется сервером при входе/после покупки (см. IPaymentService).
/// </summary>
public class SubscriptionInfo
{
    /// <summary>Идентификатор тарифного плана: "monthly" (300₽/мес) или "yearly" (2000₽/год).</summary>
    public string PlanId { get; set; } = string.Empty;

    /// <summary>Дата окончания текущего оплаченного периода (UTC).</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>true, если подписка активна (текущая дата раньше ExpiresAtUtc).</summary>
    public bool IsActive => DateTime.UtcNow < ExpiresAtUtc;

    /// <summary>Человекочитаемая цена для отображения в UI, например "300 ₽/мес".</summary>
    public string PriceLabel { get; set; } = string.Empty;
}

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.6): доступные тарифные планы подписки.
/// Используется в карточке предложения покупки на вкладке "Аккаунт".
/// </summary>
public static class SubscriptionPlans
{
    public const string Monthly = "monthly";
    public const string Yearly  = "yearly";

    public const decimal MonthlyPriceRub = 300m;
    public const decimal YearlyPriceRub  = 2000m;
}
