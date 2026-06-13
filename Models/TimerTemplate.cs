using LiteDB;

namespace FocusFlowFinal.Models;

public class TimerTemplate
{
    [BsonId]
    public int Id { get; set; }
    public string Name { get; set; } = "Стандартный";
    public int WorkMinutes { get; set; } = 25;
    public int BreakMinutes { get; set; } = 5;
    public int Cycles { get; set; } = 4;     // Количество циклов

    // ИСПРАВЛЕНО (4.5): встроенный шаблон "Помодоро 25/5" защищён от удаления
    public bool IsBuiltIn { get; set; } = false;
}
