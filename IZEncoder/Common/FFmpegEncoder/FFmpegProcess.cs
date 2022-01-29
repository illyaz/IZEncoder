namespace IZEncoder.Common.FFmpegEncoder
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading;
    using Process;

    public class FFmpegProcess : IDisposable
    {
        private readonly string _ffmpegArgs;
        private readonly string _ffmpegPath;
        private readonly string _workingDir;

        private bool _isRunning;
        private bool _isTcpRunning;
        private IZProcess _izProcess;
        private Thread _progressReaderThread;
        private TcpClient _tcpClient;
        private TcpListener _tcpListener;

        public FFmpegProcess(string ffmpegPath, string ffmpegArgs, string workingDir = null)
        {
            if (string.IsNullOrEmpty(ffmpegPath))
                throw new ArgumentNullException(nameof(ffmpegPath));

            if (string.IsNullOrEmpty(ffmpegArgs))
                throw new ArgumentNullException(nameof(ffmpegArgs));

            _ffmpegPath = ffmpegPath;
            _ffmpegArgs = ffmpegArgs;
            _workingDir = workingDir;

            _tcpListener = new TcpListener(IPAddress.Loopback, GetAvailablePort());
            _tcpListener.Start();

            _izProcess = new IZProcess(_ffmpegPath,
                $"-nostats -progress tcp://{_tcpListener.LocalEndpoint} {_ffmpegArgs}", _workingDir);
        }

        public Process NativeProcess => _izProcess;
        public IZProcess Process => _izProcess;

        public bool HasExited => _izProcess.HasExited;
        public void Dispose()
        {
            Abort();
        }

        public event EventHandler<FFmpegProgressData> ProgressChanged;

        public void Start()
        {
            _izProcess.Start();
            _isRunning = true;

            _izProcess.Process.Exited += Process_Exited;

            while (_tcpClient == null && !_izProcess.HasExited)
            {
                if (_tcpListener.Pending())
                {
                    _tcpClient = _tcpListener.AcceptTcpClient();
                    _isTcpRunning = true;

                    _progressReaderThread = new Thread(ProgressReaderLoop) {Priority = ThreadPriority.AboveNormal};
                    _progressReaderThread.Start();

                    break;
                }

                Thread.Sleep(1);
            }
        }

        public void Suspend()
        {
            if (!_isRunning)
                throw new InvalidOperationException();

            _izProcess?.Suspend();
        }

        public void Resume()
        {
            if (!_isRunning)
                throw new InvalidOperationException();

            _izProcess?.Resume();
        }

        public void Abort()
        {
            if (NativeProcess != null)
            {
                NativeProcess.StandardInput.WriteLine("q");
                NativeProcess.WaitForExit();
            }

            _isRunning = false;

            while (_progressReaderThread != null && _progressReaderThread.IsAlive)
                Thread.Sleep(1);

            _tcpClient?.Dispose();
            _tcpListener.Stop();
            _isTcpRunning = false;
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Exited");
        }

        private void ProgressReaderLoop()
        {
            using (var reader = new StreamReader(_tcpClient.GetStream()))
            {
                var isEnd = false;
                var fpsAvgs = new List<double>();
                var birateAvgs = new List<Tuple<double, double>>();
                var speedAvgs = new List<double>();
                var progressData = default(FFmpegProgressData);
                var lastProgressData = default(FFmpegProgressData);
                var line = string.Empty;

                while (_isTcpRunning && _isRunning && !isEnd)
                {
                    try
                    {
                        if ((line = reader.ReadLine()) == null)
                            break;
                    }
                    catch (IOException)
                    {
                        break;
                    }

                    var data = line.Split(new[] {'='}, 2);

                    if (data.Length == 2)
                    {
                        var key = data[0];
                        var value = data[1];

                        switch (key)
                        {
                            case "frame" when int.TryParse(value, out var v):
                                progressData.Frame = v;
                                break;
                                //case "fps" when double.TryParse(value, out var v):
                                //    progressData.Fps = v;
                                break;
                            case "stream_0_0_q":
                                break;
                            case "bitrate"
                                when value.Length > 7 &&
                                     double.TryParse(value.Substring(0, value.Length - 7), out var v):
                                progressData.BitRate = v;
                                break;
                            case "total_size" when long.TryParse(value, out var v):
                                progressData.TotalSize = v;
                                break;
                            case "out_time_ms":
                                break;
                            case "out_time" when TimeSpan.TryParse(value, out var v):
                                progressData.Time = v;
                                break;
                            case "dup_frames" when int.TryParse(value, out var v):
                                progressData.DuplicateFrames = v;
                                break;
                            case "drop_frames" when int.TryParse(value, out var v):
                                progressData.DropFrames = v;
                                break;
                            //case "speed" when value.Length > 1 &&
                            //                  double.TryParse(value.Substring(0, value.Length - 1), out var v):
                            //    progressData.Speed = v;
                            //    break;
                            case "progress":
                                progressData.ReportTime = DateTime.Now;
                                progressData.HasNext = value == "continue";
                                var reportTime = progressData.ReportTime - lastProgressData.ReportTime;
                                var reportTimeFactor = 1 / reportTime.TotalSeconds;

                                // FPS
                                var fps = (progressData.Frame - lastProgressData.Frame) * reportTimeFactor;
                                if (!(double.IsInfinity(fps) || double.IsNaN(fps)))
                                    fpsAvgs.Add(fps);

                                if (fpsAvgs.Count > 50)
                                    fpsAvgs.RemoveAt(0);

                                // Bitrate
                                //var bitrate = (progressData.TotalSize - lastProgressData.TotalSize) / 100d;
                                //if (!(double.IsInfinity(bitrate) || double.IsNaN(bitrate)))
                                //    birateAvgs.Add(new Tuple<double, double>(progressData.Time.TotalSeconds, bitrate));

                                //while (true)
                                //{
                                //    var t = progressData.Time.Subtract(TimeSpan.FromSeconds(1))
                                //        .TotalSeconds;
                                //    if (t < 0)
                                //        break;

                                //    var item = birateAvgs.FirstOrDefault(x =>
                                //        x.Item1 >t);

                                //    if(item == null)
                                //        break;

                                //    var i = birateAvgs.IndexOf(item);
                                //    if (i > 0)
                                //        birateAvgs.RemoveRange(0, i);
                                //    else
                                //        break;
                                //}

                                //if (birateAvgs.Count > 50)
                                //    birateAvgs.RemoveAt(0);

                                // Speed
                                var speed = (progressData.Time - lastProgressData.Time).TotalSeconds *
                                            reportTimeFactor;

                                if (!(double.IsInfinity(speed) || double.IsNaN(speed)))
                                    speedAvgs.Add(speed);

                                if (speedAvgs.Count > 50)
                                    speedAvgs.RemoveAt(0);

                                progressData.Fps = fpsAvgs.Count == 0 ? 0 : Math.Round(fpsAvgs.Average(), 3);

                                //progressData.BitRate =
                                //    birateAvgs.Count == 0
                                //        ? 0
                                //        : Math.Round(birateAvgs.Select(x => x.Item2).Average(), 3);

                                progressData.Speed = speedAvgs.Count == 0 ? 0 : Math.Round(speedAvgs.Average(), 3);
                                lastProgressData = progressData;
                                ProgressChanged?.Invoke(this, progressData);

                                isEnd = !progressData.HasNext;
                                break;
                        }
                    }
                }
            }
        }

        private static int GetAvailablePort(int startingPort = 49152)
        {
            var portArray = new List<int>();

            var properties = IPGlobalProperties.GetIPGlobalProperties();

            //getting active connections
            var connections = properties.GetActiveTcpConnections();
            portArray.AddRange(from n in connections
                where n.LocalEndPoint.Port >= startingPort
                select n.LocalEndPoint.Port);

            //getting active tcp listeners - WCF service listening in tcp
            var endPoints = properties.GetActiveTcpListeners();
            portArray.AddRange(from n in endPoints
                where n.Port >= startingPort
                select n.Port);

            //getting active udp listeners
            endPoints = properties.GetActiveUdpListeners();
            portArray.AddRange(from n in endPoints
                where n.Port >= startingPort
                select n.Port);

            portArray.Sort();

            for (var i = startingPort; i < ushort.MaxValue; i++)
                if (!portArray.Contains(i))
                    return i;

            return 0;
        }
    }
}