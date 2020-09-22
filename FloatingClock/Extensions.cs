using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FloatingClock
{
    public static class Extensions
    {
        public static Point ToPoint(this Position position)
        {
            return new Point(position.X, position.Y);
        }
    }

    public class Position
    {
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;

        public static Position Empty
        {
            get
            {                
                return new Position(0, 0);
            }
        }

        public Position(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
