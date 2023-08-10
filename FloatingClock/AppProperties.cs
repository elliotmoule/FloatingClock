using System.Drawing;

namespace FloatingClock
{
    internal class AppProperties
    {
        public Point OldLocation { get; set; } = Point.Empty;
        public Point Location { get; set; } = Point.Empty;
        public Size Size { get; set; } = new Size(630, 240);
        public AppSize AppSize { get; set; } = AppSize.Large;
        public bool LockPosition { get; set; } = false;
    }
}
