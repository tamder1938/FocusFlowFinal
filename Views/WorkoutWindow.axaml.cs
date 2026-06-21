using Avalonia.Controls;
using Avalonia.Interactivity;
using FocusFlowFinal.ViewModels;
using System;

namespace FocusFlowFinal.Views;

public partial class WorkoutWindow : Window
{
    public WorkoutWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private WorkoutViewModel? Vm => DataContext as WorkoutViewModel;

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (Vm == null) return;

        Vm.ExerciseListVm.AddExerciseRequested    += OnAddExerciseRequested;
        Vm.ExerciseListVm.ExerciseHistoryRequested += OnExerciseHistoryRequested;

        await Vm.InitializeAsync();

        // Устанавливаем начальную активную вкладку
        SetActiveTab(0);
    }

    // ── Переключение вкладок правой колонки ────────────────────────────

    private void RightTab_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (int.TryParse(btn.Tag?.ToString(), out var idx))
            SetActiveTab(idx);
    }

    private void SetActiveTab(int idx)
    {
        // Панели
        if (PanelExercises != null) PanelExercises.IsVisible = idx == 0;
        if (PanelAnalytics  != null) PanelAnalytics.IsVisible  = idx == 1;
        if (PanelStrength   != null) PanelStrength.IsVisible   = idx == 2;

        // Стили кнопок
        SetTabActive(TabExercises, idx == 0);
        SetTabActive(TabAnalytics,  idx == 1);
        SetTabActive(TabStrength,   idx == 2);
    }

    private static void SetTabActive(Button? btn, bool active)
    {
        if (btn == null) return;
        if (active)
            btn.Classes.Add("right-tab-active");
        else
            btn.Classes.Remove("right-tab-active");
    }

    // ── Диалог добавления упражнения ───────────────────────────────────

    private async void OnAddExerciseRequested(object? sender, EventArgs e)
    {
        var vm = Vm;
        if (vm == null) return;

        var dialogVm = new AddExerciseViewModel(vm.ExerciseListVm.Repository);
        var dialog   = new AddExerciseDialog { DataContext = dialogVm };

        dialogVm.CloseRequested += (_, _) =>
        {
            dialog.Close();
            if (dialogVm.Saved) vm.ExerciseListVm.Refresh();
        };

        await dialog.ShowDialog(this);
    }

    // ── История упражнения (заглушка — Этап 5) ─────────────────────────

    private void OnExerciseHistoryRequested(object? sender, Models.Workout.Exercise exercise)
    {
        // TODO Этап 5: открыть попап с историей подходов
    }
}
