using FloatingClock.WPF.Models;
using FloatingClock.WPF.Utilities;
using FloatingClock.WPF.ViewModels;
using FloatingClock.WPF.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FloatingClock.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _mainViewModel;
    private Color _borderHighlightColor = Colors.LawnGreen;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainWindow()
    {
        InitializeComponent();
        _mainViewModel = new MainViewModel();
        DataContext = _mainViewModel;

        Loaded += MainWindow_Loaded;

        _mainViewModel.MinuteReached += MinuteReached;
        _mainViewModel.HourReached += HourReached;
    }

    private void HourReached()
    {
        Task.Factory.StartNew(async () =>
        {
            SetTimeForeground(Colors.Blue);
            await Task.Delay(TimeSpan.FromSeconds(1));
            SetTimeForeground(Colors.White);
        });
    }

    private void MinuteReached()
    {
        Task.Factory.StartNew(async () =>
        {
            SetTimeForeground(Colors.LightYellow);
            await Task.Delay(TimeSpan.FromSeconds(1));
            SetTimeForeground(Colors.White);
        });
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SetIcon();
        _mainViewModel.Load();
        SetLabelFontSizes(false);
    }

    private void SetIcon()
    {
        var resources = ResourceHelper.GetAllResourceNames();
        var clockImageStream = ResourceHelper.GetManifestResourceStream(resources.FirstOrDefault(x => x.Contains("clock.png")));
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = clockImageStream;
        bitmap.EndInit();

        this.Icon = bitmap;
    }

    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        SetLabelFontSizes(true);
        lblDate.Visibility = Visibility.Visible;
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        SetLabelFontSizes(false);
        lblDate.Visibility = Visibility.Collapsed;
    }

    private void SetLabelFontSizes(bool enter)
    {
        double lblTimeFontSize;
        double lblDateFontSize;
        double lblTimeBottomMargin;

        switch (_mainViewModel.State.AppSize)
        {
            case AppSize.Medium:
                lblTimeFontSize = enter ? TimeFontSizeSettings.MediumAppSizeTimeHoverFont : TimeFontSizeSettings.MediumAppSizeTimeFont;
                lblDateFontSize = TimeFontSizeSettings.MediumAppSizeDateFont;
                lblTimeBottomMargin = 20;
                break;
            case AppSize.Large:
                lblTimeFontSize = enter ? TimeFontSizeSettings.LargeAppSizeTimeHoverFont : TimeFontSizeSettings.LargeAppSizeTimeFont;
                lblDateFontSize = TimeFontSizeSettings.LargeAppSizeDateFont;
                lblTimeBottomMargin = 25;
                break;
            case AppSize.Small:
            default:
                lblTimeFontSize = enter ? TimeFontSizeSettings.SmallAppSizeTimeHoverFont : TimeFontSizeSettings.SmallAppSizeTimeFont;
                lblDateFontSize = TimeFontSizeSettings.SmallAppSizeDateFont;
                lblTimeBottomMargin = 5;
                break;
        }

        lblTime.FontSize = lblTimeFontSize;
        lblDate.FontSize = lblDateFontSize;
        lblTime.Margin = new Thickness(0, 0, 0, enter ? lblTimeBottomMargin : 0);
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        _border.BorderBrush = Brushes.Transparent;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _mainViewModel.Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift)
           || key != Key.Escape)
        {
            return;
        }

        _mainViewModel.ResetLocation();
    }

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        _mainViewModel.UpdateLocation();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1)
        {
            _mainViewModel.Close();
            Close();
            return;
        }

        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        _border.BorderBrush = new SolidColorBrush(_borderHighlightColor);
        DragMove();

        if (e.LeftButton != MouseButtonState.Released)
        {
            return;
        }

        _border.BorderBrush = Brushes.Transparent;
    }

    private void Menu_SetTimer_Click(object sender, RoutedEventArgs e)
    {
        var messageResult = new MessageViewModel("Input Timer Length", "Provide a timer length. The timer will countdown from this number.", isInput: true);
        var userSetTime = new MessageView(messageResult).ShowDialog();

        if (userSetTime != true
            || !int.TryParse(messageResult.SelectedHour, out var hour)
            || !int.TryParse(messageResult.SelectedMinute, out var minute)
            || !int.TryParse(messageResult.SelectedSeconds, out var second))
        {
            new MessageView(new MessageViewModel("Timer Failure", "Failed to parse selected hours and minutes.", false)).ShowDialog();
            return;
        }

        _cancellationTokenSource = new();
        _mainViewModel.TimeKeeper.AlarmFinished += TimeKeeper_AlarmFinished;
        _mainViewModel.TimeKeeper.CreateAlarm(new TimeSpan(hour, minute, second));
        lblCountdown.Visibility = Visibility.Visible;
    }

    private void TimeKeeper_AlarmFinished(object? sender, EventArgs e)
    {
        _cancellationTokenSource ??= new CancellationTokenSource();
        _mainViewModel.TimeKeeper.AlarmFinished -= TimeKeeper_AlarmFinished;
        lblCountdown.Visibility = Visibility.Collapsed;
        Task.Factory.StartNew(async () =>
        {
            SetTimeForeground(Colors.Red);
            for (int i = 0; i < 4; i++)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
                SetTimeForeground(i % 2 == 0 ? Colors.Red : Colors.DarkRed);
                await AudioPlayer.Play("alarm");
                await Task.Delay(TimeSpan.FromSeconds(1.2));
            }

            SetTimeForeground(Colors.White);
        }, _cancellationTokenSource.Token);
    }

    private void SetTimeForeground(Color color)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            lblTime.Foreground = new SolidColorBrush(color);
            lblDate.Foreground = new SolidColorBrush(color);
        });
    }

    private void Menu_CancelTimer_Click(object sender, RoutedEventArgs e)
    {
        _mainViewModel.TimeKeeper.StopAlarm();
        lblCountdown.Visibility = Visibility.Collapsed;
    }

    private void Menu_SetSize_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem)
        {
            return;
        }

        var split = menuItem.Name.Split('_', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (split.Length < 3 || !Enum.TryParse<AppSize>(split.Last(), out var appSize))
        {
            return;
        }

        _mainViewModel.UpdateSize(appSize);
        SetLabelFontSizes(true);
    }

    private void Menu_ResetPosition_Click(object sender, RoutedEventArgs e)
    {
        _mainViewModel.ResetLocation();
    }
}