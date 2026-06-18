using FocusFlowFinal.Models;
using System;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

/// <summary>
/// ИСПРАВЛЕНО (Часть 2, п.6): заглушка покупки — имитирует успешную оплату
/// без реального платёжного провайдера. Подписка сохраняется локально
/// в AppSettings через UserProfile (в проде — на сервере, в таблице subscriptions).
///
/// TODO (реальный бэкенд): заменить тело PurchaseSubscriptionAsync на
/// POST /api/subscription/purchase { planId }, который вернёт ссылку на
/// оплату или (для тестового режима) сразу активирует подписку.
/// </summary>
public class PaymentServiceStub : IPaymentService
{
    private readonly IAuthService _authService;

    public PaymentServiceStub(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<PurchaseResult> PurchaseSubscriptionAsync(string planId)
    {
        if (_authService.CurrentUser == null)
            return Task.FromResult(PurchaseResult.Fail(LocalizationService.Instance["AuthNotLoggedIn"]));

        var (durationDays, price) = planId switch
        {
            SubscriptionPlans.Monthly => (31,  SubscriptionPlans.MonthlyPriceRub),
            SubscriptionPlans.Yearly  => (366, SubscriptionPlans.YearlyPriceRub),
            _ => (0, 0m)
        };

        if (durationDays == 0)
            return Task.FromResult(PurchaseResult.Fail(LocalizationService.Instance["PaymentInvalidPlan"]));

        var subscription = new SubscriptionInfo
        {
            PlanId       = planId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(durationDays),
            PriceLabel   = planId == SubscriptionPlans.Monthly
                ? $"{SubscriptionPlans.MonthlyPriceRub:0} ₽/{LocalizationService.Instance["PerMonth"]}"
                : $"{SubscriptionPlans.YearlyPriceRub:0} ₽/{LocalizationService.Instance["PerYear"]}"
        };

        // Записываем в профиль (in-memory) и в AppSettings (персистентно)
        _authService.CurrentUser.Subscription = subscription;

        var settings = AppSettings.Load();
        settings.SubscriptionExpiryDate = subscription.ExpiresAtUtc;
        settings.Save();

        return Task.FromResult(PurchaseResult.Ok(subscription));
    }

    public Task<SubscriptionInfo?> GetSubscriptionStatusAsync()
    {
        // TODO (реальный бэкенд): GET /api/subscription/verify
        return Task.FromResult(_authService.CurrentUser?.Subscription);
    }
}
