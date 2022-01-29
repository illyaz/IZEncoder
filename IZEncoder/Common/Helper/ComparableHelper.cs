namespace IZEncoder.Common.Helper
{
    using System;

    public static class ComparableHelper
    {
        public static bool InRange<T>(this T value, T from, T to) where T : IComparable<T>
        {
            return value.CompareTo(from) >= 0 && value.CompareTo(to) <= 0;
        }
    }
}