namespace IZEncoder.Common.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

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

        public static string SizeSuffix(long value, int decimalPlaces = 1)
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

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)

                {
                    var child = VisualTreeHelper.GetChild(depObj, i);

                    if (child != null && child is T) yield return (T) child;


                    foreach (var childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
                }
        }

        public static IEnumerable<ValidationError> GetValidationErrors(this DependencyObject depObj)
        {
            foreach (var findVisualChild in depObj.FindVisualChildren<DependencyObject>())
                foreach (var validationError in Validation.GetErrors(findVisualChild))
                    yield return validationError;
        }

        public static bool HasValidationError(this DependencyObject depObj)
        {
            return depObj.GetValidationErrors().Any();
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