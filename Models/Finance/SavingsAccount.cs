using LiteDB;
using System;

namespace FocusFlowFinal.Models.Finance;

public class SavingsAccount
{
    [BsonId]
    public int Id { get; set; }
    public string? UserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    /// <summary>Целевая сумма (0 = не задана).</summary>
    public decimal TargetAmount { get; set; } = 0m;

    /// <summary>Текущий баланс (пересчитывается при каждой транзакции).</summary>
    public decimal CurrentBalance { get; set; } = 0m;

    public DateTime StartDate { get; set; } = DateTime.Today;

    /// <summary>Можно ли снимать средства.</summary>
    public bool CanWithdraw { get; set; } = true;

    public bool IsArchived { get; set; } = false;

    /// <summary>True для копилки с целевой суммой (создаётся кнопкой «+ Копилка»).</summary>
    public bool IsGoal { get; set; } = false;

    /// <summary>Цель достигнута (баланс >= TargetAmount). Уведомление отправляется однократно.</summary>
    public bool IsGoalAchieved { get; set; } = false;

    /// <summary>Прогресс к цели (0–100). -1 если цель не задана.</summary>
    [BsonIgnore]
    public double ProgressPercent =>
        TargetAmount > 0 ? Math.Min(100.0, (double)CurrentBalance / (double)TargetAmount * 100.0) : -1;

    [BsonIgnore]
    public bool HasTarget => TargetAmount > 0;
}
