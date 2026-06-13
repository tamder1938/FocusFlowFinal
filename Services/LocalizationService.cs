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
                ["SwitchToMonth"] = "Переключить на Месяц",
                ["SwitchToYear"] = "Переключить на Год",
                ["GoToToday"] = "Перейти на сегодня",
                ["NewTask"] = "Новая задача",
                ["ExportData"] = "💾 Импорт и экспорт данных",
                ["Backup"] = "Резервное копирование",
                ["BackupSub"] = "Создайте резервную копию всех ваших задач",
                ["ExportBtn"] = "Экспортировать",
                ["ExportSuccess"] = "Данные успешно экспортированы!",
                ["ExportError"] = "Ошибка при экспорте данных: ",
                ["DangerZone"] = "Опасная зона",
                ["ClearSub"] = "Удаление всех данных без восстановления",
                ["ClearBtn"] = "Очистить все данные",
                ["ClearConfirm"] = "Вы уверены? Все данные будут удалены без возможности восстановления.",
                ["Yes"] = "Да",
                ["No"] = "Нет",
                ["ClearSuccess"] = "Все данные удалены.",
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
                ["CustomMode"] = "Пользовательский режим",
                ["Week"] = "Неделя",
                ["HoursShort"] = "ч",
                ["MinutesShort"] = "м",
                // ИСПРАВЛЕНО: полные обозначения для DurationText ("30 мин", "1 ч 15 мин")
                ["DurationMin"] = "мин",
                ["DurationHour"] = "ч",
                ["PerWeek"] = "за неделю",
                ["Deviation"] = "Отклонение",
                ["NoProject"] = "Без проекта",

                // ИСПРАВЛЕНО: новые ключи локализации (фильтры, проекты, готово, выполненные задачи)
                ["AllProjectsLbl"] = "Все проекты",
                ["AllFilter"] = "Все",
                ["ActiveFilter"] = "Активные",
                ["CompletedFilter"] = "Выполненные",
                ["DoneBtn"] = "Готово",
                ["CompletedTasksHeader"] = "Выполненные задачи",
                ["PrevWeekTip"] = "Предыдущая неделя",
                ["NextWeekTip"] = "Следующая неделя",
                ["DeviationLabel"] = "План / Факт (сегодня)",
                ["DeviationSubNoPlan"] = "Нет задач на сегодня",
                ["DeviationSubUnder"] = "Недовыполнение",
                ["DeviationSubOnTrack"] = "В плане",

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
                ["HexColorLabel"] = "HEX код:",
                ["WorkingDaysLbl"] = "Количество рабочих дней:",
                ["OffDaysLbl"] = "Количество выходных дней:",
                ["CycleStartLabel"] = "Начальная дата цикла:",
                ["IntervalLabel"] = "Интервал (каждые N):",
                ["IntervalUnitLbl"] = "Единица измерения:",

                // Выбор дня начала повторения
                ["MonthlyDayLbl"] = "День месяца (1-31):",
                ["YearlyDayLbl"] = "День:",
                ["YearlyMonthLbl"] = "Месяц (1-12):",
                ["RecurrenceStartDayHint"] = "День начала повторения",

                // Локализация типов повторения
                ["RecurrenceLbl"] = "Повторение:",
                ["Recurrence_None"] = "Без повторения",
                ["Recurrence_Daily"] = "Каждый день",
                ["Recurrence_Weekdays"] = "По рабочим дням",
                ["Recurrence_Weekly"] = "По дням недели",
                ["Recurrence_Monthly"] = "Каждый месяц",
                ["Recurrence_Yearly"] = "Каждый год",
                ["Recurrence_Shift"] = "График сменности (2/2)",
                ["Recurrence_Custom"] = "Свой интервал",

                // Локализация единиц интервала
                ["IntervalUnit_Days"] = "Дней",
                ["IntervalUnit_Weeks"] = "Недель",
                ["IntervalUnit_Months"] = "Месяцев",

                // Локализация дней недели в диалоге
                ["Day_Mon"] = "Пн",
                ["Day_Tue"] = "Вт",
                ["Day_Wed"] = "Ср",
                ["Day_Thu"] = "Чт",
                ["Day_Fri"] = "Пт",
                ["Day_Sat"] = "Сб",
                ["Day_Sun"] = "Вс",

                // Метка даты окончания повторений
                ["RepeatUntilLbl"] = "Повторять до",
                // ИСПРАВЛЕНО (4.4): локализованное имя стандартного таймера
                ["PomodoroDefault"] = "Помодоро 25/5",
                // ИСПРАВЛЕНО (4.1): watermark-подсказки для EventDialog
                ["EventNameWatermark"] = "Введите название события",
                ["EventDescriptionWatermark"] = "Добавьте описание (необязательно)",
                ["HexColorWatermark"] = "HEX-код цвета, например #3B82F6",
                // ИСПРАВЛЕНО (4.5): управление шаблонами таймера
                ["DeleteTemplateConfirm"] = "Удалить этот шаблон?",
                ["TemplatesHeader"] = "Шаблоны таймера",

                // ИСПРАВЛЕНО (Часть 2-3): аккаунт, вход, регистрация, подписка
                ["Account"] = "Аккаунт",
                ["LoginTitle"] = "Вход в FocusFlow",
                ["LoginEmailLbl"] = "Email",
                ["LoginPasswordLbl"] = "Пароль",
                ["LoginBtn"] = "Войти",
                ["RegisterLinkBtn"] = "Создать аккаунт",
                ["ContinueWithoutSyncBtn"] = "Продолжить без синхронизации",
                ["RegisterTitle"] = "Регистрация",
                ["RegisterUsernameLbl"] = "Имя пользователя",
                ["RegisterConfirmPasswordLbl"] = "Подтверждение пароля",
                ["RegisterBtn"] = "Зарегистрироваться",
                ["BackToLoginBtn"] = "Назад ко входу",
                ["AuthFillAllFields"] = "Заполните все поля",
                ["AuthEmailTaken"] = "Пользователь с таким email уже существует",
                ["AuthInvalidCredentials"] = "Неверный email или пароль",
                ["AuthNotLoggedIn"] = "Вы не вошли в аккаунт",
                ["AuthWrongPassword"] = "Неверный текущий пароль",
                ["PasswordsDontMatch"] = "Пароли не совпадают",
                ["PaymentInvalidPlan"] = "Неизвестный тарифный план",
                ["PerMonth"] = "мес",
                ["PerYear"] = "год",
                ["SubscriptionRequiredTitle"] = "Синхронизация недоступна",
                ["SubscriptionRequiredText"] = "Для использования синхронизации и управления аккаунтом приобретите подписку",
                ["BuyMonthBtn"] = "Купить месяц",
                ["BuyYearBtn"] = "Купить год",
                ["SubscriptionActiveLbl"] = "Подписка активна до",
                ["DeveloperModeLbl"] = "Режим разработчика — бесплатный доступ",
                ["FreeAccessLbl"] = "Бесплатный доступ предоставлен",
                ["SyncNowBtn"] = "Синхронизировать сейчас",
                ["SyncEnabledLbl"] = "Использовать синхронизацию",
                ["LastSyncLbl"] = "Последняя синхронизация",
                ["NeverLbl"] = "никогда",
                ["LogoutBtn"] = "Выйти из аккаунта",
                ["EditProfileUsername"] = "Имя пользователя",
                ["EditProfileEmail"] = "Email",
                ["ChangePasswordBtn"] = "Изменить пароль",
                ["CurrentPasswordLbl"] = "Текущий пароль",
                ["NewPasswordLbl"] = "Новый пароль",
                ["SyncingNowLbl"] = "Синхронизация...",
                ["SyncSuccessLbl"] = "Синхронизация завершена",
                ["SyncFailedLbl"] = "Не удалось синхронизировать — сервер недоступен",
                ["PostPurchaseTitle"] = "Подписка активирована!",
                ["PostPurchaseText"] = "Создайте аккаунт или войдите в существующий, чтобы начать синхронизацию",
                ["AlreadyHaveAccountBtn"] = "У меня уже есть аккаунт",
                ["CreateAccountBtn"] = "Создать аккаунт",
                ["ChooseAvatarBtn"] = "Выбрать фото",
                ["RepeatUntilHint"] = "Выберите дату окончания",

                // EventDialog хардкодные строки
                ["LoadTemplateHint"] = "Загрузить из шаблона:",
                ["ChooseTemplate"] = "Выберите шаблон...",
                ["EventNameLbl"] = "Название:",
                ["SaveAsTemplateLbl"] = "Сохранить как шаблон",
                ["TemplateNameWatermark"] = "Название шаблона",
                ["TemplateHint"] = "Шаблон появится в менеджере шаблонов",
                ["DeleteBtn"] = "Удалить",
                ["CancelBtn"] = "Отмена",
                ["SaveBtn"] = "Сохранить",
                ["DaysLbl"] = "Дни:",
                ["CycleStartLbl"] = "Начало цикла:",
                ["IntervalLbl"] = "Интервал (каждые N):",

                // Горячие клавиши (описания в Settings)
                ["HotkeyDay"] = "Переключить на День",
                ["HotkeyWeek"] = "Переключить на Неделю",
                ["HotkeyMonth"] = "Переключить на Месяц",
                ["HotkeyYear"] = "Переключить на Год",
                ["HotkeyNewTask"] = "Новая задача",
                ["HotkeyToday"] = "Перейти на сегодня",

                // Уведомления
                ["NotifEventTitle"] = "Напоминание о событии",
                ["NotifTimerTitle"] = "Таймер завершён",
                ["NotifTimerBody"] = "Сессия Помодоро завершена. Сделайте перерыв!",
                ["MarkTaskOnTimerFinish"] = "Автовыполнение задачи",
                ["MarkTaskOnTimerFinishSub"] = "Отмечать задачу выполненной по окончании всех циклов таймера",

                // Для годового календаря
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
                ["SunShort"] = "Вс",

                // Горячие клавиши Info
                ["HotkeyCtrl1"] = "Ctrl + 1",
                ["HotkeyCtrl2"] = "Ctrl + 2",
                ["HotkeyCtrlM"] = "Ctrl + M",
                ["HotkeyCtrlY"] = "Ctrl + Y",
                ["HotkeyCtrlN"] = "Ctrl + N",
                ["HotkeyCtrlT"] = "Ctrl + T",

                // Подзадачи
                ["SubtasksLabel"]    = "Подзадачи",
                ["SubtaskWatermark"] = "Название подзадачи",
                ["AddSubtaskBtn"]    = "+ Добавить подзадачу",

                // Диалог завершения таймера
                ["TimerDoneTitle"]       = "Таймер завершён",
                ["TimerDoneQuestion"]    = "Задача выполнена?",
                ["TimerDoneOptions"]                = "Что сделать с задачей?",
                ["TimerExtendLabel"]               = "Продлить на",
                ["TimerExtendMinSuffix"]            = "мин",
                ["TimerCompletion_ExtendWatermark"] = "Минуты",
                ["TimerExtendError"]               = "Введите целое число ≥ 1",
                ["TimerExtendBtn"]                 = "Продлить таймер",
                ["TimerDeferBtn"]        = "Отложить задачу",
                ["TimerDeleteTaskBtn"]   = "Удалить задачу"
            },
            ["English"] = new Dictionary<string, string>
            {
                // Settings window
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
                ["SwitchToMonth"] = "Switch to Month view",
                ["SwitchToYear"] = "Switch to Year view",
                ["GoToToday"] = "Go to Today",
                ["NewTask"] = "New Task",
                ["ExportData"] = "💾 Data Import & Export",
                ["Backup"] = "Backup data",
                ["BackupSub"] = "Create a secure backup of your DB",
                ["ExportBtn"] = "Export",
                ["ExportSuccess"] = "Data exported successfully!",
                ["ExportError"] = "Export error: ",
                ["DangerZone"] = "Danger Zone",
                ["ClearSub"] = "Permanent data wipe without recovery",
                ["ClearBtn"] = "Clear all data",
                ["ClearConfirm"] = "Are you sure? All data will be permanently deleted.",
                ["Yes"] = "Yes",
                ["No"] = "No",
                ["ClearSuccess"] = "All data has been cleared.",
                ["Close"] = "Close",
                ["Save"] = "Save",

                // Main window
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
                ["CustomMode"] = "Custom Mode",
                ["Week"] = "Week",
                ["HoursShort"] = "h",
                ["MinutesShort"] = "m",
                // FIXED: full units for DurationText ("30 min", "1 h 15 min")
                ["DurationMin"] = "min",
                ["DurationHour"] = "h",
                ["PerWeek"] = "per week",
                ["Deviation"] = "Deviation",
                ["NoProject"] = "No Project",

                // FIXED: new localization keys (filters, projects, done, completed tasks)
                ["AllProjectsLbl"] = "All Projects",
                ["AllFilter"] = "All",
                ["ActiveFilter"] = "Active",
                ["CompletedFilter"] = "Completed",
                ["DoneBtn"] = "Done",
                ["CompletedTasksHeader"] = "Completed Tasks",
                ["PrevWeekTip"] = "Previous week",
                ["NextWeekTip"] = "Next week",
                ["DeviationLabel"] = "Plan / Fact (today)",
                ["DeviationSubNoPlan"] = "No tasks today",
                ["DeviationSubUnder"] = "Under plan",
                ["DeviationSubOnTrack"] = "On track",

                // Pomodoro templates
                ["PomodoroTemplates"] = "Pomodoro Templates",
                ["SaveAsTemplate"] = "Save as template",
                ["TemplateName"] = "Template Name",
                ["WorkTime"] = "Work",
                ["BreakTime"] = "Break",

                // Creation windows
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
                ["HexColorLabel"] = "HEX code:",
                ["WorkingDaysLbl"] = "Working days count:",
                ["OffDaysLbl"] = "Off days count:",
                ["CycleStartLabel"] = "Cycle start date:",
                ["IntervalLabel"] = "Interval (every N):",
                ["IntervalUnitLbl"] = "Interval Unit:",

                // Recurrence start day
                ["MonthlyDayLbl"] = "Day of month (1-31):",
                ["YearlyDayLbl"] = "Day:",
                ["YearlyMonthLbl"] = "Month (1-12):",
                ["RecurrenceStartDayHint"] = "Recurrence start day",

                // Recurrence type localization
                ["RecurrenceLbl"] = "Recurrence:",
                ["Recurrence_None"] = "No recurrence",
                ["Recurrence_Daily"] = "Daily",
                ["Recurrence_Weekdays"] = "Weekdays",
                ["Recurrence_Weekly"] = "Weekly (by day)",
                ["Recurrence_Monthly"] = "Monthly",
                ["Recurrence_Yearly"] = "Yearly",
                ["Recurrence_Shift"] = "Shift schedule (2/2)",
                ["Recurrence_Custom"] = "Custom interval",

                // Interval unit localization
                ["IntervalUnit_Days"] = "Days",
                ["IntervalUnit_Weeks"] = "Weeks",
                ["IntervalUnit_Months"] = "Months",

                // Weekday checkboxes
                ["Day_Mon"] = "Mon",
                ["Day_Tue"] = "Tue",
                ["Day_Wed"] = "Wed",
                ["Day_Thu"] = "Thu",
                ["Day_Fri"] = "Fri",
                ["Day_Sat"] = "Sat",
                ["Day_Sun"] = "Sun",

                // Recurrence end date label
                ["RepeatUntilLbl"] = "Repeat until",
                // FIXED (4.4): localized default timer name
                ["PomodoroDefault"] = "Pomodoro 25/5",
                // FIXED (4.1): watermark hints for EventDialog
                ["EventNameWatermark"] = "Enter event name",
                ["EventDescriptionWatermark"] = "Add description (optional)",
                ["HexColorWatermark"] = "Color hex code, e.g. #3B82F6",
                // FIXED (4.5): timer template management
                ["DeleteTemplateConfirm"] = "Delete this template?",
                ["TemplatesHeader"] = "Timer templates",

                // FIXED (Part 2-3): account, login, registration, subscription
                ["Account"] = "Account",
                ["LoginTitle"] = "Sign in to FocusFlow",
                ["LoginEmailLbl"] = "Email",
                ["LoginPasswordLbl"] = "Password",
                ["LoginBtn"] = "Sign in",
                ["RegisterLinkBtn"] = "Create account",
                ["ContinueWithoutSyncBtn"] = "Continue without sync",
                ["RegisterTitle"] = "Registration",
                ["RegisterUsernameLbl"] = "Username",
                ["RegisterConfirmPasswordLbl"] = "Confirm password",
                ["RegisterBtn"] = "Register",
                ["BackToLoginBtn"] = "Back to sign in",
                ["AuthFillAllFields"] = "Please fill in all fields",
                ["AuthEmailTaken"] = "A user with this email already exists",
                ["AuthInvalidCredentials"] = "Invalid email or password",
                ["AuthNotLoggedIn"] = "You are not signed in",
                ["AuthWrongPassword"] = "Current password is incorrect",
                ["PasswordsDontMatch"] = "Passwords do not match",
                ["PaymentInvalidPlan"] = "Unknown subscription plan",
                ["PerMonth"] = "mo",
                ["PerYear"] = "yr",
                ["SubscriptionRequiredTitle"] = "Sync unavailable",
                ["SubscriptionRequiredText"] = "Purchase a subscription to use sync and manage your account",
                ["BuyMonthBtn"] = "Buy monthly",
                ["BuyYearBtn"] = "Buy yearly",
                ["SubscriptionActiveLbl"] = "Subscription active until",
                ["DeveloperModeLbl"] = "Developer mode — free access",
                ["FreeAccessLbl"] = "Free access granted",
                ["SyncNowBtn"] = "Sync now",
                ["SyncEnabledLbl"] = "Use sync",
                ["LastSyncLbl"] = "Last sync",
                ["NeverLbl"] = "never",
                ["LogoutBtn"] = "Log out",
                ["EditProfileUsername"] = "Username",
                ["EditProfileEmail"] = "Email",
                ["ChangePasswordBtn"] = "Change password",
                ["CurrentPasswordLbl"] = "Current password",
                ["NewPasswordLbl"] = "New password",
                ["SyncingNowLbl"] = "Syncing...",
                ["SyncSuccessLbl"] = "Sync completed",
                ["SyncFailedLbl"] = "Sync failed — server unavailable",
                ["PostPurchaseTitle"] = "Subscription activated!",
                ["PostPurchaseText"] = "Create an account or sign in to an existing one to start syncing",
                ["AlreadyHaveAccountBtn"] = "I already have an account",
                ["CreateAccountBtn"] = "Create account",
                ["ChooseAvatarBtn"] = "Choose photo",
                ["RepeatUntilHint"] = "Select end date",

                // EventDialog hardcoded strings
                ["LoadTemplateHint"] = "Load from template:",
                ["ChooseTemplate"] = "Choose template...",
                ["EventNameLbl"] = "Name:",
                ["SaveAsTemplateLbl"] = "Save as template",
                ["TemplateNameWatermark"] = "Template name",
                ["TemplateHint"] = "Template will appear in template manager",
                ["DeleteBtn"] = "Delete",
                ["CancelBtn"] = "Cancel",
                ["SaveBtn"] = "Save",
                ["DaysLbl"] = "Days:",
                ["CycleStartLbl"] = "Cycle start:",
                ["IntervalLbl"] = "Interval (every N):",

                // Hotkeys descriptions
                ["HotkeyDay"] = "Switch to Day view",
                ["HotkeyWeek"] = "Switch to Week view",
                ["HotkeyMonth"] = "Switch to Month view",
                ["HotkeyYear"] = "Switch to Year view",
                ["HotkeyNewTask"] = "New Task",
                ["HotkeyToday"] = "Go to Today",

                // Notifications
                ["NotifEventTitle"] = "Event Reminder",
                ["NotifTimerTitle"] = "Timer Finished",
                ["NotifTimerBody"] = "Pomodoro session completed. Take a break!",
                ["MarkTaskOnTimerFinish"] = "Auto-complete task",
                ["MarkTaskOnTimerFinishSub"] = "Mark task as completed when all timer cycles finish",

                // Year calendar
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
                ["SunShort"] = "S",

                // Hotkeys info
                ["HotkeyCtrl1"] = "Ctrl + 1",
                ["HotkeyCtrl2"] = "Ctrl + 2",
                ["HotkeyCtrlM"] = "Ctrl + M",
                ["HotkeyCtrlY"] = "Ctrl + Y",
                ["HotkeyCtrlN"] = "Ctrl + N",
                ["HotkeyCtrlT"] = "Ctrl + T",

                // Subtasks
                ["SubtasksLabel"]    = "Subtasks",
                ["SubtaskWatermark"] = "Subtask title",
                ["AddSubtaskBtn"]    = "+ Add subtask",

                // Timer completion dialog
                ["TimerDoneTitle"]       = "Timer finished",
                ["TimerDoneQuestion"]    = "Was the task completed?",
                ["TimerDoneOptions"]                = "What to do with the task?",
                ["TimerExtendLabel"]               = "Extend by",
                ["TimerExtendMinSuffix"]            = "min",
                ["TimerCompletion_ExtendWatermark"] = "Minutes",
                ["TimerExtendError"]               = "Enter a whole number ≥ 1",
                ["TimerExtendBtn"]                 = "Extend timer",
                ["TimerDeferBtn"]        = "Defer task",
                ["TimerDeleteTaskBtn"]   = "Delete task"
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
