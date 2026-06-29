using System;
using Avalonia.Controls;
using Avalonia.Input;
using FocusFlowFinal.Services;
using FocusFlowFinal.ViewModels;

namespace FocusFlowFinal.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => RegisterHotkeys();
        HotkeyService.Changed += OnHotkeysChanged;
        Closed += (_, _) => HotkeyService.Changed -= OnHotkeysChanged;
    }

    private void OnHotkeysChanged(object? sender, EventArgs e)
        => Avalonia.Threading.Dispatcher.UIThread.Post(RegisterHotkeys);

    private void RegisterHotkeys()
    {
        if (DataContext is not MainViewModel vm) return;
        KeyBindings.Clear();
        var all = HotkeyService.GetAll();
        TryAdd(all["Day"],     vm.SwitchToDayCommand);
        TryAdd(all["Week"],    vm.SwitchToWeekCommand);
        TryAdd(all["Month"],   vm.SwitchToMonthCommand);
        TryAdd(all["Year"],    vm.SwitchToYearCommand);
        TryAdd(all["NewTask"], vm.AddNewTaskCommand);
        TryAdd(all["Today"],   vm.GoToTodayCommand);
        vm.RefreshHotkeyLabels();
    }

    private void TryAdd(string gestureStr, System.Windows.Input.ICommand? command)
    {
        if (command == null) return;
        try
        {
            var gesture = KeyGesture.Parse(gestureStr);
            KeyBindings.Add(new KeyBinding { Gesture = gesture, Command = command });
        }
        catch { }
    }
}
