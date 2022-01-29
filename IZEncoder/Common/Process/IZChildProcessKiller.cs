namespace IZEncoder.Common.Process
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;

    // ReSharper disable once InconsistentNaming
    public class IZChildProcessKiller : IDisposable
    {
        private readonly List<int> _childPids = new List<int>();
        private readonly string _killerClientExe;

        public IZChildProcessKiller(string killerClientExe = null)
        {
            _killerClientExe = Path.GetFullPath(killerClientExe ?? Assembly.GetCallingAssembly().Location);
            CreateProcess();
        }

        public IZProcess Process { get; private set; }

        public int Count => _childPids.Count;

        public void Dispose()
        {
            Process.Process.Exited -= Process_Exited;
            Process.Process.StandardInput.Write("exit$");
            Process.Process.WaitForExit();
        }

        private void CreateProcess()
        {
            Process = new IZProcess(_killerClientExe, $"--type=background-process-killer --main-process-id={System.Diagnostics.Process.GetCurrentProcess().Id}");
            Process.Process.Exited += Process_Exited;
            Process.OutputRead += Process_OutputRead;
            Process.Start();
        }

        private void Process_OutputRead(object sender, OutputData e)
        {
            Console.WriteLine(e.Text);
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            Process = null;
            CreateProcess();
            foreach (var childPid in _childPids)
                AddChild(childPid, true);
        }

        public void AddChild(Process p)
        {
            AddChild(p.Id);
            p.EnableRaisingEvents = true;
            p.Exited += P_Exited;
        }

        public void AddChild(int pid)
        {
            AddChild(pid, false);
        }

        private void AddChild(int pid, bool force)
        {
            var exist = _childPids.Contains(pid);
            if (exist && !force)
                return;

            while (!IsAlive())
                Thread.Sleep(1);

            Process.Process.StandardInput.Write($"add {pid}$");

            if (!exist)
                _childPids.Add(pid);
        }

        public void RemoveChild(Process p)
        {
            RemoveChild(p.Id);
        }

        public void RemoveChild(int pid)
        {
            if (!_childPids.Contains(pid))
                return;

            while (!IsAlive())
                Thread.Sleep(1);

            Process.Process.StandardInput.Write($"remove {pid}$");
            _childPids.Remove(pid);
        }

        public void ClearChild()
        {
            if (_childPids.Count <= 0) return;

            Process.Process.StandardInput.Write("clear$");
            _childPids.Clear();
        }

        private void P_Exited(object sender, EventArgs e)
        {
            RemoveChild(((Process) sender).Id);
        }

        public bool IsAlive()
        {
            try
            {
                return Process?.Process != null && !Process.Process.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }
}