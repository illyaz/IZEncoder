namespace IZEncoder.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Anotar.Log4Net;
    using IZEncoderNative.Avisynth;
    using Caliburn.Micro;
    using FFmpegEncoder;
    using Helper;
    using JetBrains.Annotations;
    using LiteDB;
    using Process;
    using Project;
    using FileMode = System.IO.FileMode;

    public struct EncodingQueueProgress
    {
        public EncodingQueue Queue { get; set; }
        public int Frame { get; set; }
        public int FrameCount { get; set; }
        public double Bitrate { get; set; }
        public long Size { get; set; }
        public double Fps { get; set; }
        public double Speed { get; set; }
        public TimeSpan Time { get; set; }
        public TimeSpan Elapsed { get; set; }
        public double Percent { get; set; }
    }

    public class EncodingQueueErrorArgs
    {
        public EncodingQueue Queue { get; set; }
        public bool Retry { get; set; }
    }

    public class EncodingQueueProcessor : PropertyChangedBase, IDisposable
    {
        private readonly IZChildProcessKiller _childProcessKiller;
        private readonly Config _config;
        private readonly LiteDatabase _db;
        private readonly LiteCollection<EncodingQueue> _dbCol;
        private readonly Stopwatch _elapsedStopwatch;
        private readonly string _queueFile;

        public EncodingQueueProcessor([NotNull] string queueFile, [NotNull] Config config,
            IZChildProcessKiller childProcessKiller = null)
        {
            _queueFile = queueFile;
            _config = config;
            _childProcessKiller = childProcessKiller;
            _db = new LiteDatabase(_queueFile);
            _dbCol = _db.GetCollection<EncodingQueue>();
            _elapsedStopwatch = new Stopwatch();
            OnQueueCompleted += EncodingQueueProcessor_OnQueueCompleted;
            QueueCollection.AddRange(_dbCol.FindAll());
            foreach (var encodingQueue in QueueCollection)
            {
                if (encodingQueue.Status == EncodingQueueStatus.Processing)
                    Upsert(encodingQueue, EncodingQueueStatus.Retry, "Host exited");

                try
                {
                    encodingQueue.LoadProject();
                }
                catch (Exception e)
                {
                    LogTo.WarnException($"[{encodingQueue.Id}] Queue project load failed", e);
                }
            }
        }

        public bool IsRunning { get; private set; }
        public EncodingQueue CurrentQueue { get; set; }
        public FFmpegProcess CurrentProcess { get; set; }
        public Exception CurrentException { get; set; }
        public bool IsSuspended { get; set; }
        public int RemainingQueue { get; private set; }

        public BindableCollection<EncodingQueue> QueueCollection { get; set; }
            = new BindableCollection<EncodingQueue>();

        public void Dispose()
        {
            if (IsRunning)
                Abort("internal_dispose");

            _db?.Dispose();
        }

        private void EncodingQueueProcessor_OnQueueCompleted(object sender, EncodingQueue q)
        {
            //if (Directory.Exists(q.GetWorkingPath()) &&
            //    (q.Status == EncodingQueueStatus.Completed || q.Status == EncodingQueueStatus.Aborted))
            //    Directory.Delete(q.GetWorkingPath(), true);
        }

        public event EventHandler<EncodingQueueProgress> OnProgressChanged;
        public event EventHandler<EncodingQueue> OnQueueStarted;
        public event EventHandler<EncodingQueueErrorArgs> OnQueueError;
        public event EventHandler<EncodingQueue> OnQueueCompleted;
        public event EventHandler OnStarted;
        public event EventHandler OnCompleted;

        [LogToWarnOnException]
        public bool HasUnfinishQueue()
        {
            return _dbCol.FindAll().Any(x =>
                x.Status == EncodingQueueStatus.Waiting || x.Status == EncodingQueueStatus.Processing ||
                x.Status == EncodingQueueStatus.Retry);
        }

        [LogToWarnOnException]
        public void SuspendProcess()
        {
            if (CurrentProcess == null || CurrentProcess.HasExited)
                throw new InvalidOperationException("No active encoding process");

            _elapsedStopwatch.Stop();
            CurrentProcess.Suspend();
            IsSuspended = true;
        }

        [LogToWarnOnException]
        public void ResumeProcess()
        {
            if (CurrentProcess == null || CurrentProcess.HasExited)
                throw new InvalidOperationException("No active encoding process");

            _elapsedStopwatch.Start();
            CurrentProcess.Resume();
            IsSuspended = false;
        }

        [LogToWarnOnException]
        private void ProcessingQueue([NotNull] EncodingQueue q)
        {
            Upsert(q, EncodingQueueStatus.Processing, "queue_processor");
            CurrentQueue = q;
            CurrentProcess = null;

            using (var logStream = new FileStream(q.GetEncoderOutputLogPath(q.CommandIndex), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.Read))
            {
                using (var logWriter = new StreamWriter(logStream, Encoding.UTF8))
                {
                    if (logStream.Length > 0)
                        logStream.SetLength(0); // Reset

                    CurrentProcess = new FFmpegProcess(Path.GetFullPath(_config.Application.FFmpegPath),
                        q.CompileCommand(q.CommandIndex), q.GetWorkingPath());

                    CurrentProcess.Process.ErrorRead += (sender, data) =>
                    {
                        logWriter.WriteLine(data.Text);
                        logWriter.Flush();
                    };

                    CurrentProcess.ProgressChanged += (sender, data) =>
                    {
                        var progress = new EncodingQueueProgress
                        {
                            Time = data.Time,
                            Bitrate = data.BitRate,
                            Elapsed = _elapsedStopwatch.Elapsed,
                            Fps = data.Fps,
                            Frame = data.Frame,
                            FrameCount = q.AvisynthInfo.Frames,
                            Percent = (double)data.Frame / q.AvisynthInfo.Frames * 100,
                            Queue = q,
                            Size = data.TotalSize,
                            Speed = data.Speed
                        };

                        q.Processing = progress.Percent;
                        OnProgressChanged?.Invoke(this, progress);
                    };

                    _elapsedStopwatch.Restart();
                    CurrentProcess.Start();
                    _childProcessKiller?.AddChild(CurrentProcess.Process);

                    OnQueueStarted?.Invoke(this, q);
                    CurrentProcess.NativeProcess.WaitForExit();
                    _elapsedStopwatch.Stop();
                    q.ExitCode = CurrentProcess.NativeProcess.ExitCode;

                    if (q.Status != EncodingQueueStatus.Aborted)
                    {
                        if (CurrentProcess.NativeProcess.ExitCode == 0)
                        {
                            Upsert(CurrentQueue, EncodingQueueStatus.Completed);
                        }
                        else
                        {
                            var errObj = new EncodingQueueErrorArgs { Queue = q };
                            OnQueueError?.Invoke(this, errObj);
                            Upsert(CurrentQueue,
                                errObj.Retry ? EncodingQueueStatus.Retry : EncodingQueueStatus.Error,
                                $"process exited with code: 0x{CurrentProcess.NativeProcess.ExitCode:x}");
                        }
                    }

                    OnQueueCompleted?.Invoke(this, q);
                }
            }
        }

        [LogToWarnOnException]
        public void Start()
        {
            Start(null);
        }

        [LogToWarnOnException]
        public void Start(DateTime? fromDate)
        {
            if (IsRunning)
                throw new InvalidOperationException("queue processor has already started");

            IsRunning = true;

            OnStarted?.Invoke(this, EventArgs.Empty);

            try
            {
                while (IsRunning)
                {
                    EncodingQueue q;

                    var unfinishQueues = QueueCollection.Where(x =>
                        x.Status == EncodingQueueStatus.Waiting || x.Status == EncodingQueueStatus.Processing ||
                        x.Status == EncodingQueueStatus.Retry).ToList();

                    if (fromDate != null)
                        q = unfinishQueues.FirstOrDefault(x => x.CreatedOn >= fromDate) ??
                            unfinishQueues.FirstOrDefault();
                    else
                        q = unfinishQueues.FirstOrDefault();

                    if (q == null)
                        break;

                    IsSuspended = false;
                    try
                    {
                        ProcessingQueue(q);
                        if (q.Status == EncodingQueueStatus.Aborted)
                            IsRunning = false;
                    }
                    catch (Exception e)
                    {
                        q.ExitCode = null;
                        CurrentException = e;
                        var errObj = new EncodingQueueErrorArgs { Queue = q };

                        OnQueueError?.Invoke(this, errObj);
                        Upsert(CurrentQueue,
                            errObj.Retry ? EncodingQueueStatus.Retry : EncodingQueueStatus.Error,
                            $"Internal Exception: {e.Message}");
                    }
                }
            }
            finally
            {
                OnCompleted?.Invoke(this, EventArgs.Empty);
            }


            IsRunning = false;
        }

        [LogToWarnOnException]
        public void Abort(string reason = "user_abort")
        {
            if (!IsRunning)
                throw new InvalidOperationException("queue processor not started");

            Upsert(CurrentQueue, EncodingQueueStatus.Aborted, reason);
            CurrentProcess.NativeProcess.StandardInput.Write('q'); // send quit to ffmpeg process

            while (IsRunning)
                Thread.Sleep(1);
        }

        [LogToWarnOnException]
        private EncodingQueue GetQueue(params EncodingQueueStatus[] status)
        {
            return QueueCollection.FirstOrDefault(x => status.Any(s => s == x.Status));
        }

        [LogToWarnOnException]
        private IEnumerable<EncodingQueue> GetQueues(params EncodingQueueStatus[] status)
        {
            return QueueCollection.Where(x => status.Any(s => s == x.Status));
        }

        [LogToWarnOnException]
        private int CountRemainingQueue()
        {
            return QueueCollection.Count(x =>
                x.Status == EncodingQueueStatus.Waiting || x.Status == EncodingQueueStatus.Processing ||
                x.Status == EncodingQueueStatus.Retry);
        }

        public EncodingQueue Create(AvisynthProject project, ICollection<FFmpegParameter> ffmpegParameters,
            ICollection<AvisynthFilter.AvisynthFilter> avisynthFilters)
        {
            if (string.IsNullOrEmpty(project.Input?.Filename))
                throw new Exception("Please specify project input");

            if (!File.Exists(project.Input.Filename))
                throw new Exception("Project input not exists");

            if (string.IsNullOrEmpty(project.Output))
                throw new Exception("Please specify project output");

            var q = new EncodingQueue();
            Upsert(q, EncodingQueueStatus.Creating);

            try
            {
                q.WorkingPath = Path.GetFullPath(Path.Combine(_config.Application.TempPath, q.Id.ToString())
                    .EnsureDirectoryExists());

                // Write Project File
                project.Save(q.GetProejctPath());

                // Write Script File
                File.WriteAllText(q.GetScriptPath(),
                    project.ToScript(_config, avisynthFilters));

                using (var bridge = new AvisynthBridge())
                {
                    q.AvisynthInfo = bridge.CreateClip("src", "Import", q.GetScriptPath()).Info.DeepCopy();
                }

                var ve = ffmpegParameters.Find<FFmpegVideoParameter>(project.VideoEncoder, true);
                var ae = ffmpegParameters.Find<FFmpegAudioParameter>(project.AudioEncoder, true);
                var ce = ffmpegParameters.Find<FFmpegContainerParameter>(project.Container, true);

                if (ve == null)
                    throw new Exception($"Could not find video encoder: {project.VideoEncoder}");

                if (!ve.IsAvailable)
                    throw new Exception($"Video codec '{ve.Codec}' is not available");

                if (ae == null)
                    throw new Exception($"Could not find audio encoder: {project.AudioEncoder}");

                if (!ae.IsAvailable)
                    throw new Exception($"Audio codec '{ae.Codec}' is not available");

                if (ce == null)
                    throw new Exception($"Could not find container: {project.Container}");

                if (!ce.IsAvailable)
                    throw new Exception($"Container format '{ce.Format}' is not available");

                for (var i = 0; i < ve.Commands.Count; i++)
                {
                    var nulOutput = ve.Commands.Count != 1 && i != ve.Commands.Count - 1;
                    var command = @"-y -i ""{ScriptPath}""";

                    command += " " + ve.Build(project.VideoEncoderSettings, i);

                    if (q.AvisynthInfo.HasAudio())
                        command += " " + ae.Build(project.AudioEncoderSettings, 0);
                    else
                        command += " " + "-an";

                    command += " " + ce.Build(project.AudioEncoderSettings, 0);
                    command += " " + (nulOutput ? "NUL" : @"""{Project.Output}""");

                    q.GeneratedCommands.Add(command);
                }

                q.LoadProject();
                Upsert(q, EncodingQueueStatus.Waiting, "queue_creation");
            }
            catch
            {
                Delete(q);
                throw;
            }

            return q;
        }


        [LogToWarnOnException]
        private void Upsert(EncodingQueue q, EncodingQueueStatus status, string reason = null)
        {
            q.Reason = string.IsNullOrEmpty(reason?.Trim()) ? null : reason.Trim();

            switch (status)
            {
                case EncodingQueueStatus.Creating:
                    q.CreatedOn = DateTime.Now;
                    q.Status = EncodingQueueStatus.Creating;
                    _dbCol.Upsert(q);
                    break;
                case EncodingQueueStatus.Waiting:
                    q.Status = EncodingQueueStatus.Waiting;
                    _dbCol.Upsert(q);
                    break;
                case EncodingQueueStatus.Processing:
                    q.LastRun = DateTime.Now;
                    q.Status = EncodingQueueStatus.Processing;
                    _dbCol.Upsert(q);
                    break;
                case EncodingQueueStatus.Retry:
                    q.Status = EncodingQueueStatus.Retry;
                    _dbCol.Upsert(q);
                    break;
                case EncodingQueueStatus.Completed:
                    if (q.CommandIndex + 1 >= q.GeneratedCommands.Count)
                    {
                        q.Status = EncodingQueueStatus.Completed;
                        q.CompletedOn = DateTime.Now;
                    }
                    else
                    {
                        q.Status = EncodingQueueStatus.Processing;
                        q.CommandIndex++;
                    }

                    _dbCol.Upsert(q);
                    break;
                case EncodingQueueStatus.Aborted:
                    q.Status = EncodingQueueStatus.Aborted;
                    _dbCol.Upsert(q);
                    break;
                case EncodingQueueStatus.Error:
                    q.Status = EncodingQueueStatus.Error;
                    _dbCol.Upsert(q);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }

            RemainingQueue = CountRemainingQueue();

            if (QueueCollection.FirstOrDefault(x => x.Id == q.Id) == null)
                QueueCollection.Add(q);
        }

        [LogToWarnOnException]
        public void Delete(EncodingQueue q)
        {
            if (Directory.Exists(q.GetWorkingPath()))
                Directory.Delete(q.GetWorkingPath(), true);

            _dbCol.Delete(q.Id);
            QueueCollection.Remove(QueueCollection.FirstOrDefault(x => x.Id == q.Id));
        }
    }
}