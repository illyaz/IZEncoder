namespace IZEncoder.Launcher.Common.Helper
{
    using System;
    using System.Linq;
    using System.Windows;

    // ReSharper disable once InconsistentNaming
    public static class UIHelper
    {
        private static readonly string[] SizeSuffixes =
            {"bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};

        public static MoveDirections GetMoveDirection(this Point p1, Point p2)
        {
            if (Math.Abs(p2.X - p1.X) > Math.Abs(p2.Y - p1.Y))
                return p2.X - p1.X < 0 ? MoveDirections.Right : MoveDirections.Left;

            return p2.Y - p1.Y < 0 ? MoveDirections.Up : MoveDirections.Down;
        }

        public static bool IsWindowOpen<T>(this T win) where T : Window
        {
            return Application.Current.Windows.OfType<T>().Any(w => Equals(w, win));
        }

        public static string SizeSuffix(this long value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) throw new ArgumentOutOfRangeException(nameof(decimalPlaces));
            if (value < 0) return "-" + SizeSuffix(-value);
            if (value == 0) return string.Format("{0:n" + decimalPlaces + "} bytes", 0);

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            var mag = (int) Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            var adjustedSize = (decimal) value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
    }

    public enum MoveDirections
    {
        Left,
        Right,
        Up,
        Down
    }
}