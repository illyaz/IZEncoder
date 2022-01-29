namespace IZEncoder.Launcher.Common.Helper
{
    using System;
    using System.Runtime.InteropServices;

    internal static unsafe class Unmanaged
    {
        public static void* New<T>(int elementCount)
            where T : struct
        {
            return Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)) *
                                        elementCount).ToPointer();
        }

        public static void* NewAndInit<T>(int elementCount)
            where T : struct
        {
            var newSizeInBytes = Marshal.SizeOf(typeof(T)) * elementCount;
            var newArrayPointer =
                (byte*) Marshal.AllocHGlobal(newSizeInBytes).ToPointer();

            for (var i = 0; i < newSizeInBytes; i++)
                *(newArrayPointer + i) = 0;

            return newArrayPointer;
        }

        public static void Free(void* pointerToUnmanagedMemory)
        {
            Marshal.FreeHGlobal(new IntPtr(pointerToUnmanagedMemory));
        }

        public static void* Resize<T>(void* oldPointer, int newElementCount)
            where T : struct
        {
            return Marshal.ReAllocHGlobal(new IntPtr(oldPointer),
                new IntPtr(Marshal.SizeOf(typeof(T)) * newElementCount)).ToPointer();
        }
    }
}