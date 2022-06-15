using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace FloatingClock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string currentTime;
        private string shortPattern = "HHmm";
        private string fullPattern = "ddd, dd MMM yyyy";
        private DispatcherTimer timer;
        private DispatcherTimer alarmTimer;
        private bool timesUp = false;
        private Color originalColor;
        private List<TextBlock> labels = new List<TextBlock>();
        private TimeSpan countDown;
        private bool _inFocus = false;
        private char _currentAppSize = 'l';
        private bool _isLogoffTimer = false;

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOACTIVATE = 0x0010;

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void AssignElements()
        {
            labels.Add(t1);
            labels.Add(t2);
            labels.Add(t3);
            labels.Add(t4);
        }

        private void TimeKeeper()
        {
            if (timer == null) timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 500) };

            MinuteReached += MainWindow_MinuteReached;
            HourReached += MainWindow_HourReached;

            SetTime();

            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void MainWindow_HourReached(object sender, EventArgs e)
        {
            SetClockColor(Colors.CornflowerBlue);
            SoundPlayer player = new SoundPlayer(Properties.Resources.Grandfather_clock_chimes_quiet);
            player.Play();
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(2500);
                SetClockColor(originalColor);
            });
        }

        private void MainWindow_MinuteReached(object sender, EventArgs e)
        {

        }

        public event EventHandler MinuteReached;
        public event EventHandler HourReached;

        private void Timer_Tick(object sender, EventArgs e) => SetTime();

        private void SetTime()
        {
            var oldTime = currentTime;
            var dateTime = DateTime.Now;
            var now = dateTime.ToString(shortPattern);
            if (currentTime != now)
            {
                currentTime = now;
                if (oldTime == null)
                {
                    oldTime = currentTime;
                }
                Task.Factory.StartNew(() =>
                {
                    // Separate process to not block UI thread.                    
                    var oldHour = oldTime.Substring(0, 2);
                    var oldMinute = oldTime.Substring(2, 2);
                    var newHour = now.Substring(0, 2);
                    var newMinute = now.Substring(2, 2);

                    int.TryParse(oldHour, out int oldHourInt);
                    int.TryParse(newHour, out int newHourInt);
                    int.TryParse(oldMinute, out int oldMinuteInt);
                    int.TryParse(newMinute, out int newMinuteInt);

                    if (oldHourInt + 1 == newHourInt)
                    {
                        HourReached?.Invoke(this, EventArgs.Empty);
                    }

                    if (oldMinuteInt + 1 == newMinuteInt)
                    {
                        MinuteReached?.Invoke(this, EventArgs.Empty);
                    }
                });
                for (int i = 0; i < labels.Count; i++)
                {
                    labels[i].Text = now[i].ToString();
                }

                var fullTime = dateTime.ToString(fullPattern);
                SmallTime.Text = now.Substring(0, 2) + ":" + now.Substring(2, 2);
                Date.Text = fullTime;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Close();
            }
            else
            {
                if (e.ChangedButton == MouseButton.Left)
                {

                    if (alarmTimer != null && timesUp)
                    {
                        RemoveAlarmTimer();
                    }
                    else
                    {
                        Border.BorderBrush = Brushes.LawnGreen;
                        this.DragMove();
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowPosition();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.Activated += Current_Activated;
            Application.Current.Deactivated += Current_Deactivated;
            AssignElements();
            RestoreWindowPosition();
            VisibilityToggle(this.IsMouseOver);
            DebugCheck();
            TimeKeeper();
            originalColor = GetOriginalColor();
            txtCountdown.Visibility = Visibility.Collapsed;
            this.Visibility = Visibility.Visible;
        }

        private void Current_Activated(object sender, EventArgs e)
        {
            _inFocus = true;
        }

        private void Current_Deactivated(object sender, EventArgs e)
        {
            _inFocus = false;
            SendToBack();
        }

        private void DebugCheck()
        {
#if DEBUG            
            SetClockColor(Colors.SkyBlue);
#endif
        }

        private void SetClockColor(Color color)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SolidColorBrush newColor = new SolidColorBrush(color);
                for (int i = 0; i < labels.Count; i++)
                {
                    labels[i].Foreground = newColor;
                }
                period.Foreground = newColor;
                SmallTime.Foreground = newColor;
                Date.Foreground = newColor;
            });
        }

        private void SetClockFontSize(int size)
        {
            for (int i = 0; i < labels.Count; i++)
            {
                labels[i].FontSize = size;
            }
            period.FontSize = size * 0.74f;
            Date.FontSize = size * 0.22f;
            SmallTime.FontSize = size * 0.4f;
        }

        private void RestoreWindowPosition()
        {
            _currentAppSize = Properties.Settings.Default.AppSize;
            SetAppSize(_currentAppSize);
            if (Properties.Settings.Default.HasSetDefaults)
            {
                System.Drawing.Point location = Properties.Settings.Default.Location;
                SetLocation(location);
                System.Drawing.Size size = Properties.Settings.Default.Size;
                SetSize(size);
            }
        }

        private void SendToBack()
        {
            var hWnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }

        private void SaveWindowPosition()
        {
            Properties.Settings.Default.Location = GetLocation();
            Properties.Settings.Default.Size = GetSize();
            Properties.Settings.Default.AppSize = _currentAppSize;
            Properties.Settings.Default.HasSetDefaults = true;

            Properties.Settings.Default.Save();
        }

        private void ResetWindowPosition()
        {
            var x = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
            var y = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);
            var loc = new System.Drawing.Point((int)Math.Round(x), (int)Math.Round(y));
            Properties.Settings.Default.Location = loc;
            Properties.Settings.Default.Size = GetSize();
            Properties.Settings.Default.AppSize = 'l';
            _currentAppSize = 'l';
            SetAppSize('l');
            Properties.Settings.Default.HasSetDefaults = true;

            Properties.Settings.Default.Save();

            SetLocation(loc);
        }

        private void SetLocation(System.Drawing.Point point)
        {
            Application.Current.MainWindow.Left = point.X;
            Application.Current.MainWindow.Top = point.Y;
        }

        private void SetSize(System.Drawing.Size size)
        {
            Application.Current.MainWindow.Width = size.Width;
            Application.Current.MainWindow.Height = size.Height;
        }

        private void SetAppSize(char letter)
        {
            if (letter == 's')
            {
                // Small size.
                SetSize(new System.Drawing.Size(330, 120));
                Properties.Settings.Default.AppSize = 's';
                SetClockFontSize(135);
            }
            else if (letter == 'm')
            {
                // Medium size.
                SetSize(new System.Drawing.Size(472, 180));
                Properties.Settings.Default.AppSize = 'm';
                SetClockFontSize(203);
            }
            else
            {
                // Large size.
                SetSize(new System.Drawing.Size(630, 240));
                Properties.Settings.Default.AppSize = 'l';
                SetClockFontSize(270);
                letter = 'l'; // Forces letter to be 'L'.
            }
            _currentAppSize = letter;
            Properties.Settings.Default.Save();
        }

        private System.Drawing.Point GetLocation()
        {
            return new System.Drawing.Point((int)Application.Current.MainWindow.Left, (int)Application.Current.MainWindow.Top);
        }

        private System.Drawing.Size GetSize()
        {
            return new System.Drawing.Size((int)Application.Current.MainWindow.Width, (int)Application.Current.MainWindow.Height);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                SaveWindowPosition();
                Border.BorderBrush = Brushes.Transparent;
            }
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            VisibilityToggle(true);
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            VisibilityToggle(false);
        }

        private void VisibilityToggle(bool visible)
        {
            ShortDate.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
            FullDate.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Menu_Quit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Menu_SetTimer_Click(object sender, RoutedEventArgs e)
        {
            RemoveAlarmTimer();
            AlarmDialog alarmDialog = new AlarmDialog("How long do you want the timer for?", "10");

            if (alarmDialog.ShowDialog() == true)
            {
                originalColor = GetOriginalColor();
                var time = 0;

                switch (alarmDialog.SelectedTimeIndex)
                {
                    case 0:
                        // Minutes
                        int.TryParse(alarmDialog.Answer, out time);
                        countDown = new TimeSpan(0, time, 0);
                        break;
                    case 1:
                        // Hours
                        int.TryParse(alarmDialog.Answer, out time);
                        countDown = new TimeSpan(time, 0, 0);
                        break;
                    case 2:
                        // Specific Time
                        var split = alarmDialog.Answer.Split(':');

                        if (split.Length < 2)
                        {
                            return;
                        }

                        var hour = 0;
                        var minute = 0;
                        int.TryParse(split[0], out hour);
                        int.TryParse(split[1], out minute);

                        hour = hour > 12 ? hour - 12 : hour;
                        var now = DateTime.Now;
                        var d = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
                        if (now > d)
                        {
                            d = new DateTime(now.Year, now.Month, now.Day, hour + 12, minute, 0);
                        }
                        countDown = d.Subtract(new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0));
                        break;
                    default:
                        countDown = TimeSpan.Zero;
                        break;
                }

                if (countDown != TimeSpan.Zero)
                {
                    alarmTimer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, (s, ev) => AlarmTick_Timer(s, ev), Application.Current.Dispatcher);

                    Menu_CancelTimer.Visibility = Visibility.Visible;
                    Menu_TimerSeperator.Visibility = Visibility.Visible;
                    alarmTimer.Start();
                }
            }
        }

        private void Menu_CancelTimer_Click(object sender, RoutedEventArgs e)
        {
            originalColor = GetOriginalColor();
            SetClockColor(Colors.LightGreen);
            countDown = TimeSpan.Zero;
            txtCountdown.Visibility = Visibility.Collapsed;
            RemoveAlarmTimer();
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(700);
                SetClockColor(originalColor);
            });

            if (_isLogoffTimer)
            {
                _isLogoffTimer = false;
                // Abort Logoff timer.
                Process.Start("Shutdown", "/a");
            }
        }

        private void RemoveAlarmTimer()
        {
            if (alarmTimer != null)
            {
                Menu_CancelTimer.Visibility = Visibility.Collapsed;
                Menu_TimerSeperator.Visibility = Visibility.Collapsed;
                timesUp = false;
                alarmTimer.Stop();
                alarmTimer.Tick -= AlarmTick_Timer;
                alarmTimer = null;
            }
        }

        private void AlarmTick_Timer(object sender, EventArgs e)
        {
            txtCountdown.Text = countDown.ToString("c");
            if (countDown != TimeSpan.Zero)
            {
                txtCountdown.Visibility = Visibility.Visible;
                countDown = countDown.Add(TimeSpan.FromSeconds(-1));
            }
            else
            {
                txtCountdown.Visibility = Visibility.Collapsed;
                timesUp = true;
                RemoveAlarmTimer();

                Task.Factory.StartNew(() =>
                {
                    SetClockColor(Colors.Red);
                    var alarmSound = new SoundPlayer(Properties.Resources.Alarm_ringtone);
                    alarmSound.PlaySync();
                    for (int i = 0; i < 4; i++)
                    {
                        SetClockColor(Colors.Red);
                        Thread.Sleep(500);
                        SetClockColor(Colors.DarkRed);
                        alarmSound.PlaySync();
                    }

                    alarmSound.Stop();
                    SetClockColor(originalColor);
                });
            }
        }

        private void LogoutTick_Timer(object sender, EventArgs e)
        {
            txtCountdown.Text = countDown.ToString("c");
            if (countDown != TimeSpan.Zero)
            {
                txtCountdown.Visibility = Visibility.Visible;
                countDown = countDown.Add(TimeSpan.FromSeconds(-1));
            }
            else
            {
                txtCountdown.Visibility = Visibility.Collapsed;
                timesUp = true;
                RemoveAlarmTimer();
            }
        }

        private Color GetOriginalColor()
        {
            return (period.Foreground as SolidColorBrush).Color;
        }

        private void Menu_ResetPosition_Click(object sender, RoutedEventArgs e)
        {
            ResetWindowPosition();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
               && key == Key.Escape)
            {
                ResetWindowPosition();
            }
        }

        private void Menu_SetSizeSmall_Click(object sender, RoutedEventArgs e)
        {
            _currentAppSize = 's';
            SetAppSize(_currentAppSize);
            SaveWindowPosition();
        }

        private void Menu_SetSizeMedium_Click(object sender, RoutedEventArgs e)
        {
            _currentAppSize = 'm';
            SetAppSize(_currentAppSize);
            SaveWindowPosition();
        }

        private void Menu_SetSizeLarge_Click(object sender, RoutedEventArgs e)
        {
            _currentAppSize = 'l';
            SetAppSize(_currentAppSize);
            SaveWindowPosition();
        }

        private void Menu_SetLogoutTimer_Click(object sender, RoutedEventArgs e)
        {
            RemoveAlarmTimer();
            AlarmDialog alarmDialog = new AlarmDialog("When do you want to logout?", "480");

            if (alarmDialog.ShowDialog() == true)
            {
                originalColor = GetOriginalColor();

                var time = 0;

                switch (alarmDialog.SelectedTimeIndex)
                {
                    case 0:
                        // Minutes
                        int.TryParse(alarmDialog.Answer, out time);
                        countDown = new TimeSpan(0, time, 0);
                        break;
                    case 1:
                        // Hours
                        int.TryParse(alarmDialog.Answer, out time);
                        countDown = new TimeSpan(time, 0, 0);
                        break;
                    case 2:
                        // Specific Time
                        var split = alarmDialog.Answer.Split(':');

                        if (split.Length < 2)
                        {
                            return;
                        }

                        var hour = 0;
                        var minute = 0;
                        int.TryParse(split[0], out hour);
                        int.TryParse(split[1], out minute);
                        hour = hour > 12 ? hour - 12 : hour;
                        var now = DateTime.Now;
                        var d = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
                        if (now > d)
                        {
                            d = new DateTime(now.Year, now.Month, now.Day, hour + 12, minute, 0);
                        }
                        countDown = d.Subtract(new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0));
                        break;
                    default:
                        countDown = TimeSpan.Zero;
                        break;
                }

                if (countDown != TimeSpan.Zero)
                {
                    _isLogoffTimer = true;
                    alarmTimer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, (s, ev) => LogoutTick_Timer(s, ev), Application.Current.Dispatcher);
                    Menu_CancelTimer.Visibility = Visibility.Visible;
                    Menu_TimerSeperator.Visibility = Visibility.Visible;
                    alarmTimer.Start();
                    var arguments = $"/s /f /t {countDown.TotalSeconds}";
                    Process.Start("Shutdown", arguments);
                }
                else
                {
                    MessageBox.Show("Unable to parse provided time.");
                }
            }
        }
    }
}
