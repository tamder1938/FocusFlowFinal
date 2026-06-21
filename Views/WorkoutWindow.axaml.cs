using Avalonia.Controls;
using Avalonia.Interactivity;
using FocusFlowFinal.Models.Workout;
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
        Vm.ProgramListVm.AddProgramRequested       += OnAddProgramRequested;
        Vm.ProgramListVm.EditProgramRequested      += OnEditProgramRequested;

        await Vm.InitializeAsync();

        SetActiveTab(0);
        SetLeftTab(0);
    }

    // ── Вкладки левой колонки ──────────────────────────────────────────

    private void LeftTab_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (int.TryParse(btn.Tag?.ToString(), out var idx))
            SetLeftTab(idx);
    }

    private void SetLeftTab(int idx)
    {
        if (PanelPrograms != null) PanelPrograms.IsVisible = idx == 0;
        if (PanelHistory  != null) PanelHistory.IsVisible  = idx == 1;

        SetTabActive2(LeftTabPrograms, idx == 0);
        SetTabActive2(LeftTabHistory,  idx == 1);
    }

    private static void SetTabActive2(Button? btn, bool active)
    {
        if (btn == null) return;
        if (active) btn.Classes.Add("left-tab-active");
        else        btn.Classes.Remove("left-tab-active");
    }

    // ── Карточка истории — клик (раскрыть/свернуть) ───────────────────

    private void HistoryCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border { DataContext: SessionHistoryItemViewModel vm })
            vm.ToggleExpandedCommand.Execute(null);
    }

    // ── Вкладки правой колонки ─────────────────────────────────────────

    private void RightTab_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (int.TryParse(btn.Tag?.ToString(), out var idx))
            SetActiveTab(idx);
    }

    private void SetActiveTab(int idx)
    {
        if (PanelExercises != null) PanelExercises.IsVisible = idx == 0;
        if (PanelAnalytics  != null) PanelAnalytics.IsVisible  = idx == 1;
        if (PanelStrength   != null) PanelStrength.IsVisible   = idx == 2;

        SetTabActive(TabExercises, idx == 0);
        SetTabActive(TabAnalytics,  idx == 1);
        SetTabActive(TabStrength,   idx == 2);
    }

    private static void SetTabActive(Button? btn, bool active)
    {
        if (btn == null) return;
        if (active) btn.Classes.Add("right-tab-active");
        else        btn.Classes.Remove("right-tab-active");
    }

    // ── Карточка программы — клик ──────────────────────────────────────

    private void ProgramCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border { DataContext: WorkoutProgramCardViewModel cardVm })
            Vm?.ProgramListVm.SelectProgramCommand.Execute(cardVm);
    }

    // ── Диалог: добавить программу ─────────────────────────────────────

    private async void OnAddProgramRequested(object? sender, EventArgs e)
    {
        var vm = Vm;
        if (vm == null) return;

        var dialogVm = new AddProgramViewModel(vm.WorkoutRepo);
        await OpenProgramDialog(dialogVm, vm);
    }

    private async void OnEditProgramRequested(object? sender, WorkoutProgram program)
    {
        var vm = Vm;
        if (vm == null) return;

        var dialogVm = new AddProgramViewModel(vm.WorkoutRepo, program);
        await OpenProgramDialog(dialogVm, vm);
    }

    private async System.Threading.Tasks.Task OpenProgramDialog(
        AddProgramViewModel dialogVm, WorkoutViewModel wvm)
    {
        var dialog = new AddProgramDialog { DataContext = dialogVm };

        dialogVm.CloseRequested += (_, _) =>
        {
            dialog.Close();
            if (dialogVm.Saved) wvm.ProgramListVm.Refresh();
        };

        await dialog.ShowDialog(this);
    }

    // ── Диалог: добавить упражнение ────────────────────────────────────

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

    private void OnExerciseHistoryRequested(object? sender, Exercise exercise) { }
}
