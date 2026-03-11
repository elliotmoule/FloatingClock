using System.Windows;

namespace FloatingClock.WPF.Models;

internal class AppState
{
    public Point Location { get; set; }
    public AppSize AppSize { get; set; } = AppSize.Large;
}
