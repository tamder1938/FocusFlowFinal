using FocusFlowFinal.Models;
using System;

namespace FocusFlowFinal.Services;

public class EntitlementService : IEntitlementService
{
    // Лимиты бесплатного тарифа
    public const int TaskLimit    = 50;
    public const int ProjectLimit = 3;
    public const int EventLimit   = 50;

    private readonly IAuthService _auth;

    public EntitlementService(IAuthService auth)
    {
        _auth = auth;
    }

    public bool IsPremiumActive
    {
        get
        {
            var user = _auth.CurrentUser;
            if (user?.IsDeveloper == true || user?.HasFreeAccess == true) return true;
            if (user?.Subscription?.IsActive == true) return true;
            // Fallback: подписка куплена, но сессия не восстановлена
            var storedExpiry = AppSettings.Load().SubscriptionExpiryDate;
            return storedExpiry.HasValue && storedExpiry.Value > DateTime.UtcNow;
        }
    }
}
