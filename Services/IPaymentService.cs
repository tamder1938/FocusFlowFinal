using FocusFlowFinal.Models;
using System;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.3, 6): сервис покупки подписки.
///
/// Реальная реализация должна интегрироваться с платёжным провайдером
/// (например, ЮKassa, CloudPayments, Stripe) через их SDK/REST API:
/// 1. Клиент инициирует платёж — сервер создаёт платёжную сессию и
///    возвращает URL формы оплаты.
/// 2. Пользователь оплачивает во встроенном WebView/браузере.
/// 3. Провайдер шлёт webhook на сервер — сервер обновляет подписку
///    пользователя в таблице subscriptions (PostgreSQL).
/// 4. Клиент вызывает <see cref="IAuthService.RestoreSessionAsync"/> или
///    отдельный /api/subscription/verify, чтобы получить обновлённый статус.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Имитация покупки подписки. planId — <see cref="SubscriptionPlans.Monthly"/>
    /// или <see cref="SubscriptionPlans.Yearly"/>. Возвращает обновлённую
    /// информацию о подписке при успехе.
    /// </summary>
    Task<PurchaseResult> PurchaseSubscriptionAsync(string planId);

    /// <summary>Проверка текущего статуса подписки (для периодической ревалидации).</summary>
    Task<SubscriptionInfo?> GetSubscriptionStatusAsync();
}

public class PurchaseResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public SubscriptionInfo? Subscription { get; set; }

    public static PurchaseResult Ok(SubscriptionInfo info) => new() { Success = true, Subscription = info };
    public static PurchaseResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
