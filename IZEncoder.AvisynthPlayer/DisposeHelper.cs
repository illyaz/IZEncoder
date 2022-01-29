namespace IZEncoder.AvisynthPlayer
{
    using System;

    public static class DisposeHelper
    {
        public static void DisposeAndNull<T>(ref T obj)
            where T : IDisposable
        {
            obj?.Dispose();
            obj = default(T);
        }
    }
}