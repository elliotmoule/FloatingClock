using FloatingClock.WPF.Models;
using System.Windows;

namespace FloatingClock.WPF.Helpers;

internal static class PositionHelpers
{
    internal static Point GetLocation() => new(Application.Current.MainWindow.Left, Application.Current.MainWindow.Top);

    internal static Size GetSize() => new(Application.Current.MainWindow.Width, Application.Current.MainWindow.Height);

    internal static void SetSize(Size size)
    {
        Application.Current.MainWindow.Width = size.Width;
        Application.Current.MainWindow.Height = size.Height;
    }

    internal static void SetLocation(Point point)
    {
        Application.Current.MainWindow.Left = point.X;
        Application.Current.MainWindow.Top = point.Y;
    }

    internal static void SetAppSize(AppSize appSize)
    {
        var size = appSize switch
        {
            AppSize.Medium => new Size(472, 180),
            AppSize.Large => new Size(630, 240),
            _ => new Size(330, 120),
        };
        SetSize(size);
    }
}
