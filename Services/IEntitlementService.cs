using System;

namespace FocusFlowFinal.Services;

public interface IEntitlementService
{
    /// <summary>true — активна подписка (или Developer / FreeAccess).</summary>
    bool IsPremiumActive { get; }
}
