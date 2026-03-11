using FloatingClock.WPF.Helpers;
using FloatingClock.WPF.Models;
using FloatingClock.WPF.Utilities;
using System.Windows;

namespace FloatingClock.WPF.ViewModels;

internal class MainViewModel : BaseViewModel
{
    internal AppState State { get; set; } = new();
    private TimeKeeper _timeKeeper = new();
    public TimeKeeper TimeKeeper
    {
        get { return _timeKeeper; }
        set { SetProperty(ref _timeKeeper, value); }
    }

    internal event Action? MinuteReached;
    internal event Action? HourReached;

    public MainViewModel()
    {
        TimeKeeper.HourReached += TimeKeeper_HourReached;
        TimeKeeper.MinuteReached += TimeKeeper_MinuteReached;
    }

    private async void TimeKeeper_MinuteReached(object? sender, MinuteReachedEventArgs e)
    {
        MinuteReached?.Invoke();
    }

    private async void TimeKeeper_HourReached(object? sender, HourReachedEventArgs e)
    {
        HourReached?.Invoke();
        await AudioPlayer.Play("chime-quiet");
    }

    internal void Load()
    {
        State = StateManagement.DoLoad();
        RestoreAppProperties(State);
    }

    internal void Close()
    {
        StateManagement.DoSave(State);
    }

    private static void RestoreAppProperties(AppState appState)
    {
        PositionHelpers.SetAppSize(appState.AppSize);
        PositionHelpers.SetLocation(appState.Location);
    }

    internal void ResetLocation()
    {
        var x = (SystemParameters.PrimaryScreenWidth / 2) - (Application.Current.MainWindow.Width / 2);
        var y = (SystemParameters.PrimaryScreenHeight / 2) - (Application.Current.MainWindow.Height / 2);
        var location = new Point(x, y);

        State.Location = location;
        State.AppSize = AppSize.Medium;
        PositionHelpers.SetAppSize(State.AppSize);
        PositionHelpers.SetLocation(State.Location);
        StateManagement.DoSave(State);
    }

    internal void UpdateLocation(Point? point = null)
    {
        var newPosition = point ?? PositionHelpers.GetLocation();
        if (State.Location == newPosition)
        {
            return;
        }

        State.Location = newPosition;
        PositionHelpers.SetLocation(newPosition);
        StateManagement.DoSave(State);
    }

    internal void UpdateSize(AppSize appSize)
    {
        if (State.AppSize == appSize)
        {
            return;
        }

        State.AppSize = appSize;
        PositionHelpers.SetAppSize(appSize);
        StateManagement.DoSave(State);
    }
}
