using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public class LocalizationService : ObservableObject
{
    private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
    public static LocalizationService Instance => _instance.Value;

    private string _currentLanguage = "Русский";
    private readonly Dictionary<string, Dictionary<string, string>> _translations;

    private LocalizationService()
    {
        _translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["Русский"] = new Dictionary<string, string>
            {
                // Окно настроек
                ["Settings"] = "Настройки",
                ["General"] = "Общие",
                ["Notifications"] = "Уведомления",
                ["Hotkeys"] = "Горячие клавиши",
                ["Data"] = "Данные",
                ["Appearance"] = "🌙 Внешний вид",
                ["LightTheme"] = "Светлая",
                ["DarkTheme"] = "Тёмная",
                ["AutoTheme"] = "Авто",
                ["LangRegion"] = "🌐 Язык и регион",
                ["SysNotif"] = "Системные уведомления",
                ["SysNotifSub"] = "Показывать уведомления в системе",
                ["SoundNotif"] = "Звуковые уведомления",
                ["SoundNotifSub"] = "Воспроизводить звук при окончании таймера",
                ["SwitchToDay"] = "Переключить на День",
                ["SwitchToWeek"] = "Переключить на Неделю",
                ["NewTask"] = "Новая задача",
                ["ExportData"] = "💾 Импорт и экспорт данных",
                ["Backup"] = "Резервное копирование",
                ["BackupSub"] = "Создайте резервную копию всех ваших задач",
                ["ExportBtn"] = "Экспортировать",
                ["DangerZone"] = "Опасная зона",
                ["ClearSub"] = "Удаление всех данных без восстановления",
                ["ClearBtn"] = "Очистить все данные",
                ["Close"] = "Закрыть",
                ["Save"] = "Сохранить",

                // Главное окно
                ["TitleDay"] = "День",
                ["TitleWeek"] = "Неделя",
                ["TitleMonth"] = "Месяц",
                ["TitleYear"] = "Год",
                ["Tasks"] = "Задачи",
                ["Timer"] = "Таймер",
                ["AnalyticsTitle"] = "Аналитика",
                ["MoreBtn"] = "Подробнее",
                ["CustomSettings"] = "Свои настройки",
                ["FilterAll"] = "Все",
                ["FilterHigh"] = "Высокий",
                ["FilterMedium"] = "Средний",
                ["AddEventBtn"] = "+ Событие",
                ["TodayBtn"] = "Сегодня",

                ["AnalyticsSubtitle"] = "Детальный отчет за текущий период",
                ["FocusedTotal"] = "Всего сфокусирован",
                ["TasksCompleted"] = "Задач завершено",
                ["Productivity"] = "Продуктивность",
                ["FocusDistributionWeek"] = "Распределение фокуса по дням недели",
                ["ProjectFocusCategories"] = "Фокус по категориям проектов",
                ["Done"] = "Готово",
                ["LowLevel"] = "Низкий уровень",
                ["MediumLevel"] = "Средний уровень",
                ["HighLevel"] = "Высокий уровень",

                ["PlanVsFactToday"] = "План vs Факт сегодня",
                ["MoreDetails"] = "Подробнее",
                ["Plan"] = "План",
                ["Fact"] = "Факт",
                ["Timer"] = "Таймер",
                ["CustomMode"] = "Пользовательский режим",
                ["Week"] = "Неделя",
                ["HoursShort"] = "ч",
                ["MinutesShort"] = "м",
                ["PerWeek"] = "за неделю",
                ["Deviation"] = "Отклонение",
                ["NoProject"] = "Без проекта",


                // Шаблоны Помодоро
                ["PomodoroTemplates"] = "Шаблоны Помодоро",
                ["SaveAsTemplate"] = "Сохранить как шаблон",
                ["TemplateName"] = "Имя шаблона",
                ["WorkTime"] = "Работа",
                ["BreakTime"] = "Отдых",


                // Окна создания
                ["TaskTitle"] = "Задача",
                ["TaskNameLbl"] = "Название задачи:",
                ["TaskDesc"] = "Описание:",
                ["DueDateLabel"] = "Срок выполнения:",
                ["PriorityLabel"] = "Приоритет:",
                ["TaskNameWatermark"] = "Введите название задачи",
                ["TaskDescWatermark"] = "Добавьте описание",
                ["HasDateOpt"] = "📅 Назначить дату дедлайна",
                ["IsTimeBoundOpt"] = "⏰ Привязать к конкретному времени",
                ["IsDurationOpt"] = "📊 Установить длительность",
                ["DurationLabel"] = "Продолжительность (мин):",
                ["FilterLow"] = "Низкий",
                ["ProjectLabel"] = "📁 Проект:",
                ["EventTitle"] = "Событие",
                ["AllDayLbl"] = "Весь день",
                ["StartTimeLbl"] = "Время начала:",
                ["EndTimeLbl"] = "Время окончания:",
                ["HourLabel"] = "Час:",
                ["MinLabel"] = "Мин:",
                ["ColorLabel"] = "Цвет:",
                ["WorkingDaysLbl"] = "Количество рабочих дней:",
                ["OffDaysLbl"] = "Количество выходных дней:",
                ["CycleStartLabel"] = "Начальная дата цикла:",
                ["IntervalLabel"] = "Интервал (каждые N):",
                ["IntervalUnitLbl"] = "Единица измерения:",

                // ДЛЯ ГОДОВОГО КАЛЕНДАРЯ (НОВЫЕ КЛЮЧИ)
                ["YearWord"] = "год",
                ["January"] = "Январь",
                ["February"] = "Февраль",
                ["March"] = "Март",
                ["April"] = "Апрель",
                ["May"] = "Май",
                ["June"] = "Июнь",
                ["July"] = "Июль",
                ["August"] = "Август",
                ["September"] = "Сентябрь",
                ["October"] = "Октябрь",
                ["November"] = "Ноябрь",
                ["December"] = "Декабрь",
                ["MonShort"] = "Пн",
                ["TueShort"] = "Вт",
                ["WedShort"] = "Ср",
                ["ThuShort"] = "Чт",
                ["FriShort"] = "Пт",
                ["SatShort"] = "Сб",
                ["SunShort"] = "Вс"
            },
            ["English"] = new Dictionary<string, string>
            {
                // Окно настроек
                ["Settings"] = "Settings",
                ["General"] = "General",
                ["Notifications"] = "Notifications",
                ["Hotkeys"] = "Hotkeys",
                ["Data"] = "Data",
                ["Appearance"] = "🌙 Appearance",
                ["LightTheme"] = "Light",
                ["DarkTheme"] = "Dark",
                ["AutoTheme"] = "Auto",
                ["LangRegion"] = "🌐 Language & Region",
                ["SysNotif"] = "System Notifications",
                ["SysNotifSub"] = "Show system push notifications",
                ["SoundNotif"] = "Sound Notifications",
                ["SoundNotifSub"] = "Play audio on timer finish",
                ["SwitchToDay"] = "Switch to Day view",
                ["SwitchToWeek"] = "Switch to Week view",
                ["NewTask"] = "New Task",
                ["ExportData"] = "💾 Data Import & Export",
                ["Backup"] = "Backup data",
                ["BackupSub"] = "Create a secure backup of your DB",
                ["ExportBtn"] = "Export",
                ["DangerZone"] = "Danger Zone",
                ["ClearSub"] = "Permanent data wipe without recovery",
                ["ClearBtn"] = "Clear all data",
                ["Close"] = "Close",
                ["Save"] = "Save",

                // Главное окно
                ["TitleDay"] = "Day",
                ["TitleWeek"] = "Week",
                ["TitleMonth"] = "Month",
                ["TitleYear"] = "Year",
                ["Tasks"] = "Tasks",
                ["Timer"] = "Timer",
                ["AnalyticsTitle"] = "Analytics",
                ["MoreBtn"] = "More Details",
                ["CustomSettings"] = "Custom Mode",
                ["FilterAll"] = "All",
                ["FilterHigh"] = "High",
                ["FilterMedium"] = "Medium",
                ["AddEventBtn"] = "+ Event",
                ["TodayBtn"] = "Today",

                ["AnalyticsSubtitle"] = "Detailed report for current period",
                ["FocusedTotal"] = "Total Focused",
                ["TasksCompleted"] = "Tasks Completed",
                ["Productivity"] = "Productivity",
                ["FocusDistributionWeek"] = "Focus distribution by weekdays",
                ["ProjectFocusCategories"] = "Focus by project categories",
                ["Done"] = "Done",
                ["LowLevel"] = "Low level",
                ["MediumLevel"] = "Medium level",
                ["HighLevel"] = "High level",

                ["PlanVsFactToday"] = "Plan vs Fact today",
                ["MoreDetails"] = "More Details",
                ["Plan"] = "Plan",
                ["Fact"] = "Fact",
                ["Timer"] = "Timer",
                ["CustomMode"] = "Custom Mode",
                ["Week"] = "Week",
                ["HoursShort"] = "h",
                ["MinutesShort"] = "m",
                ["PerWeek"] = "per week",
                ["Deviation"] = "Deviation",
                ["NoProject"] = "No Project",



                // Шаблоны Помодоро
                ["PomodoroTemplates"] = "Pomodoro Templates",
                ["SaveAsTemplate"] = "Save as template",
                ["TemplateName"] = "Template Name",
                ["WorkTime"] = "Work",
                ["BreakTime"] = "Break",

                // Окна создания
                ["TaskTitle"] = "Task Dialog",
                ["TaskNameLbl"] = "Task Title:",
                ["TaskDesc"] = "Description:",
                ["DueDateLabel"] = "Due Date:",
                ["PriorityLabel"] = "Priority:",
                ["TaskNameWatermark"] = "Enter task name",
                ["TaskDescWatermark"] = "Add description",
                ["HasDateOpt"] = "📅 Assign due date",
                ["IsTimeBoundOpt"] = "⏰ Bind to specific time",
                ["IsDurationOpt"] = "📊 Set planned duration",
                ["DurationLabel"] = "Duration (min):",
                ["FilterLow"] = "Low",
                ["ProjectLabel"] = "📁 Project:",
                ["EventTitle"] = "Event Dialog",
                ["AllDayLbl"] = "All Day",
                ["StartTimeLbl"] = "Start Time:",
                ["EndTimeLbl"] = "End Time:",
                ["HourLabel"] = "Hour:",
                ["MinLabel"] = "Min:",
                ["ColorLabel"] = "Color:",
                ["WorkingDaysLbl"] = "Working days count:",
                ["OffDaysLbl"] = "Off days count:",
                ["CycleStartLabel"] = "Cycle start date:",
                ["IntervalLabel"] = "Interval (every N):",
                ["IntervalUnitLbl"] = "Interval Unit:",

                // ДЛЯ ГОДОВОГО КАЛЕНДАРЯ (НОВЫЕ КЛЮЧИ)
                ["YearWord"] = "year",
                ["January"] = "January",
                ["February"] = "February",
                ["March"] = "March",
                ["April"] = "April",
                ["May"] = "May",
                ["June"] = "June",
                ["July"] = "July",
                ["August"] = "August",
                ["September"] = "September",
                ["October"] = "October",
                ["November"] = "November",
                ["December"] = "December",
                ["MonShort"] = "M",
                ["TueShort"] = "T",
                ["WedShort"] = "W",
                ["ThuShort"] = "T",
                ["FriShort"] = "F",
                ["SatShort"] = "S",
                ["SunShort"] = "S"
            }
        };
    }

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_translations.ContainsKey(value))
            {
                _currentLanguage = value;
                OnPropertyChanged(string.Empty);
            }
        }
    }

    public string this[string key]
    {
        get
        {
            if (_translations.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out var word))
                return word;
            return key;
        }
    }
}