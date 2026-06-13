using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FocusFlowFinal.Models;

/// <summary>
/// Runtime-only observable wrapper for a Subtask, used in the task list view.
/// Not persisted to DB — created fresh when tasks are loaded.
/// </summary>
public class SubtaskViewItem : INotifyPropertyChanged
{
    private readonly Subtask _subtask;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Action? OnToggled { get; set; }

    public string Title => _subtask.Title;

    private bool _isCompleted;
    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (_isCompleted == value) return;
            _isCompleted = value;
            _subtask.IsCompleted = value;
            OnPropertyChanged();
            OnToggled?.Invoke();
        }
    }

    public SubtaskViewItem(Subtask subtask)
    {
        _subtask = subtask;
        _isCompleted = subtask.IsCompleted;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
