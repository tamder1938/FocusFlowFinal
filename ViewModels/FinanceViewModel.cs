using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlowFinal.Models.Finance;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlowFinal.ViewModels;

public partial class FinanceViewModel : ObservableObject
{
    private readonly IDatabaseService      _db;
    private readonly INotificationService  _notify;
    public LocalizationService Loc => LocalizationService.Instance;

    // ── Вкладки ──────────────────────────────────────────────────────
    [ObservableProperty] private bool _isIncomeTab    = true;
    [ObservableProperty] private bool _isExpenseTab;
    [ObservableProperty] private bool _isSubTab;
    [ObservableProperty] private bool _isLoanTab;
    [ObservableProperty] private bool _isSavingsTab;
    [ObservableProperty] private bool _isSummaryTab;

    // ── Ошибки ───────────────────────────────────────────────────────
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    // ── Доходы ──────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<FinanceIncome> _incomes = new();
    private FinanceIncome? _editingIncome;
    [ObservableProperty] private bool _isAddingIncome;
    [ObservableProperty] private string _newIncomeCategory = string.Empty;
    [ObservableProperty] private decimal? _newIncomeAmount;
    [ObservableProperty] private string _newIncomeNote = string.Empty;
    [ObservableProperty] private DateTimeOffset _newIncomeDate = DateTimeOffset.Now;

    // ── Расходы ──────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<FinanceExpense> _expenses = new();
    private FinanceExpense? _editingExpense;
    [ObservableProperty] private bool _isAddingExpense;
    [ObservableProperty] private string _newExpenseCategory = string.Empty;
    [ObservableProperty] private decimal? _newExpenseAmount;
    [ObservableProperty] private string _newExpenseNote = string.Empty;
    [ObservableProperty] private DateTimeOffset _newExpenseDate = DateTimeOffset.Now;

    // ── Подписки ─────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<FinanceSubscriptionItem> _subscriptions = new();
    private FinanceSubscriptionItem? _editingSub;
    [ObservableProperty] private bool _isAddingSub;
    [ObservableProperty] private string _newSubName = string.Empty;
    [ObservableProperty] private decimal? _newSubAmount;
    [ObservableProperty] private string _newSubCategory = string.Empty;
    [ObservableProperty] private int _newSubCycleIndex;
    [ObservableProperty] private DateTimeOffset _newSubNextDate = DateTimeOffset.Now.AddMonths(1);
    [ObservableProperty] private int _newSubNotifDays;

    // ── Кредиты ──────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<FinanceLoan> _loans = new();
    private FinanceLoan? _editingLoan;
    [ObservableProperty] private bool _isAddingLoan;
    [ObservableProperty] private string _newLoanName = string.Empty;
    [ObservableProperty] private decimal? _newLoanTotal;
    [ObservableProperty] private decimal? _newLoanDown;
    [ObservableProperty] private decimal? _newLoanRate;
    [ObservableProperty] private int _newLoanTermMonths = 12;
    [ObservableProperty] private int _newLoanTermYears;
    [ObservableProperty] private decimal? _newLoanMonthly;
    [ObservableProperty] private DateTimeOffset _newLoanStartDate = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset? _newLoanNextPayment;
    [ObservableProperty] private int _newLoanNotifDays;

    // ── Досрочные погашения ───────────────────────────────────────────
    [ObservableProperty] private FinanceLoan? _selectedLoan;
    [ObservableProperty] private ObservableCollection<LoanEarlyRepayment> _earlyRepayments = new();
    [ObservableProperty] private bool _isAddingRepayment;
    [ObservableProperty] private decimal? _newRepaymentAmount;
    [ObservableProperty] private string _newRepaymentNote = string.Empty;
    [ObservableProperty] private DateTimeOffset _newRepaymentDate = DateTimeOffset.Now;

    // ── Категории (пользовательские) ──────────────────────────────────
    [ObservableProperty] private ObservableCollection<string> _incomeCategoryList = new();
    [ObservableProperty] private ObservableCollection<string> _expenseCategoryList = new();
    [ObservableProperty] private ObservableCollection<string> _subCategoryList = new();
    [ObservableProperty] private ObservableCollection<string> _loanCategoryList = new();
    [ObservableProperty] private bool _isAddingCategory;
    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private string _pendingCategoryType = string.Empty;
    // Удаление категории с подтверждением
    [ObservableProperty] private bool _isConfirmingCategoryDelete;
    [ObservableProperty] private string _deleteCategoryMessage = string.Empty;
    private string _pendingDeleteCategoryName = string.Empty;
    private string _pendingDeleteCategoryType = string.Empty;

    // ── Состояния "список пуст" ──────────────────────────────────────
    public bool IsIncomesEmpty  => Incomes.Count == 0 && !IsAddingIncome;
    public bool IsExpensesEmpty => Expenses.Count == 0 && !IsAddingExpense;
    public bool IsSubsEmpty     => Subscriptions.Count == 0 && !IsAddingSub;
    public bool IsLoansEmpty    => Loans.Count == 0 && !IsAddingLoan;

