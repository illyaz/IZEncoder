namespace IZEncoder.Common.Helper
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    internal static class LauncherHelper
    {
        private static bool _isReady;
        private static bool _isBypass;
        private static bool _isVaild;
        internal static string Id;
        internal static IntPtr Handle;
        internal static int ReadyMessageId;
        internal static IntPtr ReadyMessageWParam;
        internal static IntPtr ReadyMessageLParam;
        internal static int StatusCallbackMessageId;
        internal static int ExceptionCallbackMessageId;
        internal static string CallbackMapName;

        private static MemoryMappedFile _callbackMappedFile;
        private static MemoryMappedViewAccessor _callbackMappedAccessor;
        private static EventWaitHandle _callbackWaitHandle;
        static LauncherHelper()
        {
            var args = EnvironmentHelper.GetCommandLineArgs();

            if (args.ContainsKey("launcher") &&
                TryParseArgs(args["launcher"], out Id, out var hwnd, out ReadyMessageId, out var wp, out var lp, out StatusCallbackMessageId, out ExceptionCallbackMessageId, out var mapSize, out CallbackMapName))
            {
                Handle = new IntPtr(hwnd);
                ReadyMessageWParam = new IntPtr(wp);
                ReadyMessageLParam = new IntPtr(lp);

                var sb = new StringBuilder(255);
                User32Helper.GetWindowText(Handle, sb, sb.Capacity);
                if (new string(sb.ToString().Reverse().ToArray()).Equals(Id))
                {
                    try
                    {
                        _callbackMappedFile =
                            MemoryMappedFile.OpenExisting(CallbackMapName, MemoryMappedFileRights.Write);
                        _callbackMappedAccessor = _callbackMappedFile.CreateViewAccessor(0, mapSize);
                        _callbackWaitHandle =
                            new EventWaitHandle(false, EventResetMode.ManualReset, $"{CallbackMapName}WH");

                        _isVaild = true;
                    }
                    catch
                    {
                        _callbackMappedAccessor?.Dispose();
                        _callbackMappedFile?.Dispose();
                        _callbackWaitHandle?.Dispose();
                    }
                }
            }

            if (Debugger.IsAttached || args.ContainsKey("force"))
                _isBypass = true;
        }

        internal static void Ready()
        {
            CheckAccess();
            if (_isReady)
                return;

            _isReady = true;
            _callbackMappedAccessor?.Dispose();
            _callbackMappedFile?.Dispose();
            _callbackWaitHandle?.Dispose();
            User32Helper.PostMessage(Handle, ReadyMessageId, ReadyMessageWParam, ReadyMessageLParam);
        }

        internal static void SendMessage(string message, int timeout = 15)
        {
            CheckAccess();
            var messageData = Encoding.UTF8.GetBytes(message);
            _callbackMappedAccessor.WriteArray(0, messageData, 0, messageData.Length);
            User32Helper.PostMessage(Handle, StatusCallbackMessageId, IntPtr.Zero, new IntPtr(messageData.Length));
            _callbackWaitHandle.WaitOne();
        }

        internal static bool TrySendMessage(string message, int timeout = 15)
        {
            try
            {
                SendMessage(message, timeout);
                return true;
            }
            catch { /* ignored */ }

            return false;
        }

        internal static void SendException(Exception e, string title = null, int timeout = 15)
        {
            CheckAccess();
            var messageData = Encoding.UTF8.GetBytes($"{title ?? e.Message}|--|Message: {e.Message}\nStackTrace:\n{e.StackTrace}");
            _callbackMappedAccessor.WriteArray(0, messageData, 0, messageData.Length);
            User32Helper.PostMessage(Handle, ExceptionCallbackMessageId, IntPtr.Zero, new IntPtr(messageData.Length));
            _callbackWaitHandle.WaitOne();
        }

        internal static bool TrySendException(Exception e, string title = null, int timeout = 15)
        {
            try
            {
                SendException(e, title, timeout);
                return true;
            }
            catch { /* ignored */ }

            return false;
        }

        internal static void StartLauncher()
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "IZEncoder.Launcher.exe");

            if (File.Exists(path))
                Process.Start(
                    new ProcessStartInfo(path, "--redirect") {WorkingDirectory = Path.GetDirectoryName(path)});
        }

        internal static void StartLauncherAndExit()
        {
            StartLauncher();
            Environment.Exit(0);
        }
        internal static bool IsValid()
        {
            return _isVaild;
        }

        internal static bool IsBypass()
        {
            return _isBypass;
        }

        internal static bool TryParseArgs(string args, out string id, out long handle, out int msg, out int wp, 
            out int lp, out int callbackId, out int exceptionCallbackId, out int mapSize, out string mapName)
        {
            var sp = args.Split(',');

            if (sp.Length == 9)
            {
                var r0 = Guid.TryParse(sp[0], out _);
                id = sp[0];
                var r1 = long.TryParse(sp[1], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out handle);
                var r2 = int.TryParse(sp[2], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out msg);
                var r3 = int.TryParse(sp[3], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out wp);
                var r4 = int.TryParse(sp[4], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out lp);
                var r5 = int.TryParse(sp[5], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out callbackId);
                var r6 = int.TryParse(sp[6], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out exceptionCallbackId);
                var r7 = int.TryParse(sp[7], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out mapSize);
                mapName = sp[8];
                if (r0 && r1 && r2 && r3 && r4 && r5 && r6 && r7)
                    return true;                
            }

            id = mapName = null;
            handle = msg = wp = lp = callbackId = exceptionCallbackId = mapSize = 0;
            return false;
        }

        private static void CheckAccess()
        {
            if (!_isVaild)
                throw new InvalidOperationException();
        }
    }
}