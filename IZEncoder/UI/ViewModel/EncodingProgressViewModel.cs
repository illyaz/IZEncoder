namespace IZEncoder.UI.ViewModel
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using Common;
    using Common.MessageBox;
    using View;

    public class EncodingProgressViewModel : IZEScreen<EncodingProgressView>
    {
        private CancellationTokenSource _cts;

        public EncodingProgressViewModel(Global g)
        {
            G = g;
        }

        public DateTime? StartDate { get; set; }
        public Global G { get; }
        public LoadingIndicator LoadingIndicator { get; set; }
        protected override EncodingProgressView View => (Window?.Content ?? GetView()) as EncodingProgressView;
        public double Processing { get; set; }
        public int FrameCount { get; set; }
        public int Frame { get; set; }
        public double CpuUsage { get; set; }
        public string Bitrate { get; set; }
        public string Size { get; set; }
        public string Fps { get; set; }
        public string Speed { get; set; }
        public TimeSpan Time { get; set; }
        private bool isCompleted;
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            LoadingIndicator = new LoadingIndicator(View.Content as Grid, View);

            G.EncodingQueueProcessor.OnStarted += EncodingQueueProcessor_OnStarted;
            G.EncodingQueueProcessor.OnCompleted += EncodingQueueProcessor_OnCompleted;
            G.EncodingQueueProcessor.OnQueueError += EncodingQueueProcessor_OnQueueError;
            G.EncodingQueueProcessor.OnQueueStarted += EncodingQueueProcessorOnQueueStarted;
            G.EncodingQueueProcessor.OnProgressChanged += EncodingQueueProcessor_OnProgressChanged;
            G.EncodingQueueProcessor.OnQueueCompleted += EncodingQueueProcessorOnQueueCompleted;

            _cts = new CancellationTokenSource();

            Task.Factory.StartNew(() => G.EncodingQueueProcessor.Start(StartDate));
            Task.Factory.StartNew(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    CpuUsage = G.EncodingQueueProcessor.IsSuspended || !G.EncodingQueueProcessor.IsRunning
                        ? 0
                        : G.EncodingQueueProcessor.CurrentProcess?.Process?.GetCPU_Usage() ?? 0;
                    try
                    {
                        await Task.Delay(1000, _cts.Token);
                    }
                    catch (TaskCanceledException) { }
                }
            });
        }

        private void EncodingQueueProcessor_OnQueueError(object sender, EncodingQueueErrorArgs e)
        {
            LoadingIndicator.Run(() =>
            {
                switch (G.ShowMessage("Encoding Progress",
                    (e.Queue.ExitCode != null
                        ? $"FFmpeg process exited with code: 0x{e.Queue.ExitCode:x}\n"
                        : $"Internal Error\n{G.EncodingQueueProcessor.CurrentException.Message}\n") +
                    "You want to retry ?", this,
                    ex => ex.ClearButton().AddButton(IZMessageBoxButton.YesNo)))
                {
                    case "Yes":
                        e.Retry = true;
                        break;
                    case "No":
                        e.Retry = false;
                        break;
                }
            }, -1, "Please wait ...").Wait(); // Don't await in event handler
        }

        private void EncodingQueueProcessor_OnStarted(object sender, EventArgs e)
        {
            Console.WriteLine("Queue processor started");
        }

        private void EncodingQueueProcessor_OnCompleted(object sender, EventArgs e)
        {
            Console.WriteLine("Queue processor completed");
            isCompleted = true;
            if (!G.Config.EncodingProcess.AttachToMainWindow)
                TryClose();
        }

        private void EncodingQueueProcessor_OnProgressChanged(object sender, EncodingQueueProgress e)
        {
            FrameCount = e.Queue.AvisynthInfo.Frames;
            Frame = e.Frame;
            Bitrate = e.Bitrate > 0 ? $"{e.Bitrate}kbits/s" : "N/A";
            Size = e.Size > 0 ? $"{BytesToString(e.Size)}" : "N/A";
            Fps = e.Fps > 0 ? $"{e.Fps}" : "N/A";
            Processing = e.Percent;
            Speed = e.Speed > 0 ? $"{e.Speed}x" : "N/A";
        }

        private void EncodingQueueProcessorOnQueueCompleted(object sender, EncodingQueue e)
        {
            Console.WriteLine(
                $"Completed: {e.Id}, {e.Status}: {e.Reason}, rem: {((EncodingQueueProcessor) sender).RemainingQueue}");
        }

        private void EncodingQueueProcessorOnQueueStarted(object sender, EncodingQueue e)
        {
            Console.WriteLine($"Started: {e.Id}, {e.Status}: {e.Reason}");
        }

        public void PauseToggle()
        {
            if (G.EncodingQueueProcessor.IsSuspended)
                G.EncodingQueueProcessor.ResumeProcess();
            else
                G.EncodingQueueProcessor.SuspendProcess();
        }

        public Task Abort()
        {
            return LoadingIndicator.Run(() =>
            {
                var lastSus = G.EncodingQueueProcessor.IsSuspended;
                if (!G.EncodingQueueProcessor.IsSuspended)
                    G.EncodingQueueProcessor.SuspendProcess();

                if (G.ShowMessage("Encoding Progress",
                        "You want to abort ?", this,
                        ex => ex.ClearButton().AddButton(IZMessageBoxButton.YesNo)) == "Yes")
                {
                    LoadingIndicator.Set(-1, "Aborting ...");

                    G.EncodingQueueProcessor.ResumeProcess();
                    G.EncodingQueueProcessor.Abort();

                    return true; // Don't close indicator
                }

                if (!lastSus)
                    G.EncodingQueueProcessor.ResumeProcess();

                return false;
            }, -1, "Please wait ...");
        }

        public void Detail()
        {
            G.ShowMessage(new NotImplementedException(), ownerView: this);
        }

        private static string BytesToString(long byteCount, int round = 3)
        {
            string[] suf = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), round);
            return Math.Sign(byteCount) * num + suf[place];
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
                UnRegisterEvent();

            base.OnDeactivate(close);
        }

        public override bool IsClosed()
        {
            if (G.Config.EncodingProcess.AttachToMainWindow)
                return isCompleted;

            return base.IsClosed();
        }

        public void UnRegisterEvent()
        {
            G.EncodingQueueProcessor.OnStarted -= EncodingQueueProcessor_OnStarted;
            G.EncodingQueueProcessor.OnCompleted -= EncodingQueueProcessor_OnCompleted;
            G.EncodingQueueProcessor.OnQueueError -= EncodingQueueProcessor_OnQueueError;
            G.EncodingQueueProcessor.OnQueueStarted -= EncodingQueueProcessorOnQueueStarted;
            G.EncodingQueueProcessor.OnProgressChanged -= EncodingQueueProcessor_OnProgressChanged;
            G.EncodingQueueProcessor.OnQueueCompleted -= EncodingQueueProcessorOnQueueCompleted;
        }
        public override void CanClose(Action<bool> callback)
        {
            var cb = true;

            switch (G.EncodingQueueProcessor.CurrentQueue.Status)
            {
                case EncodingQueueStatus.Processing:
                    cb = false;
                    Abort();
                    break;
            }

            if (cb)  
                _cts?.Cancel();
            
            callback(cb);
        }
    }
}