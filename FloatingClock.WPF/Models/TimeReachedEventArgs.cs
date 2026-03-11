namespace FloatingClock.WPF.Models;

public class MinuteReachedEventArgs : EventArgs
{
    public int NewMinute { get; set; }
    public int OldMinute { get; set; }
}

public class HourReachedEventArgs : EventArgs
{
    public int NewHour { get; set; }
    public int OldHour { get; set; }
}