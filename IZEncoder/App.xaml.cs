namespace IZEncoder
{
    using System.Threading;
    using System.Windows;
    using Common.Helper;

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        internal static Mutex Mutex;
        internal static Mutex ChildKillerMutex;
        internal static bool IsBypassLauncher;


        internal static int MessageBringToFront = User32Helper.RegisterWindowMessage("izencoder.recv.bringtofront");
    }
}