using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FocusFlowFinal.Views;

public class SelectableDateItem
{
    public DateTime Date { get; set; }
    public string DisplayText => Date.ToString("dd.MM.yyyy (dddd)", new CultureInfo("ru-RU"));
}

public partial class DeleteCustomDaysWindow : Window
{
    public List<DateTime>? SelectedDates { get; private set; } = new();

    private ListBox? _listBox;

    public DeleteCustomDaysWindow()
    {
        InitializeComponent();

        _listBox = this.FindControl<ListBox>("DateListBox");

        var btnCancel  = this.FindControl<Button>("BtnCancel");
        var btnConfirm = this.FindControl<Button>("BtnConfirm");

        if (btnCancel  != null) btnCancel.Click  += (_, _) => { SelectedDates = null; Close(); };
        if (btnConfirm != null) btnConfirm.Click += (_, _) =>
        {
            SelectedDates = _listBox?.SelectedItems?
                .OfType<SelectableDateItem>()
                .Select(i => i.Date.Date)
                .ToList() ?? new List<DateTime>();
            Close();
        };
    }

    public DeleteCustomDaysWindow(List<DateTime> upcomingDates) : this()
    {
        if (_listBox != null)
            _listBox.ItemsSource = upcomingDates
                .Select(d => new SelectableDateItem { Date = d.Date })
                .ToList();
    }
}