    partial void OnIsAddingIncomeChanged(bool v)  => OnPropertyChanged(nameof(IsIncomesEmpty));
    partial void OnIsAddingExpenseChanged(bool v) => OnPropertyChanged(nameof(IsExpensesEmpty));
    partial void OnIsAddingSubChanged(bool v)     => OnPropertyChanged(nameof(IsSubsEmpty));
    partial void OnIsAddingLoanChanged(bool v)    => OnPropertyChanged(nameof(IsLoansEmpty));

    // ── Сводка ───────────────────────────────────────────────────────
    [ObservableProperty] private decimal _monthIncome;
    [ObservableProperty] private decimal _monthExpense;
    [ObservableProperty] private decimal _monthBalance;
    [ObservableProperty] private decimal _yearIncome;
    [ObservableProperty] private decimal _yearExpense;
    [ObservableProperty] private decimal _yearBalance;

    // Сводка — детальная статистика по секциям
    [ObservableProperty] private int     _summaryIncomeCount;
    [ObservableProperty] private int     _summaryExpenseCount;
    [ObservableProperty] private int     _summarySubCount;
    [ObservableProperty] private decimal _summarySubMonthly;
    [ObservableProperty] private int     _summaryLoanCount;
    [ObservableProperty] private decimal _summaryLoanDebt;
    [ObservableProperty] private decimal _summaryLoanMonthly;

    // Счета/Копилки в сводке
    [ObservableProperty] private decimal _savingsTotalBalance;
    [ObservableProperty] private int     _savingsCount;
    [ObservableProperty] private ObservableCollection<SavingsAccount> _savingsSummaryList = new();

    public string[] CycleItems => new[] { Loc["Finance_Monthly"], Loc["Finance_Yearly"] };

    public FinanceViewModel(IDatabaseService db, INotificationService notify)
    {
        _db     = db;
        _notify = notify;
        LoadAll();
    }

