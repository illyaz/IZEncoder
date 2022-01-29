namespace IZEncoder.Common.Process
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    // ReSharper disable once InconsistentNaming
    public class IZProcess
    {
        [Flags]
        public enum ThreadAccess
        {
            Terminate = 0x0001,
            SuspendResume = 0x0002,
            GetContext = 0x0008,
            SetContext = 0x0010,
            SetInformation = 0x0020,
            QueryInformation = 0x0040,
            SetThreadToken = 0x0080,
            Impersonate = 0x0100,
            DirectImpersonation = 0x0200
        }

        private CancellationTokenSource _asyncStreamCancellationToken;
        private string _instanceName;

        private PerformanceCounter _pcProcessCpu;
        private PerformanceCounter _pcProcessMem;
        private bool _processClosed;
        private bool _processExited;
        private bool _processStarted;

        public IZProcess(string filename, string arguments, string workingDir = null)
        {
            Process = new Process {StartInfo = new ProcessStartInfo(filename, arguments)};
            Process.StartInfo.RedirectStandardInput = true;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.RedirectStandardError = true;
            Process.StartInfo.CreateNoWindow = true;
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            Process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            Process.StartInfo.WorkingDirectory =
                string.IsNullOrEmpty(workingDir) ? Environment.CurrentDirectory : workingDir;
        }

        public bool HasExited => Process?.HasExited ?? true;
        public bool IsSuspended { get; private set; }
        public Process Process { get; }

        [DllImport("kernel32")]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32")]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32")]
        private static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        public event EventHandler<OutputData> OutputRead;
        public event EventHandler<OutputData> ErrorRead;

        public void Start()
        {
            Process.EnableRaisingEvents = true;
            Process.Exited += Process_Exited;
            Process.Start();
            _processStarted = true;

            //_instanceName = GetProcessInstanceName(Process);
            //_pcProcessCpu = new PerformanceCounter("Process", "% Processor Time", _instanceName, true);
            //_pcProcessMem = new PerformanceCounter("Process", "Working Set - Private", _instanceName, true);

            _asyncStreamCancellationToken = new CancellationTokenSource();

            BeginReadOutputStream();
            BeginReadErrorStream();
        }

        public void Suspend()
        {
            if (IsSuspended)
                return;

            if (Process == null || Process.HasExited)
                throw new InvalidOperationException();

            foreach (ProcessThread pT in Process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SuspendResume, false, (uint) pT.Id);

                if (pOpenThread == IntPtr.Zero)
                    continue;

                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }

            IsSuspended = true;
        }

        public void Resume()
        {
            if (!IsSuspended)
                return;

            if (Process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in Process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SuspendResume, false, (uint) pT.Id);

                if (pOpenThread == IntPtr.Zero)
                    continue;

                int suspendCount;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }

            IsSuspended = false;
        }

        public void Kill()
        {
            if (!_processClosed && !Process.HasExited)
                Process.Kill();
        }

        public void KillWait()
        {
            Kill();
            while (!_processExited)
                Thread.Sleep(1);
        }

        public void Close()
        {
            if (Process.HasExited)
                Process.Close();
            else
                throw new InvalidOperationException("Cannot close running process");

            _processClosed = true;
        }

        public double GetCPU_Usage()
        {
            if (!_processStarted || _processExited || _processClosed)
                return 0;
            try
            {
                if (_pcProcessCpu == null)
                    throw new InvalidOperationException();

                return _pcProcessCpu.NextValue() / Environment.ProcessorCount;
            }
            catch (InvalidOperationException)
            {
                if (_instanceName != null && new PerformanceCounterCategory("Process").InstanceExists(_instanceName))
                    throw;

                _instanceName = GetProcessInstanceName(Process);

                if (_instanceName == null)
                    return 0;

                _pcProcessCpu = new PerformanceCounter("Process", "% Processor Time", _instanceName, true);
                return GetCPU_Usage();
            }
        }

        public double GetMemory_Usage()
        {
            if (!_processStarted || _processExited || _processClosed)
                return 0;

            try
            {
                if (_pcProcessMem == null)
                    throw new InvalidOperationException();

                return _pcProcessMem.NextValue();
            }
            catch (InvalidOperationException)
            {
                if (_instanceName != null && new PerformanceCounterCategory("Process").InstanceExists(_instanceName))
                    throw;

                _instanceName = GetProcessInstanceName(Process);

                if (_instanceName == null)
                    return 0;

                _pcProcessMem = new PerformanceCounter("Process", "Working Set - Private", _instanceName, true);
                return GetMemory_Usage();
            }
        }

        private async void BeginReadOutputStream()
        {
            var buf = new char[1];
            var od = new OutputData();

            while (true)
            {
                if (_asyncStreamCancellationToken.IsCancellationRequested)
                    break;

                if (!(await Process.StandardOutput.ReadAsync(buf, 0, buf.Length) > 0))
                    break;

                if (buf[0] == '\r')
                {
                    od.IsCarriage = true;
                }
                else if (buf[0] == '\n')
                {
                    od.IsLineFeed = true;
                }
                else if (od.IsCarriage || od.IsLineFeed)
                {
                    OutputRead?.Invoke(this, od);
                    od = new OutputData();
                    od.Text += buf[0];
                }
                else
                {
                    od.Text += buf[0];
                }
            }
        }

        private async void BeginReadErrorStream()
        {
            var buf = new char[1];
            var od = new OutputData();

            while (true)
            {
                if (_asyncStreamCancellationToken.IsCancellationRequested)
                    break;

                if (!(await Process.StandardError.ReadAsync(buf, 0, buf.Length) > 0))
                    break;

                if (buf[0] == '\r')
                {
                    od.IsCarriage = true;
                }
                else if (buf[0] == '\n')
                {
                    od.IsLineFeed = true;
                }
                else if (od.IsCarriage || od.IsLineFeed)
                {
                    ErrorRead?.Invoke(this, od);
                    od = new OutputData();
                    od.Text += buf[0];
                }
                else
                {
                    od.Text += buf[0];
                }
            }
        }

        #region Private Methods

        private void Process_Exited(object sender, EventArgs e)
        {
            _asyncStreamCancellationToken?.Cancel();
            _processExited = true;
        }

        private static string GetProcessInstanceName(Process process)
        {
            try
            {
                var processName = Path.GetFileNameWithoutExtension(process.ProcessName);

                var cat = new PerformanceCounterCategory("Process");
                var instances = cat.GetInstanceNames()
                    .Where(inst => inst.StartsWith(processName))
                    .ToArray();

                foreach (var instance in instances)
                    using (var cnt = new PerformanceCounter("Process",
                        "ID Process", instance, true))
                    {
                        var val = (int) cnt.RawValue;
                        if (val == process.Id) return instance;
                    }
            }
            catch (InvalidOperationException) { }

            return null;
        }

        public static implicit operator Process(IZProcess a)
        {
            return a.Process;
        }

        #endregion
    }

    public struct OutputData
    {
        public bool IsCarriage { get; set; }
        public bool IsLineFeed { get; set; }
        public string Text { get; set; }
    }
}