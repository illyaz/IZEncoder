namespace IZEncoder.Launcher
{
    using System;
    using System.Linq;
    using System.Media;
    using System.Reflection;
    using System.Windows;
    using Caliburn.Micro;
    using Common.Helper;
    using Common.MessageBox;
    using MahApps.Metro.IconPacks;

    public class Global
    {
        public Version Version { get; set; }

        public Global()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version;
        }

        internal GlobalWin32Message GlobalWin32Message { get; set; }

        internal string ShowMessage(string title = null, string message = null, ViewAware ownerView = null,
            Action<IZMessageBox> extend = null)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                var box = new IZMessageBox
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Title = title
                };

                if (!Equals(Application.Current.MainWindow, box))
                    box.Owner = ownerView?.GetView() as Window ?? Application.Current.MainWindow;
                else
                    box.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                box.AddText(message)
                    .AddButton("OK")
                    .FocusButton()
                    .SetSound(SystemSounds.Exclamation)
                    .SetIcon(PackIconMaterialKind.Alert);

                extend?.Invoke(box);
                box.ShowDialog();

                return box.Result;
            });
        }

        internal string ShowMessage(Exception e, string title = null, ViewAware ownerView = null,
            Action<IZMessageBox> extend = null)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                var box = new IZMessageBox
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Title = title ?? e.GetType().Name
                };

                if (!Equals(Application.Current.MainWindow, box))
                    box.Owner = ownerView?.GetView() as Window ?? Application.Current.MainWindow;
                else
                    box.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                box.AddText($"Message: {e.Message}");
                if (!string.IsNullOrEmpty(e.StackTrace))
                    box.AddLine()
                        .AddText("StackTrace: ")
                        .AddLine()
                        .AddText(e.StackTrace.Replace(@"J:\source\repos\IZEncoderV2", "")
                            .Replace(".cs:line", ":line"));

                box.AddButton("OK")
                    .FocusButton()
                    .SetSound(SystemSounds.Exclamation)
                    .SetIcon(PackIconMaterialKind.Alert);

                extend?.Invoke(box);
                box.ShowDialog();

                return box.Result;
            });
        }
    }
}