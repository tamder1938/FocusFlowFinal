using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace FocusFlowFinal.ViewModels;

public partial class RestTimerViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _timer;

    [ObservableProperty] private int  _totalSeconds = 90;
    [ObservableProperty] private int  _remaining;
    [ObservableProperty] private bool _isRunning;

    public string RemainingLabel =>
        $"{Remaining / 60:D2}:{Remaining % 60:D2}";

    public double Progress =>
        TotalSeconds > 0 ? 1.0 - (double)Remaining / TotalSeconds : 1.0;

    public event EventHandler? TimerFinished;

    public RestTimerViewModel()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTick;
    }

    public void Start(int seconds)
    {
        TotalSeconds = Math.Max(1, seconds);
        Remaining    = TotalSeconds;
        IsRunning    = true;
        _timer.Start();
        RefreshDisplay();
    }

    public void Stop()
    {
        _timer.Stop();
        IsRunning = false;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (Remaining <= 0)
        {
            Stop();
            TimerFinished?.Invoke(this, EventArgs.Empty);
            return;
        }
        Remaining--;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        OnPropertyChanged(nameof(RemainingLabel));
        OnPropertyChanged(nameof(Progress));
    }

    [RelayCommand]
    private void Skip()
    {
        Stop();
        Remaining = 0;
        RefreshDisplay();
    }

    [RelayCommand]
    private void AdjustTime(string param)
    {
        if (!int.TryParse(param, out int delta)) return;
        Remaining    = Math.Max(0, Remaining + delta);
        TotalSeconds = Math.Max(TotalSeconds, Remaining);
        RefreshDisplay();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }
}