    private void LoadAll()
    {
        try
        {
            Incomes.Clear();
            foreach (var x in _db.GetAllIncomes()) Incomes.Add(x);

            Expenses.Clear();
            foreach (var x in _db.GetAllExpenses()) Expenses.Add(x);

            Subscriptions.Clear();
            foreach (var x in _db.GetAllFinanceSubscriptions()) Subscriptions.Add(x);

            Loans.Clear();
            foreach (var x in _db.GetAllLoans()) Loans.Add(x);

            LoadCategories();
            RefreshSummary();
            HasError = false;
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(IsIncomesEmpty));
            OnPropertyChanged(nameof(IsExpensesEmpty));
            OnPropertyChanged(nameof(IsSubsEmpty));
            OnPropertyChanged(nameof(IsLoansEmpty));
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
        }
    }

    private void LoadCategories()
    {
        IncomeCategoryList  = new ObservableCollection<string>(
            _db.GetCategoriesByType("income").Select(c => c.Name));
        ExpenseCategoryList = new ObservableCollection<string>(
            _db.GetCategoriesByType("expense").Select(c => c.Name));
        SubCategoryList     = new ObservableCollection<string>(
            _db.GetCategoriesByType("subscription").Select(c => c.Name));
        LoanCategoryList    = new ObservableCollection<string>(
            _db.GetCategoriesByType("loan").Select(c => c.Name));

        // Сбросить выбранную категорию, если она больше не существует
        if (!IncomeCategoryList.Contains(NewIncomeCategory))   NewIncomeCategory  = IncomeCategoryList.FirstOrDefault() ?? string.Empty;
        if (!ExpenseCategoryList.Contains(NewExpenseCategory)) NewExpenseCategory = ExpenseCategoryList.FirstOrDefault() ?? string.Empty;
        if (!SubCategoryList.Contains(NewSubCategory))         NewSubCategory     = SubCategoryList.FirstOrDefault() ?? string.Empty;
    }

    private void RefreshSummary()
    {
        var now        = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var yearStart  = new DateTime(now.Year, 1, 1);

        MonthIncome  = Incomes.Where(x => x.Date >= monthStart).Sum(x => x.Amount);
        MonthExpense = Expenses.Where(x => x.Date >= monthStart).Sum(x => x.Amount);
        MonthBalance = MonthIncome - MonthExpense;

        YearIncome   = Incomes.Where(x => x.Date >= yearStart).Sum(x => x.Amount);
        YearExpense  = Expenses.Where(x => x.Date >= yearStart).Sum(x => x.Amount);
        YearBalance  = YearIncome - YearExpense;

        // Детальная статистика по секциям
        SummaryIncomeCount  = Incomes.Count(x => x.Date >= monthStart);
        SummaryExpenseCount = Expenses.Count(x => x.Date >= monthStart);

        SummarySubCount   = Subscriptions.Count;
        SummarySubMonthly = Subscriptions.Sum(s =>
            s.BillingCycle == FinanceBillingCycle.Monthly ? s.Amount : Math.Round(s.Amount / 12m, 2));

        SummaryLoanCount   = Loans.Count;
        SummaryLoanDebt    = Loans.Sum(l => l.RemainingBalance);
        SummaryLoanMonthly = Loans.Sum(l => l.MonthlyPayment);

        // Счета/Копилки
        var activeAccounts = _db.GetAllSavingsAccounts().Where(a => !a.IsArchived).ToList();
        SavingsCount        = activeAccounts.Count;
        SavingsTotalBalance = activeAccounts.Sum(a => a.CurrentBalance);
        SavingsSummaryList.Clear();
        foreach (var a in activeAccounts)
            SavingsSummaryList.Add(a);
    }

    // ── Переключение вкладок ─────────────────────────────────────────
    [RelayCommand]
    private void SelectTab(string tab)
    {
        IsConfirmingCategoryDelete = false;
        IsIncomeTab   = tab == "Income";
        IsExpenseTab  = tab == "Expense";
        IsSubTab      = tab == "Sub";
        IsLoanTab     = tab == "Loan";
        IsSavingsTab  = tab == "Savings";
        IsSummaryTab  = tab == "Summary";
        if (IsSummaryTab)  RefreshSummary();
        if (IsSavingsTab)  LoadSavings();
    }

    // ── Категории: добавление ────────────────────────────────────────
    [RelayCommand]
    private void BeginAddCategory(string type)
    {
        PendingCategoryType = type;
        NewCategoryName     = string.Empty;
        IsAddingCategory    = true;
    }

    [RelayCommand]
    private void SaveCategory()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName)) { IsAddingCategory = false; return; }
        var cat = new FinanceCategory { Name = NewCategoryName.Trim(), Type = PendingCategoryType };
        _db.UpsertCategory(cat);
        IsAddingCategory = false;
        LoadCategories();
    }

    [RelayCommand]
    private void CancelCategory() => IsAddingCategory = false;

    // ── Категории: удаление ──────────────────────────────────────────
    [RelayCommand]
    private void BeginDeleteIncomeCategory()
    {
        if (string.IsNullOrEmpty(NewIncomeCategory)) return;
        _pendingDeleteCategoryName = NewIncomeCategory;
        _pendingDeleteCategoryType = "income";
        DeleteCategoryMessage      = string.Format(Loc["Categories_DeleteConfirm"], NewIncomeCategory);
        IsConfirmingCategoryDelete = true;
    }

    [RelayCommand]
    private void BeginDeleteExpenseCategory()
    {
        if (string.IsNullOrEmpty(NewExpenseCategory)) return;
        _pendingDeleteCategoryName = NewExpenseCategory;
        _pendingDeleteCategoryType = "expense";
        DeleteCategoryMessage      = string.Format(Loc["Categories_DeleteConfirm"], NewExpenseCategory);
        IsConfirmingCategoryDelete = true;
    }

    [RelayCommand]
    private void BeginDeleteSubCategory()
    {
        if (string.IsNullOrEmpty(NewSubCategory)) return;
        _pendingDeleteCategoryName = NewSubCategory;
        _pendingDeleteCategoryType = "subscription";
        DeleteCategoryMessage      = string.Format(Loc["Categories_DeleteConfirm"], NewSubCategory);
        IsConfirmingCategoryDelete = true;
    }

    [RelayCommand]
    private void ConfirmDeleteCategory()
    {
        if (string.IsNullOrEmpty(_pendingDeleteCategoryName)) return;
        try
        {
            switch (_pendingDeleteCategoryType)
            {
                case "income":
                    foreach (var i in _db.GetAllIncomes().Where(i => i.Category == _pendingDeleteCategoryName).ToList())
                    { i.Category = string.Empty; _db.UpsertIncome(i); }
                    break;
                case "expense":
                    foreach (var e in _db.GetAllExpenses().Where(e => e.Category == _pendingDeleteCategoryName).ToList())
                    { e.Category = string.Empty; _db.UpsertExpense(e); }
                    break;
                case "subscription":
                    foreach (var s in _db.GetAllFinanceSubscriptions().Where(s => s.Category == _pendingDeleteCategoryName).ToList())
                    { s.Category = string.Empty; _db.UpsertFinanceSubscription(s); }
                    break;
            }
            var cat = _db.GetCategoriesByType(_pendingDeleteCategoryType)
                         .FirstOrDefault(c => c.Name == _pendingDeleteCategoryName);
            if (cat != null) _db.DeleteCategory(cat.Id);
            IsConfirmingCategoryDelete = false;
            LoadAll();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка удаления: {ex.Message}";
            HasError     = true;
        }
    }

    [RelayCommand]
    private void CancelConfirmDeleteCategory()
    {
        IsConfirmingCategoryDelete = false;
        _pendingDeleteCategoryName = string.Empty;
        _pendingDeleteCategoryType = string.Empty;
    }

    // ── Доходы CRUD ──────────────────────────────────────────────────
    [RelayCommand]
    private void BeginAddIncome()
    {
        _editingIncome    = null;
        NewIncomeDate     = DateTimeOffset.Now;
        NewIncomeCategory = IncomeCategoryList.FirstOrDefault() ?? string.Empty;
        NewIncomeAmount   = null;
        NewIncomeNote     = string.Empty;
        IsAddingIncome    = true;
    }

    [RelayCommand]
    private void EditIncome(FinanceIncome? item)
    {
        if (item == null) return;
        _editingIncome    = item;
        NewIncomeDate     = new DateTimeOffset(item.Date);
        NewIncomeCategory = item.Category;
        NewIncomeAmount   = item.Amount;
        NewIncomeNote     = item.Note;
        IsAddingIncome    = true;
    }

    [RelayCommand]
    private void SaveIncome()
    {
        decimal amount = NewIncomeAmount ?? 0m;
        if (amount <= 0)
        {
            ErrorMessage = Loc["Finance_AmountRequired"];
            HasError     = true;
            return;
        }
        try
        {
            var item = _editingIncome ?? new FinanceIncome();
            item.Date     = NewIncomeDate.Date;
            item.Category = NewIncomeCategory;
            item.Amount   = amount;
            item.Note     = NewIncomeNote;
            _db.UpsertIncome(item);
            IsAddingIncome = false;
            HasError       = false;
            ErrorMessage   = string.Empty;
            Incomes.Clear();
            foreach (var x in _db.GetAllIncomes()) Incomes.Add(x);
            OnPropertyChanged(nameof(IsIncomesEmpty));
            RefreshSummary();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
            HasError     = true;
        }
    }

    [RelayCommand]
    private void CancelIncome() { IsAddingIncome = false; HasError = false; }

    [RelayCommand]
    private void DeleteIncome(FinanceIncome? item)
    {
        if (item == null) return;
        _db.DeleteIncome(item.Id);
        Incomes.Remove(item);
        OnPropertyChanged(nameof(IsIncomesEmpty));
        RefreshSummary();
    }

    // ── Расходы CRUD ─────────────────────────────────────────────────
    [RelayCommand]
    private void BeginAddExpense()
    {
        _editingExpense    = null;
        NewExpenseDate     = DateTimeOffset.Now;
        NewExpenseCategory = ExpenseCategoryList.FirstOrDefault() ?? string.Empty;
        NewExpenseAmount   = null;
        NewExpenseNote     = string.Empty;
        IsAddingExpense    = true;
    }

    [RelayCommand]
    private void EditExpense(FinanceExpense? item)
    {
        if (item == null) return;
        _editingExpense    = item;
        NewExpenseDate     = new DateTimeOffset(item.Date);
        NewExpenseCategory = item.Category;
        NewExpenseAmount   = item.Amount;
        NewExpenseNote     = item.Note;
        IsAddingExpense    = true;
    }

    [RelayCommand]
    private void SaveExpense()
    {
        decimal amount = NewExpenseAmount ?? 0m;
        if (amount <= 0)
        {
            ErrorMessage = Loc["Finance_AmountRequired"];
            HasError     = true;
            return;
        }
        try
        {
            var item = _editingExpense ?? new FinanceExpense();
            item.Date     = NewExpenseDate.Date;
            item.Category = NewExpenseCategory;
            item.Amount   = amount;
            item.Note     = NewExpenseNote;
            _db.UpsertExpense(item);
            IsAddingExpense = false;
            HasError        = false;
            ErrorMessage    = string.Empty;
            Expenses.Clear();
            foreach (var x in _db.GetAllExpenses()) Expenses.Add(x);
            OnPropertyChanged(nameof(IsExpensesEmpty));
            RefreshSummary();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
            HasError     = true;
        }
    }

    [RelayCommand]
    private void CancelExpense() { IsAddingExpense = false; HasError = false; }

    [RelayCommand]
    private void DeleteExpense(FinanceExpense? item)
    {
        if (item == null) return;
        _db.DeleteExpense(item.Id);
        Expenses.Remove(item);
        OnPropertyChanged(nameof(IsExpensesEmpty));
        RefreshSummary();
    }

    // ── Подписки CRUD ────────────────────────────────────────────────
    [RelayCommand]
    private void BeginAddSub()
    {
        _editingSub      = null;
        NewSubName       = string.Empty;
        NewSubAmount     = null;
        NewSubCategory   = SubCategoryList.FirstOrDefault() ?? string.Empty;
        NewSubCycleIndex = 0;
        NewSubNextDate   = DateTimeOffset.Now.AddMonths(1);
        NewSubNotifDays  = 0;
        IsAddingSub      = true;
    }

    [RelayCommand]
    private void EditSub(FinanceSubscriptionItem? item)
    {
        if (item == null) return;
        _editingSub      = item;
        NewSubName       = item.Name;
        NewSubAmount     = item.Amount;
        NewSubCategory   = item.Category;
        NewSubCycleIndex = (int)item.BillingCycle;
        NewSubNextDate   = new DateTimeOffset(item.NextBillingDate);
        NewSubNotifDays  = item.NotificationOffsetMinutes / (60 * 24);
        IsAddingSub      = true;
    }

    [RelayCommand]
    private void SaveSub()
    {
        if (string.IsNullOrWhiteSpace(NewSubName)) return;
        decimal amount = NewSubAmount ?? 0m;
        try
        {
            var item = _editingSub ?? new FinanceSubscriptionItem();
            item.Name            = NewSubName;
            item.Amount          = amount;
            item.Category        = NewSubCategory;
            item.BillingCycle    = (FinanceBillingCycle)NewSubCycleIndex;
            item.NextBillingDate = NewSubNextDate.Date;
            item.NotificationOffsetMinutes = NewSubNotifDays * 60 * 24;
            _db.UpsertFinanceSubscription(item);
            IsAddingSub = false;
            Subscriptions.Clear();
            foreach (var x in _db.GetAllFinanceSubscriptions()) Subscriptions.Add(x);
            OnPropertyChanged(nameof(IsSubsEmpty));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
            HasError     = true;
        }
    }

    [RelayCommand]
    private void CancelSub() => IsAddingSub = false;

    [RelayCommand]
    private void DeleteSub(FinanceSubscriptionItem? item)
    {
        if (item == null) return;
        _db.DeleteFinanceSubscription(item.Id);
        Subscriptions.Remove(item);
        OnPropertyChanged(nameof(IsSubsEmpty));
    }

    [RelayCommand]
    private void ToggleSubPaid(FinanceSubscriptionItem? item)
    {
        if (item == null) return;
        item.IsPaid = !item.IsPaid;
        _db.UpsertFinanceSubscription(item);
        int idx = Subscriptions.IndexOf(item);
        if (idx >= 0) { Subscriptions.RemoveAt(idx); Subscriptions.Insert(idx, item); }
    }

    // ── Кредиты CRUD ─────────────────────────────────────────────────
    [RelayCommand]
    private void BeginAddLoan()
    {
        _editingLoan       = null;
        NewLoanName        = string.Empty;
        NewLoanTotal       = null;
        NewLoanDown        = null;
        NewLoanRate        = null;
        NewLoanTermMonths  = 12;
        NewLoanTermYears   = 0;
        NewLoanMonthly     = null;
        NewLoanStartDate   = DateTimeOffset.Now;
        NewLoanNextPayment = DateTimeOffset.Now.AddMonths(1);
        NewLoanNotifDays   = 0;
        IsAddingLoan       = true;
    }

    [RelayCommand]
    private void EditLoan(FinanceLoan? item)
    {
        if (item == null) return;
        _editingLoan       = item;
        NewLoanName        = item.Name;
        NewLoanTotal       = item.TotalAmount;
        NewLoanDown        = item.DownPayment;
        NewLoanRate        = item.InterestRate;
        NewLoanTermMonths  = item.TermMonths;
        NewLoanTermYears   = item.TermYears;
        NewLoanMonthly     = item.MonthlyPayment;
        NewLoanStartDate   = new DateTimeOffset(item.StartDate);
        NewLoanNextPayment = item.NextPaymentDate.HasValue ? new DateTimeOffset(item.NextPaymentDate.Value) : null;
        NewLoanNotifDays   = item.NotificationOffsetMinutes / (60 * 24);
        IsAddingLoan       = true;
    }

    [RelayCommand]
    private void CalcLoanPayment()
    {
        var tmp = new FinanceLoan
        {
            TotalAmount  = NewLoanTotal ?? 0m,
            DownPayment  = NewLoanDown  ?? 0m,
            InterestRate = NewLoanRate  ?? 0m,
            TermMonths   = NewLoanTermMonths > 0 ? NewLoanTermMonths : NewLoanTermYears * 12
        };
        tmp.RecalculateMonthlyPayment();
        NewLoanMonthly = Math.Round(tmp.MonthlyPayment, 2);
    }

    [RelayCommand]
    private void SaveLoan()
    {
        decimal total = NewLoanTotal ?? 0m;
        if (string.IsNullOrWhiteSpace(NewLoanName) || total <= 0) return;
        try
        {
            var item = _editingLoan ?? new FinanceLoan();
            item.Name             = NewLoanName;
            item.TotalAmount      = total;
            item.DownPayment      = NewLoanDown ?? 0m;
            item.InterestRate     = NewLoanRate ?? 0m;
            item.TermMonths       = NewLoanTermMonths;
            item.TermYears        = NewLoanTermYears;
            item.MonthlyPayment   = NewLoanMonthly ?? 0m;
            item.RemainingBalance = item.RemainingBalance > 0 ? item.RemainingBalance : (total - (NewLoanDown ?? 0m));
            item.StartDate        = NewLoanStartDate.Date;
            item.NextPaymentDate  = NewLoanNextPayment?.Date;
            item.NotificationOffsetMinutes = NewLoanNotifDays * 60 * 24;
            _db.UpsertLoan(item);
            IsAddingLoan = false;
            Loans.Clear();
            foreach (var x in _db.GetAllLoans()) Loans.Add(x);
            OnPropertyChanged(nameof(IsLoansEmpty));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
            HasError     = true;
        }
    }

    [RelayCommand]
    private void CancelLoan() => IsAddingLoan = false;

    // Флаг защиты от рекурсии при синхронизации TermMonths ↔ TermYears
    private bool _updatingTerms;

    partial void OnNewLoanStartDateChanged(DateTimeOffset value) => AutoUpdateLoanDates();

    partial void OnNewLoanTermMonthsChanged(int value)
    {
        if (_updatingTerms) return;
        _updatingTerms = true;
        NewLoanTermYears = value / 12;
        _updatingTerms = false;
        AutoUpdateLoanDates();
    }

    partial void OnNewLoanTermYearsChanged(int value)
    {
        if (_updatingTerms) return;
        _updatingTerms = true;
        NewLoanTermMonths = value * 12;
        _updatingTerms = false;
        AutoUpdateLoanDates();
    }

    private void AutoUpdateLoanDates()
    {
        int months = NewLoanTermMonths > 0 ? NewLoanTermMonths : (NewLoanTermYears * 12);
        if (months > 0)
            NewLoanNextPayment = NewLoanStartDate.AddMonths(1);
        OnPropertyChanged(nameof(NewLoanEndDateDisplay));
    }

    public string NewLoanEndDateDisplay
    {
        get
        {
            int months = NewLoanTermMonths > 0 ? NewLoanTermMonths : (NewLoanTermYears * 12);
            if (months <= 0) return "—";
            return NewLoanStartDate.AddMonths(months).ToString("MM.yyyy");
        }
    }

    [RelayCommand]
    private void DeleteLoan(FinanceLoan? item)
    {
        if (item == null) return;
        _db.DeleteLoan(item.Id);
        Loans.Remove(item);
        OnPropertyChanged(nameof(IsLoansEmpty));
        if (SelectedLoan?.Id == item.Id) SelectedLoan = null;
    }

    // ── Досрочные погашения ───────────────────────────────────────────
    [RelayCommand]
    private void SelectLoan(FinanceLoan? item)
    {
        SelectedLoan = item;
        if (item == null) return;
        EarlyRepayments.Clear();
        foreach (var r in _db.GetEarlyRepayments(item.Id)) EarlyRepayments.Add(r);
    }

    [RelayCommand]
    private void BeginAddRepayment()
    {
        NewRepaymentAmount = null;
        NewRepaymentNote   = string.Empty;
        NewRepaymentDate   = DateTimeOffset.Now;
        IsAddingRepayment  = true;
    }

    [RelayCommand]
    private void SaveRepayment()
    {
        decimal amount = NewRepaymentAmount ?? 0m;
        if (amount <= 0 || SelectedLoan == null) return;
        try
        {
            var rep = new LoanEarlyRepayment
            {
                LoanId = SelectedLoan.Id,
                Date   = NewRepaymentDate.Date,
                Amount = amount,
                Note   = NewRepaymentNote
            };
            _db.UpsertEarlyRepayment(rep);

            // Уменьшаем остаток
            SelectedLoan.RemainingBalance = Math.Max(0, SelectedLoan.RemainingBalance - amount);
            SelectedLoan.RecalculateMonthlyPayment();
            _db.UpsertLoan(SelectedLoan);

            IsAddingRepayment = false;
            SelectLoan(SelectedLoan);
            Loans.Clear();
            foreach (var x in _db.GetAllLoans()) Loans.Add(x);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
            HasError     = true;
        }
    }

    [RelayCommand]
    private void CancelRepayment() => IsAddingRepayment = false;

    [RelayCommand]
    private void DeleteRepayment(LoanEarlyRepayment? item)
    {
        if (item == null || SelectedLoan == null) return;
        _db.DeleteEarlyRepayment(item.Id);
        EarlyRepayments.Remove(item);
    }

    // ── Копилки / Сберегательные счета ──────────────────────────────
    [ObservableProperty] private ObservableCollection<SavingsAccount> _savingsAccounts = new();
    public bool IsSavingsEmpty => SavingsAccounts.Count == 0 && !IsAddingSavingsAccount;
    [ObservableProperty] private SavingsAccount? _selectedSavingsAccount;
    [ObservableProperty] private ObservableCollection<SavingsTransaction> _savingsTransactions = new();

    // Форма создания/редактирования счёта
    [ObservableProperty] private bool _isAddingSavingsAccount;
    partial void OnIsAddingSavingsAccountChanged(bool value) => OnPropertyChanged(nameof(IsSavingsEmpty));
    [ObservableProperty] private bool _isCreatingGoal;
    partial void OnIsCreatingGoalChanged(bool v) => OnPropertyChanged(nameof(SavingsFormTitle));
    public string SavingsFormTitle => IsCreatingGoal ? Loc["Savings_NewGoal"] : Loc["Savings_NewAccount"];
    private SavingsAccount? _editingSavings;
    [ObservableProperty] private string _newSavingsName = string.Empty;
    [ObservableProperty] private decimal? _newSavingsBalance;
    [ObservableProperty] private decimal? _newSavingsTargetAmount;
    [ObservableProperty] private bool _newSavingsCanWithdraw = true;
    [ObservableProperty] private DateTimeOffset _newSavingsStartDate = DateTimeOffset.Now;
    [ObservableProperty] private string _newSavingsNotes = string.Empty;

    // Форма добавления транзакции
    [ObservableProperty] private bool _isAddingSavingsTx;
    [ObservableProperty] private string _pendingSavingsTxType = "deposit"; // "deposit" | "withdrawal"
    [ObservableProperty] private decimal? _newSavingsTxAmount;
    [ObservableProperty] private string _newSavingsTxNote = string.Empty;
    [ObservableProperty] private DateTimeOffset _newSavingsTxDate = DateTimeOffset.Now;

    private void LoadSavings()
    {
        SavingsAccounts.Clear();
        foreach (var a in _db.GetAllSavingsAccounts().Where(a => !a.IsArchived))
            SavingsAccounts.Add(a);
        OnPropertyChanged(nameof(IsSavingsEmpty));
        if (SelectedSavingsAccount != null)
            LoadSavingsTx(SelectedSavingsAccount.Id);
    }


    private void LoadSavingsTx(int accountId)
    {
        SavingsTransactions.Clear();
        foreach (var t in _db.GetSavingsTransactions(accountId))
            SavingsTransactions.Add(t);
    }

    [RelayCommand]
    private void SelectSavingsAccount(SavingsAccount? account)
    {
        // Toggle: повторный клик на тот же счёт закрывает историю
        if (account != null && SelectedSavingsAccount?.Id == account.Id)
        {
            SelectedSavingsAccount = null;
            SavingsTransactions.Clear();
            return;
        }
        SelectedSavingsAccount = account;
        if (account != null) LoadSavingsTx(account.Id);
        else SavingsTransactions.Clear();
    }

    private void ResetSavingsForm()
    {
        _editingSavings         = null;
        NewSavingsName          = string.Empty;
        NewSavingsBalance       = null;
        NewSavingsTargetAmount  = null;
        NewSavingsCanWithdraw   = true;
        NewSavingsStartDate     = DateTimeOffset.Now;
        NewSavingsNotes         = string.Empty;
    }

    [RelayCommand]
    private void BeginAddSavingsAccount()
    {
        ResetSavingsForm();
        IsCreatingGoal          = false;
        IsAddingSavingsAccount  = true;
    }

    [RelayCommand]
    private void BeginAddSavingsGoal()
    {
        ResetSavingsForm();
        IsCreatingGoal          = true;
        IsAddingSavingsAccount  = true;
    }

    [RelayCommand]
    private void EditSavingsAccount(SavingsAccount? account)
    {
        if (account == null) return;
        _editingSavings        = account;
        NewSavingsName         = account.Name;
        NewSavingsBalance      = account.CurrentBalance;
        NewSavingsTargetAmount = account.IsGoal ? account.TargetAmount : (decimal?)null;
        NewSavingsCanWithdraw  = account.CanWithdraw;
        NewSavingsStartDate    = new DateTimeOffset(account.StartDate);
        NewSavingsNotes        = account.Notes;
        IsCreatingGoal         = account.IsGoal;
        IsAddingSavingsAccount = true;
    }

    [RelayCommand]
    private void SaveSavingsAccount()
    {
        if (string.IsNullOrWhiteSpace(NewSavingsName)) return;

        // Для копилки целевая сумма обязательна
        if (IsCreatingGoal && (NewSavingsTargetAmount ?? 0m) <= 0m)
        {
            ErrorMessage = Loc["Savings_GoalRequiredHint"];
            HasError     = true;
            return;
        }

        try
        {
            var isNew = _editingSavings == null;
            var item  = _editingSavings ?? new SavingsAccount();
            item.Name           = NewSavingsName.Trim();
            item.CurrentBalance = NewSavingsBalance ?? 0m;
            item.StartDate      = NewSavingsStartDate.Date;
            item.Notes          = NewSavingsNotes;
            if (isNew) item.IsGoal = IsCreatingGoal;

            // Поля только для копилки
            if (IsCreatingGoal)
            {
                item.TargetAmount = NewSavingsTargetAmount ?? 0m;
                item.CanWithdraw  = NewSavingsCanWithdraw;
            }
            else
            {
                // Счёт: всегда можно снимать, нет цели
                item.TargetAmount = 0m;
                item.CanWithdraw  = true;
            }

            _db.UpsertSavingsAccount(item);
            IsAddingSavingsAccount   = false;
            IsCreatingGoal           = false;
            HasError                 = false;
            ErrorMessage             = string.Empty;
            LoadSavings();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
            HasError     = true;
        }
    }

    [RelayCommand]
    private void CancelSavingsAccount()
    {
        IsAddingSavingsAccount = false;
        IsCreatingGoal         = false;
    }

    [RelayCommand]
    private void ArchiveSavingsAccount(SavingsAccount? account)
    {
        if (account == null) return;
        account.IsArchived = true;
        _db.UpsertSavingsAccount(account);
        if (SelectedSavingsAccount?.Id == account.Id) SelectedSavingsAccount = null;
        LoadSavings();
    }

    [RelayCommand]
    private void DeleteSavingsAccount(SavingsAccount? account)
    {
        if (account == null) return;
        _db.DeleteSavingsAccount(account.Id);
        if (SelectedSavingsAccount?.Id == account.Id) SelectedSavingsAccount = null;
        LoadSavings();
    }

    [RelayCommand]
    private void BeginDeposit(SavingsAccount? account)
    {
        if (account == null) return;
        SelectedSavingsAccount  = account;
        PendingSavingsTxType    = "deposit";
        NewSavingsTxAmount      = null;
        NewSavingsTxNote        = string.Empty;
        NewSavingsTxDate        = DateTimeOffset.Now;
        IsAddingSavingsTx       = true;
    }

    [RelayCommand]
    private void BeginWithdraw(SavingsAccount? account)
    {
        if (account == null || !account.CanWithdraw) return;
        SelectedSavingsAccount  = account;
        PendingSavingsTxType    = "withdrawal";
        NewSavingsTxAmount      = null;
        NewSavingsTxNote        = string.Empty;
        NewSavingsTxDate        = DateTimeOffset.Now;
        IsAddingSavingsTx       = true;
    }

    [RelayCommand]
    private void SaveSavingsTx()
    {
        decimal amount = NewSavingsTxAmount ?? 0m;
        if (amount <= 0 || SelectedSavingsAccount == null) return;
        try
        {
            var tx = new SavingsTransaction
            {
                AccountId = SelectedSavingsAccount.Id,
                Date      = NewSavingsTxDate.Date,
                Amount    = PendingSavingsTxType == "withdrawal" ? -amount : amount,
                Type      = PendingSavingsTxType,
                Note      = NewSavingsTxNote
            };
            _db.AddSavingsTransaction(tx);
            IsAddingSavingsTx = false;
            // Обновляем текущий баланс в памяти
            SelectedSavingsAccount.CurrentBalance += tx.Amount;
            OnPropertyChanged(nameof(SelectedSavingsAccount));
            LoadSavingsTx(SelectedSavingsAccount.Id);
            // Обновляем список счетов (баланс изменился)
            var idx = SavingsAccounts.IndexOf(SelectedSavingsAccount);
            if (idx >= 0)
            {
                SavingsAccounts.RemoveAt(idx);
                SavingsAccounts.Insert(idx, SelectedSavingsAccount);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
            HasError     = true;
        }
    }

    [RelayCommand]
    private void CancelSavingsTx() => IsAddingSavingsTx = false;

    [RelayCommand]
    private void DeleteSavingsTx(SavingsTransaction? tx)
    {
        if (tx == null || SelectedSavingsAccount == null) return;
        _db.DeleteSavingsTransaction(tx.Id);
        SelectedSavingsAccount.CurrentBalance -= tx.Amount;
        SavingsTransactions.Remove(tx);
        OnPropertyChanged(nameof(SelectedSavingsAccount));
    }

    // ── Экспорт Excel ────────────────────────────────────────────────
    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var desktop = Avalonia.Application.Current?.ApplicationLifetime as
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var win = desktop?.Windows.LastOrDefault(w => w.DataContext is FinanceViewModel);
            if (win == null) return;

            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(win);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title             = Loc["Finance_ExportExcel"],
                    SuggestedFileName = $"FocusFlow_Finance_{DateTime.Now:yyyy-MM-dd}.xlsx",
                    DefaultExtension  = "xlsx",
                    FileTypeChoices   = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Excel")
                            { Patterns = new[] { "*.xlsx" } }
                    }
                });

            if (file == null) return;

            await using var stream = await file.OpenWriteAsync();
            ExcelExporter.Export(stream, Incomes.ToList(), Expenses.ToList(), Subscriptions.ToList(), Loans.ToList(), Loc);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
            HasError     = true;
        }
    }
}
