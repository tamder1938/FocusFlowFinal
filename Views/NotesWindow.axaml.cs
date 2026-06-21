using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FocusFlowFinal.Models.Notes;
using FocusFlowFinal.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlowFinal.Views;

public partial class NotesWindow : Window
{
    public NotesWindow()
    {
        InitializeComponent();
    }

    private NoteViewModel? Vm => DataContext as NoteViewModel;

    // ── Выбор заметки из списка ──────────────────────────────────────

    private void NoteItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border b && b.DataContext is Note note && Vm != null)
            Vm.SelectedNote = note;
    }

    // ── Клавиша Enter в поле тега ────────────────────────────────────

    private void TagInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Vm?.AddTagCommand.Execute(null);
            e.Handled = true;
        }
    }

    // ── Форматирование текста ────────────────────────────────────────

    private TextBox? Editor => this.FindControl<TextBox>("ContentEditor");

    private void ApplyInlineFormat(string prefix, string suffix)
    {
        var tb = Editor;
        if (tb == null) return;

        int start = tb.SelectionStart;
        int end   = tb.SelectionEnd;
        if (start > end) (start, end) = (end, start);
        int len = end - start;

        var text = tb.Text ?? string.Empty;
        if (len > 0)
        {
            string selected = text.Substring(start, len);
            tb.Text = text.Remove(start, len).Insert(start, prefix + selected + suffix);
            tb.SelectionStart = start;
            tb.SelectionEnd   = start + prefix.Length + len + suffix.Length;
        }
        else
        {
            tb.Text = text.Insert(start, prefix + suffix);
            tb.SelectionStart = start + prefix.Length;
            tb.SelectionEnd   = start + prefix.Length;
        }
        tb.Focus();
    }

    private void ApplyLinePrefix(string linePrefix)
    {
        var tb = Editor;
        if (tb == null) return;

        int pos  = tb.SelectionStart;
        var text = tb.Text ?? string.Empty;

        int lineStart = text.LastIndexOf('\n', Math.Max(0, pos - 1)) + 1;
        tb.Text = text.Insert(lineStart, linePrefix);
        tb.SelectionStart = pos + linePrefix.Length;
        tb.SelectionEnd   = pos + linePrefix.Length;
        tb.Focus();
    }

    private void BtnBold_Click(object? sender, RoutedEventArgs e)   => ApplyInlineFormat("**", "**");
    private void BtnItalic_Click(object? sender, RoutedEventArgs e) => ApplyInlineFormat("_", "_");
    private void BtnCode_Click(object? sender, RoutedEventArgs e)   => ApplyInlineFormat("`", "`");
    private void BtnH1_Click(object? sender, RoutedEventArgs e)     => ApplyLinePrefix("# ");
    private void BtnH2_Click(object? sender, RoutedEventArgs e)     => ApplyLinePrefix("## ");
    private void BtnQuote_Click(object? sender, RoutedEventArgs e)  => ApplyLinePrefix("> ");
    private void BtnList_Click(object? sender, RoutedEventArgs e)   => ApplyLinePrefix("- ");

    // ── Удаление заметки ─────────────────────────────────────────────

    private async void BtnDelete_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm?.SelectedNote == null) return;
        var dlg = new Window
        {
            Width  = 360, Height = 160,
            Title  = "Подтверждение",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Background,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = Vm.Loc["Notes_DeleteConfirm"], TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Foreground = (Avalonia.Media.IBrush?)Application.Current?.Resources["PrimaryTextBrush"] },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 8,
                        Children =
                        {
                            new Button { Content = Vm.Loc["Yes"], Tag = true,
                                Background = Avalonia.Media.Brush.Parse("#EF4444"),
                                Foreground = Avalonia.Media.Brushes.White,
                                Padding = new Thickness(16, 6) },
                            new Button { Content = Vm.Loc["No"], Tag = false,
                                Padding = new Thickness(16, 6) }
                        }
                    }
                }
            }
        };

        bool confirmed = false;
        foreach (var btn in ((StackPanel)((StackPanel)dlg.Content!).Children[1]).Children.OfType<Button>())
        {
            btn.Click += (_, _) => { confirmed = btn.Tag is true; dlg.Close(); };
        }
        await dlg.ShowDialog(this);
        if (confirmed) await Vm.DeleteNoteCommand.ExecuteAsync(null);
    }

    // ── Экспорт ──────────────────────────────────────────────────────

    private async void BtnExportMd_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm?.SelectedNote == null) return;
        var path = await PickSavePath("Markdown файлы (*.md)", "md");
        if (path != null) Vm.ExportAsMd(path);
    }

    private async void BtnExportHtml_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm?.SelectedNote == null) return;
        var path = await PickSavePath("HTML файлы (*.html)", "html");
        if (path != null) Vm.ExportAsHtml(path);
    }

    private async void BtnExportTxt_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm?.SelectedNote == null) return;
        var path = await PickSavePath("Текстовые файлы (*.txt)", "txt");
        if (path != null) Vm.ExportAsTxt(path);
    }

    private async System.Threading.Tasks.Task<string?> PickSavePath(string filterName, string ext)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title            = $"Сохранить как .{ext}",
            DefaultExtension = ext,
            FileTypeChoices  = new[]
            {
                new FilePickerFileType(filterName) { Patterns = new[] { $"*.{ext}" } }
            },
            SuggestedFileName = Vm?.SelectedNote?.Title ?? "note"
        });
        return file?.TryGetLocalPath();
    }
}
