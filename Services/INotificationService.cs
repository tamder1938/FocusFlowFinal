using System;

namespace FocusFlowFinal.Services;

public interface INotificationService
{
    /// <summary>Инициализирует менеджер уведомлений. Вызывать после открытия главного окна.</summary>
    void Initialize(Avalonia.Controls.TopLevel topLevel);

    /// <summary>Показать уведомление с заголовком и текстом.</summary>
    void Show(string title, string message, NotificationLevel level = NotificationLevel.Information);

    /// <summary>Запустить фоновый таймер проверки событий.</summary>
    void StartPolling();

    /// <summary>Остановить опрос.</summary>
    void StopPolling();
}

public enum NotificationLevel
{
    Information,
    Success,
    Warning,
    Error
}
