using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using FocusFlowFinal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FocusFlowFinal.Services;

/// <summary>
/// Сервис уведомлений на базе Avalonia WindowNotificationManager.
/// Показывает тосты в углу экрана и опрашивает базу данных на предмет
/// событий, начинающихся в ближайшие 15 минут.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IDatabaseService _db;

    private WindowNotificationManager? _manager;
    private Timer? _pollingTimer;

    // Уже показанные уведомления (key = eventId + дата начала),
    // чтобы не повторять одно событие несколько раз в одну сессию.
    private readonly HashSet<string> _shownKeys = new();

    // Настройки берём при каждой проверке, чтобы учитывать изменения без перезапуска.
    private static AppSettings CurrentSettings => AppSettings.Load();

    public NotificationService(IDatabaseService db)
    {
        _db = db;
    }

    // ---------------------------------------------------------------
    // Инициализация
    // ---------------------------------------------------------------

    /// <inheritdoc/>
    public void Initialize(TopLevel topLevel)
    {
        _manager = new WindowNotificationManager(topLevel)
        {
            Position    = NotificationPosition.BottomRight,
            MaxItems    = 4,
            Margin      = new Avalonia.Thickness(0, 0, 10, 10)
        };
    }

    // ---------------------------------------------------------------
    // Показ уведомления
    // ---------------------------------------------------------------

    /// <inheritdoc/>
    public void Show(
        string title,
        string message,
        NotificationLevel level = NotificationLevel.Information)
    {
        if (_manager == null) return;
        if (!CurrentSettings.SystemNotifications) return;

        var type = level switch
        {
            NotificationLevel.Success  => NotificationType.Success,
            NotificationLevel.Warning  => NotificationType.Warning,
            NotificationLevel.Error    => NotificationType.Error,
            _                          => NotificationType.Information
        };

        // WindowNotificationManager требует вызова из UI-потока
        Dispatcher.UIThread.Post(() =>
        {
            _manager.Show(new Notification(title, message, type, TimeSpan.FromSeconds(6)));
        });
    }

    // ---------------------------------------------------------------
    // Опрос событий
    // ---------------------------------------------------------------

    /// <inheritdoc/>
    public void StartPolling()
    {
        // Первая проверка через 10 сек. после запуска, далее каждую минуту.
        _pollingTimer = new Timer(
            callback: _ => CheckUpcomingEvents(),
            state:    null,
            dueTime:  TimeSpan.FromSeconds(10),
            period:   TimeSpan.FromMinutes(1));
    }

    /// <inheritdoc/>
    public void StopPolling()
    {
        _pollingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _pollingTimer?.Dispose();
        _pollingTimer = null;
    }

    // ---------------------------------------------------------------
    // Уведомление об окончании таймера (вызывается из TimerViewModel)
    // ---------------------------------------------------------------

    /// <summary>
    /// Показывает уведомление об окончании сессии Pomodoro.
    /// Вызывается из <see cref="TimerViewModel"/> при срабатывании таймера.
    /// </summary>
    public void NotifyTimerFinished(string taskTitle)
    {
        var loc = LocalizationService.Instance;
        string title   = loc["NotifTimerTitle"];
        string message = string.IsNullOrWhiteSpace(taskTitle)
            ? loc["NotifTimerBody"]
            : $"\"{taskTitle}\" — {loc["NotifTimerBody"]}";

        Show(title, message, NotificationLevel.Success);

        // Звуковое уведомление (системный звук Windows)
        if (CurrentSettings.SoundNotifications)
            PlaySystemSound();
    }

    // ---------------------------------------------------------------
    // Внутренняя логика проверки событий
    // ---------------------------------------------------------------

    private void CheckUpcomingEvents()
    {
        if (!CurrentSettings.SystemNotifications) return;

        var now        = DateTime.Now;
        var windowEnd  = now.AddMinutes(15);

        IEnumerable<CalendarEvent> events;
        try
        {
            events = _db.GetEventsForPeriod(now.Date, now.Date.AddDays(1));
        }
        catch
        {
            return;
        }

        foreach (var ev in events)
        {
            // Событие начинается в ближайшие 15 минут (но ещё не началось)
            if (ev.Start > now && ev.Start <= windowEnd)
            {
                string key = $"{ev.Id}_{ev.Start:yyyyMMddHHmm}";
                if (_shownKeys.Contains(key)) continue;

                _shownKeys.Add(key);

                var loc     = LocalizationService.Instance;
                string title = loc["NotifEventTitle"];
                int minsLeft = (int)(ev.Start - now).TotalMinutes;
                string body  = minsLeft <= 1
                    ? $"{ev.Title} — {loc["StartsNow"] ?? "начинается сейчас"}"
                    : $"{ev.Title} — через {minsLeft} мин. ({ev.Start:HH:mm})";

                Show(title, body, NotificationLevel.Information);
            }
        }
    }

    // ---------------------------------------------------------------
    // Звуковое уведомление
    // ---------------------------------------------------------------

    /// <summary>
    /// Воспроизводит системный звук через Console.Beep или Windows SystemSounds
    /// (не требует дополнительных пакетов).
    /// </summary>
    private static void PlaySystemSound()
    {
        try
        {
            // Простой системный beep на 880 Гц, 300 мс
            Console.Beep(880, 300);
        }
        catch
        {
            // Игнорируем — на некоторых системах Console.Beep не работает
        }
    }
}
