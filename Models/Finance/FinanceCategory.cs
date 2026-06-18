using LiteDB;

namespace FocusFlowFinal.Models.Finance;

public class FinanceCategory
{
    [BsonId]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>"income" | "expense" | "subscription" | "loan"</summary>
    public string Type { get; set; } = string.Empty;
}
