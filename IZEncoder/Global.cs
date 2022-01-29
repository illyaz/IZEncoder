namespace IZEncoder
{
    using System;
    using System.Diagnostics;
    using System.Media;
    using System.Reflection;
    using System.Windows;
    using Caliburn.Micro;
    using Common;
    using Common.AvisynthFilter;
    using Common.FFmpegEncoder;
    using Common.FontIndexer;
    using Common.Helper;
    using Common.MessageBox;
    using Common.Process;
    using Common.Project;
    using MahApps.Metro.IconPacks;
    using UI.ViewModel;

    public class Global : PropertyChangedBase
    {
        public Version Version { get; set; }

        public Global()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version;
        }

        public Config Config { get; set; }
        public AvisynthProject ActiveProject { get; set; }

        public BindableCollection<AvisynthFilter> AvisynthFilters { get; set; } =
            new BindableCollection<AvisynthFilter>();

        public BindableCollection<FFmpegParameter> FFmpegParameters { get; set; } =
            new BindableCollection<FFmpegParameter>();

        public BindableCollection<EncoderTemplate> FFmpegTemplates { get; set; } =
            new BindableCollection<EncoderTemplate>();

        public BindableCollection<FFmpegEncoder> FFmpegSupportedEncoders { get; set; }
            = new BindableCollection<FFmpegEncoder>();

        public BindableCollection<FFmpegFormat> FFmpegSupportedFormats { get; set; }
            = new BindableCollection<FFmpegFormat>();

        internal  GlobalWin32Message GlobalWin32Message { get; set; }
        internal IZChildProcessKiller ChildProcessKiller { get; set; }
        internal IZFontIndexer FontIndexer { get; set; }
        public EncodingQueueProcessor EncodingQueueProcessor { get; set; }
        public MainWindowViewModel MainWindowVm { get; set; }
        public EncodingProgressViewModel EncodingProgressVm { get; set; }
        public QueueManagementViewModel QueueManagementVm { get; set; }

        internal void LogException(Exception e, string devMessage = null)
        {
            var st = new StackTrace(e, true);
            var frame = st.GetFrame(0);
            var methodName = frame.GetMethod()?.ReflectedType?.FullName ?? "?";
            Console.WriteLine(
                $"Error: {e.Message} at {methodName} [{frame.GetFileName()}:{frame.GetFileLineNumber()}:{frame.GetFileColumnNumber()}]");
        }

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