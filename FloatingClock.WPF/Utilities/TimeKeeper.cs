using FloatingClock.WPF.Models;
using FloatingClock.WPF.ViewModels;
using System.Windows;
using System.Windows.Threading;

namespace FloatingClock.WPF.Utilities;

internal class TimeKeeper : BaseViewModel
{
    private DispatcherTimer? MainTimer { get; set; }
    private DispatcherTimer? AlarmTimer { get; set; }
    private DateTime _oldTime;

    private DateTime _currentTime = DateTime.Now;
    public DateTime CurrentTime
    {
        get { return _currentTime; }
        set { SetProperty(ref _currentTime, value); }
    }

    private TimeSpan _countdown;
    public TimeSpan Countdown
    {
        get { return _countdown; }
        set { SetProperty(ref _countdown, value); }
    }

    public TimeKeeper()
    {
        StartTimer();
    }

    internal void StartTimer()
    {
        if (MainTimer == null)
        {
            MainTimer = new() { Interval = new(0, 0, 0, 0, 500) };
            MainTimer.Tick += MainTimer_Tick;
        }
        else
        {
            MainTimer.Stop();
        }

        MainTimer.Start();
    }

    internal void StopTimer()
    {
        if (MainTimer == null)
        {
            return;
        }

        MainTimer.Stop();
        MainTimer.Tick -= MainTimer_Tick;
        MainTimer = null;
    }

    private void MainTimer_Tick(object? sender, EventArgs e)
    {
        _oldTime = CurrentTime;
        CurrentTime = DateTime.Now;

        if (_oldTime.Minute + 1 == CurrentTime.Minute)
        {
            MinuteReached?.Invoke(this, new MinuteReachedEventArgs { OldMinute = _oldTime.Minute, NewMinute = CurrentTime.Minute });
        }

        if (_oldTime.Hour + 1 == CurrentTime.Hour)
        {
            HourReached?.Invoke(this, new HourReachedEventArgs { OldHour = _oldTime.Hour, NewHour = CurrentTime.Hour });
        }
    }

    internal void CreateAlarm(TimeSpan timeSpan)
    {
        StopAlarm();

        if (timeSpan <= TimeSpan.Zero)
        {
            return;
        }

        Countdown = timeSpan;
        AlarmTimer = new DispatcherTimer(new(0, 0, 1), DispatcherPriority.Normal, AlarmTimer_Tick, Application.Current.Dispatcher);
        AlarmTimer.Start();
    }

    internal void StopAlarm()
    {
        if (AlarmTimer == null)
        {
            return;
        }

        AlarmTimer.Stop();
        AlarmTimer.Tick -= AlarmTimer_Tick;
        AlarmTimer = null;
    }

    private async void AlarmTimer_Tick(object? sender, EventArgs e)
    {
        if (Countdown > TimeSpan.Zero)
        {
            Countdown = Countdown.Add(TimeSpan.FromSeconds(-1));
            return;
        }

        StopAlarm();
        AlarmFinished?.Invoke(this, EventArgs.Empty);
    }

    #region Event Handling
    protected virtual void OnMinuteReached(MinuteReachedEventArgs args) => MinuteReached?.Invoke(this, args);
    protected virtual void OnHourReached(HourReachedEventArgs args) => HourReached?.Invoke(this, args);
    protected virtual void OnAlarmFinished(EventArgs args) => AlarmFinished?.Invoke(this, args);

    internal event EventHandler<MinuteReachedEventArgs>? MinuteReached;
    internal event EventHandler<HourReachedEventArgs>? HourReached;
    internal event EventHandler<EventArgs>? AlarmFinished;
    #endregion
}
