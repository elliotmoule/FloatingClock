using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace FloatingClock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private string currentTime;
        private readonly string shortPattern = "HHmm";
        private readonly string fullPattern = "ddd, dd MMM yyyy";
        private DispatcherTimer timer;
        private DispatcherTimer alarmTimer;
        private bool timesUp = false;
        private Color originalColor;
        private Color defaultEdgeHighlightColor = Colors.Black;
        private Color edgeHighlightColor = Colors.LawnGreen;
        private readonly List<TextBlock> labels = new List<TextBlock>();
        private TimeSpan countDown;
        private AppSize _currentAppSize = AppSize.Large;
        private bool _isLogoffTimer = false;
        private readonly DropShadowEffect[] _dropShadowEffects = new DropShadowEffect[7];

        private const string _companyDirectoryName = "EMSTechnologies";
        private const string _appDirectoryName = "FloatingClock";
        private const string _propertiesFileName = "FloatingClock.json";
        #endregion

        #region Properties
        public string SaveLocation { get; set; }
        public static bool HasLoaded { get; set; }
        public static string AppDirectoryLocation => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _companyDirectoryName, _appDirectoryName);
        #endregion

        #region System Calls
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
        #endregion

        internal static AppProperties AppProperties { get; } = new AppProperties();

        public MainWindow()
        {
            InitializeComponent();
            LoadDropShadowControls();
        }

        private void LoadDropShadowControls()
        {
            _dropShadowEffects[0] = Shadow0;
            _dropShadowEffects[1] = Shadow1;
            _dropShadowEffects[2] = Shadow2;
            _dropShadowEffects[3] = Shadow3;
            _dropShadowEffects[4] = Shadow4;
            _dropShadowEffects[5] = Shadow5;
            _dropShadowEffects[6] = Shadow6;
        }

        private void UpdateShadowColor(Color color)
        {
            foreach (var dropShadowEffect in _dropShadowEffects)
            {
                if (dropShadowEffect == null)
                {
                    continue;
                }
                dropShadowEffect.Color = color;
            }
        }

        private static void SaveAppProperties()
        {
            if (HasLoaded)
            {
                try
                {
                    var propertiesFilePath = Path.Combine(AppDirectoryLocation, _propertiesFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(propertiesFilePath));
                    var appPropertiesJson = JsonSerializer.Serialize(AppProperties);
                    File.WriteAllText(propertiesFilePath, appPropertiesJson);
                }
                catch
                {
                    MessageBox.Show("Failed to save app properties!");
                }
            }
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
            var player = new SoundPlayer(Properties.Resources.Grandfather_clock_chimes_quiet);
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
                        if (AppProperties != null && AppProperties.LockPosition)
                        {
                            return;
                        }
                        Border.BorderBrush = new SolidColorBrush(edgeHighlightColor);
                        UpdateShadowColor(edgeHighlightColor);
                        this.DragMove();
                        if (e.LeftButton == MouseButtonState.Released)
                        {
                            SaveWindowPosition();
                            Border.BorderBrush = Brushes.Transparent;
                            UpdateShadowColor(defaultEdgeHighlightColor);
                        }
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
            try
            {
                var propertiesFilePath = Path.Combine(AppDirectoryLocation, _propertiesFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(propertiesFilePath));
                if (File.Exists(propertiesFilePath))
                {
                    MapAppProperties(JsonSerializer.Deserialize<AppProperties>(File.ReadAllText(propertiesFilePath)));
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Failed to loadapp properties!");
                });
            }
            Application.Current.Deactivated += Current_Deactivated;
            AssignElements();
            RestoreWindowPosition();
            VisibilityToggle(this.IsMouseOver);
            DebugCheck();
            TimeKeeper();
            originalColor = GetOriginalColor();
            txtCountdown.Visibility = Visibility.Collapsed;
            this.Visibility = Visibility.Visible;
            HasLoaded = true;
        }

        private void MapAppProperties(AppProperties newAppProperties)
        {
            if (newAppProperties == null)
            {
                return;
            }

            AppProperties.AppSize = newAppProperties.AppSize;
            AppProperties.Size = newAppProperties.Size;
            AppProperties.Location = newAppProperties.Location;
            AppProperties.OldLocation = newAppProperties.OldLocation;
            AppProperties.LockPosition = newAppProperties.LockPosition;
        }

        private void Current_Deactivated(object sender, EventArgs e)
        {
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
                var newColor = new SolidColorBrush(color);
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
            _currentAppSize = AppProperties.AppSize;
            var location = AppProperties == null ?
                System.Drawing.Point.Empty :
                    (AppProperties.LockPosition && AppProperties.OldLocation != System.Drawing.Point.Empty ?
                    AppProperties.OldLocation :
                    AppProperties.Location);

            SetAppSize(_currentAppSize);
            SetLocation(location);
            SetSize(AppProperties.Size);
        }

        private void SendToBack()
        {
            var hWnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }

        private void SaveWindowPosition()
        {
            AppProperties.Location = GetLocation();
            AppProperties.Size = GetSize();
            AppProperties.AppSize = _currentAppSize;

            SaveAppProperties();
        }

        private void ResetWindowPosition()
        {
            AppProperties.LockPosition = false;
            var x = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
            var y = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);
            var loc = new System.Drawing.Point((int)Math.Round(x), (int)Math.Round(y));
            AppProperties.Location = loc;
            AppProperties.Size = GetSize();
            AppProperties.AppSize = AppSize.Large;
            _currentAppSize = AppSize.Large;
            SetAppSize(AppSize.Large);

            SaveAppProperties();

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

        private void SetAppSize(AppSize appSize)
        {
            if (appSize == AppSize.Small)
            {
                // Small size.
                SetSize(new System.Drawing.Size(330, 120));
                AppProperties.AppSize = AppSize.Small;
                SetClockFontSize(135);
            }
            else if (appSize == AppSize.Medium)
            {
                // Medium size.
                SetSize(new System.Drawing.Size(472, 180));
                AppProperties.AppSize = AppSize.Medium;
                SetClockFontSize(203);
            }
            else
            {
                // Large size.
                SetSize(new System.Drawing.Size(630, 240));
                AppProperties.AppSize = AppSize.Large;
                SetClockFontSize(270);
                appSize = AppSize.Large; // Forces letter to be 'L'.
            }
            _currentAppSize = appSize;
            SaveAppProperties();
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
            var alarmDialog = new AlarmDialog("How long do you want the timer for?", "10");

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
            _currentAppSize = AppSize.Small;
            SetAppSize(_currentAppSize);
            SaveWindowPosition();
        }

        private void Menu_SetSizeMedium_Click(object sender, RoutedEventArgs e)
        {
            _currentAppSize = AppSize.Medium;
            SetAppSize(_currentAppSize);
            SaveWindowPosition();
        }

        private void Menu_SetSizeLarge_Click(object sender, RoutedEventArgs e)
        {
            _currentAppSize = AppSize.Large;
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

        private void LockPositionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AppProperties.LockPosition = !AppProperties.LockPosition;
            AppProperties.OldLocation = AppProperties.Location;
            SaveAppProperties();
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (AppProperties != null && AppProperties.LockPosition
                && GetLocation() != AppProperties.Location)
            {
                // In the event the window position is moved while it's locked (thus not the user), then restore the original position.
                RestoreWindowPosition();
            }
        }
    }
}
