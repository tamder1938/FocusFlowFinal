using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FocusFlowFinal.ViewModels;

public partial class SubtaskEditItem : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private bool _isCompleted;

    // Команда удаления задаётся при создании из TaskDialogViewModel
    public IRelayCommand? RemoveCommand { get; set; }

    // Вызывается при изменении IsCompleted — TaskDialogViewModel передаёт коллбэк
    public Action? CompletionChanged { get; set; }

    partial void OnIsCompletedChanged(bool value) => CompletionChanged?.Invoke();
}
