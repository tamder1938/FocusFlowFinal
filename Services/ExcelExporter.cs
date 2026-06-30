using ClosedXML.Excel;
using FocusFlowFinal.Models.Finance;
using FocusFlowFinal.Services;
using System;
using System.Collections.Generic;
using System.IO;

namespace FocusFlowFinal.Services;

/// <summary>Экспорт финансовых данных в Excel через ClosedXML.</summary>
public static class ExcelExporter
{
    public static void Export(
        Stream destination,
        List<FinanceIncome> incomes,
        List<FinanceExpense> expenses,
        List<FinanceSubscriptionItem> subscriptions,
        List<FinanceLoan> loans,
        LocalizationService loc,
        List<SavingsAccount>? savings = null)
    {
        using var wb = new XLWorkbook();

        AddIncomeSheet(wb, incomes, loc);
        AddExpenseSheet(wb, expenses, loc);
        AddSubSheet(wb, subscriptions, loc);
        AddLoanSheet(wb, loans, loc);
        if (savings != null && savings.Count > 0)
            AddSavingsSheet(wb, savings, loc);

        wb.SaveAs(destination);
    }

    private static void AddIncomeSheet(XLWorkbook wb, List<FinanceIncome> items, LocalizationService loc)
    {
        var ws = wb.Worksheets.Add(loc["FinanceTab_Income"]);
        ws.Cell(1, 1).Value = loc["Finance_DateLbl"].TrimEnd(':');
        ws.Cell(1, 2).Value = loc["Finance_CategoryLbl"].TrimEnd(':');
        ws.Cell(1, 3).Value = loc["Finance_AmountLbl"].TrimEnd(':');
        ws.Cell(1, 4).Value = loc["Finance_NoteLbl"].TrimEnd(':');
        StyleHeader(ws.Row(1));

        int row = 2;
        foreach (var x in items)
        {
            ws.Cell(row, 1).Value = x.Date.ToString("dd.MM.yyyy");
            ws.Cell(row, 2).Value = x.Category;
            ws.Cell(row, 3).Value = (double)x.Amount;
            ws.Cell(row, 4).Value = x.Note;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void AddExpenseSheet(XLWorkbook wb, List<FinanceExpense> items, LocalizationService loc)
    {
        var ws = wb.Worksheets.Add(loc["FinanceTab_Expense"]);
        ws.Cell(1, 1).Value = loc["Finance_DateLbl"].TrimEnd(':');
        ws.Cell(1, 2).Value = loc["Finance_CategoryLbl"].TrimEnd(':');
        ws.Cell(1, 3).Value = loc["Finance_AmountLbl"].TrimEnd(':');
        ws.Cell(1, 4).Value = loc["Finance_NoteLbl"].TrimEnd(':');
        StyleHeader(ws.Row(1));

        int row = 2;
        foreach (var x in items)
        {
            ws.Cell(row, 1).Value = x.Date.ToString("dd.MM.yyyy");
            ws.Cell(row, 2).Value = x.Category;
            ws.Cell(row, 3).Value = (double)x.Amount;
            ws.Cell(row, 4).Value = x.Note;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void AddSubSheet(XLWorkbook wb, List<FinanceSubscriptionItem> items, LocalizationService loc)
    {
        var ws = wb.Worksheets.Add(loc["FinanceTab_Sub"]);
        ws.Cell(1, 1).Value = loc["Finance_NameLbl"].TrimEnd(':');
        ws.Cell(1, 2).Value = loc["Finance_AmountLbl"].TrimEnd(':');
        ws.Cell(1, 3).Value = loc["Finance_CycleLbl"].TrimEnd(':');
        ws.Cell(1, 4).Value = loc["Finance_NextDateLbl"].TrimEnd(':');
        ws.Cell(1, 5).Value = loc["Finance_PaidLbl"];
        StyleHeader(ws.Row(1));

        int row = 2;
        foreach (var x in items)
        {
            ws.Cell(row, 1).Value = x.Name;
            ws.Cell(row, 2).Value = (double)x.Amount;
            ws.Cell(row, 3).Value = x.BillingCycle == FinanceBillingCycle.Monthly
                ? loc["Finance_Monthly"] : loc["Finance_Yearly"];
            ws.Cell(row, 4).Value = x.NextBillingDate.ToString("dd.MM.yyyy");
            ws.Cell(row, 5).Value = x.IsPaid ? "✓" : "—";
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void AddLoanSheet(XLWorkbook wb, List<FinanceLoan> items, LocalizationService loc)
    {
        var ws = wb.Worksheets.Add(loc["FinanceTab_Loans"]);
        ws.Cell(1, 1).Value = loc["Finance_NameLbl"].TrimEnd(':');
        ws.Cell(1, 2).Value = loc["Finance_TotalLbl"].TrimEnd(':');
        ws.Cell(1, 3).Value = loc["Finance_RateLbl"].TrimEnd(':');
        ws.Cell(1, 4).Value = loc["Finance_TermLbl"].TrimEnd(':');
        ws.Cell(1, 5).Value = loc["Finance_MonthlyLbl"].TrimEnd(':');
        ws.Cell(1, 6).Value = loc["Finance_RemainLbl"].TrimEnd(':');
        ws.Cell(1, 7).Value = loc["Finance_StartDateLbl"].TrimEnd(':');
        StyleHeader(ws.Row(1));

        int row = 2;
        foreach (var x in items)
        {
            ws.Cell(row, 1).Value = x.Name;
            ws.Cell(row, 2).Value = (double)x.TotalAmount;
            ws.Cell(row, 3).Value = (double)x.InterestRate;
            ws.Cell(row, 4).Value = x.TermMonths;
            ws.Cell(row, 5).Value = (double)x.MonthlyPayment;
            ws.Cell(row, 6).Value = (double)x.RemainingBalance;
            ws.Cell(row, 7).Value = x.StartDate.ToString("dd.MM.yyyy");
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void AddSavingsSheet(XLWorkbook wb, List<SavingsAccount> items, LocalizationService loc)
    {
        var ws = wb.Worksheets.Add(loc["FinanceTab_Savings"]);
        ws.Cell(1, 1).Value = loc["Savings_NameLbl"].TrimEnd(':');
        ws.Cell(1, 2).Value = loc["Savings_CurrentBalanceLbl"].TrimEnd(':');
        ws.Cell(1, 3).Value = loc["Savings_TargetLbl"].TrimEnd(':');
        ws.Cell(1, 4).Value = loc["Savings_StartDateLbl"].TrimEnd(':');
        ws.Cell(1, 5).Value = loc["Savings_NotesLbl"].TrimEnd(':');
        StyleHeader(ws.Row(1));

        int row = 2;
        foreach (var x in items)
        {
            ws.Cell(row, 1).Value = x.Name;
            ws.Cell(row, 2).Value = (double)x.CurrentBalance;
            ws.Cell(row, 3).Value = x.TargetAmount > 0 ? (double)x.TargetAmount : 0;
            ws.Cell(row, 4).Value = x.StartDate.ToString("dd.MM.yyyy");
            ws.Cell(row, 5).Value = x.Notes;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void StyleHeader(IXLRow row)
    {
        row.Style.Font.Bold = true;
        row.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
        row.Style.Font.FontColor = XLColor.White;
    }
}
