namespace IZEncoder.Launcher.Common.Client
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Helper;
    using RestSharp;
    using RestSharp.Serializers.NewtonsoftJson;

    //using RestRequest = RestSharp.Serializers.Newtonsoft.Json.RestRequest;

    public class LauncherClient : IDisposable
    {
        public delegate void DownloadProgressChangedCallback(long bytesReceived, long totalBytesToReceive);

        private readonly RestClient _client;
        private readonly MD5 _md5;

        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(out int Description, int ReservedValue);


        public LauncherClient(string baseUrl)
        {
            _client = new RestClient(baseUrl);
            _client.UseSerializer<JsonNetSerializer>();
            _md5 = MD5.Create();
        }

        public void Dispose()
        {
            _md5?.Dispose();
        }

        public static bool IsConnectedToInternet()
        {
            return InternetGetConnectedState(out _, 0);
        }

        public async Task<Dictionary<string, PatchInfo>> GetPatchInfosAsync()
        {
            return EnsureSuccess(
                    await _client.ExecuteAsync<Dictionary<string, PatchInfo>>(
                        new RestRequest("info.json", Method.GET)
                        {
                            OnBeforeDeserialization = resp =>
                                resp.ContentType = resp.IsSuccessful ? "application/json" : resp.ContentType
                        }))
                .Data;
        }

        public async Task<PatchInfo> GetLauncherInfoAsync()
        {
            return EnsureSuccess(await _client.ExecuteAsync<PatchInfo>(new RestRequest("launcher.json", Method.GET)
            {
                OnBeforeDeserialization = resp =>
                    resp.ContentType = resp.IsSuccessful ? "application/json" : resp.ContentType
            })).Data;
        }

        public async Task<AvisynthInfo> GetAvisynthInfoAsync()
        {
            return EnsureSuccess(await _client.ExecuteAsync<AvisynthInfo>(
                new RestRequest("avisynth.json", Method.GET)
                {
                    OnBeforeDeserialization = resp =>
                        resp.ContentType = resp.IsSuccessful ? "application/json" : resp.ContentType
                })).Data;
        }

        public async Task<VcRuntimeInfo> GetVcRuntimeInfoAsync()
        {
            return EnsureSuccess(await _client.ExecuteAsync<VcRuntimeInfo>(
                new RestRequest("vcruntime.json", Method.GET)
                {
                    OnBeforeDeserialization = resp =>
                        resp.ContentType = resp.IsSuccessful ? "application/json" : resp.ContentType
                })).Data;
        }

        public Task<byte[]> ComputeHashBytesAsync(Stream stream)
        {
            return Task.Factory.StartNew(() => _md5.ComputeHash(stream));
        }

        public Task<string> ComputeHashStringAsync(Stream stream)
        {
            return Task.Factory.StartNew(() =>
                BitConverter.ToString(_md5.ComputeHash(stream)).ToLower().Replace("-", ""));
        }

        public async Task DecompressAsync(Stream compressStream, Stream outputStream)
        {
            using (var gz = new GZipStream(compressStream, CompressionMode.Decompress, true))
            {
                await gz.CopyToAsync(outputStream);
            }
        }

        public async Task<DownloadAsyncResult> DownloadFileAsync(string resource, string save,
            DownloadProgressChangedCallback progressChangedCallback = null, CancellationToken? cancellationToken = null)
        {
            using (var wc = new WebClient())
            {
                var isCancelled = false;
                DownloadAsyncResult result = null;
                var userState = new DownloadAsyncUserState
                {
                    ProgressChangedCallback = progressChangedCallback,
                    CompletedCallback = (cancelled, exception) =>
                    {
                        result = new DownloadAsyncResult
                        {
                            Cancelled = cancelled,
                            Exception = exception
                        };
                    }
                };

                wc.DownloadProgressChanged += DownloadAsync_DownloadProgressChanged;
                wc.DownloadFileCompleted += DownloadAsync_DownloadFileCompleted;

                wc.DownloadFileAsync(new Uri(_client.BaseUrl, resource), save, userState);

                while (result == null)
                    if (cancellationToken != null && !isCancelled)
                    {
                        if (cancellationToken.Value.IsCancellationRequested)
                        {
                            wc.CancelAsync();
                            isCancelled = true;
                        }

                        try
                        {
                            await Task.Delay(1000, cancellationToken.Value);
                        }
                        catch (TaskCanceledException) { }
                    }
                    else
                    {
                        await Task.Delay(1);
                    }

                // Remove events
                wc.DownloadProgressChanged -= DownloadAsync_DownloadProgressChanged;
                wc.DownloadFileCompleted -= DownloadAsync_DownloadFileCompleted;

                return result;
            }
        }

        public async Task<DownloadAsyncResult> DownloadFileAndDecompressAsync(string resource, string save,
            DownloadProgressChangedCallback progressChangedCallback = null, CancellationToken? cancellationToken = null)
        {
            var result = await DownloadFileAsync(resource, $"{save}.izc", progressChangedCallback, cancellationToken);

            if (!result.Cancelled)
                try
                {
                    using (var compressStream = File.OpenRead($"{save}.izc"))
                    {
                        using (var decompressStream = File.Create(save))
                        {
                            await DecompressAsync(compressStream, decompressStream);
                        }
                    }
                }
                catch
                {
                    if (File.Exists($"{save}"))
                        File.Delete($"{save}");

                    throw;
                }
                finally
                {
                    if (File.Exists($"{save}.izc"))
                        File.Delete($"{save}.izc");
                }

            return result;
        }

        internal static Task<int> Launch(string applicationPath, GlobalWin32Message globalWin32Message,
            Action<string> messageCallback = null,
            Action<string, string> errorCallback = null, bool offline = false)
        {
            return Task.Factory.StartNew(() =>
            {
                var rnd = new Random();
                var isRun = false;
                var isTimeout = false;
                
                var uptMsg = User32Helper.RegisterWindowMessage(Guid.NewGuid().ToString());
                var exMsg = User32Helper.RegisterWindowMessage(Guid.NewGuid().ToString());
                var rdyMsg = User32Helper.RegisterWindowMessage(Guid.NewGuid().ToString());
                var rdyParam = rnd.Next(int.MaxValue);
                var rdyLparam = rnd.Next(int.MaxValue);
                var memMapBytes = new byte[1000000];
                var memMapName = Guid.NewGuid().ToString().Replace("-", "");

                using (var memMap = MemoryMappedFile.CreateNew(memMapName, memMapBytes.Length))
                {
                    using (var memAccessor = memMap.CreateViewAccessor(0, memMapBytes.Length))
                    {
                        var waitHande = new EventWaitHandle(false, EventResetMode.ManualReset, $"{memMapName}WH");
                        void MessageHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
                        {
                            if (msg == rdyMsg && wParam.ToInt32() == rdyParam && lParam.ToInt32() == rdyLparam)
                            {
                                globalWin32Message.MessageHandler -= MessageHandler;
                                isRun = true;
                            }
                            else if (msg == uptMsg && lParam != IntPtr.Zero)
                            {
                                memAccessor.ReadArray(0, memMapBytes, 0, lParam.ToInt32());
                                messageCallback?.Invoke(Encoding.UTF8.GetString(memMapBytes, 0, lParam.ToInt32()));
                                waitHande.Set();
                            }
                            else if (msg == exMsg && lParam != IntPtr.Zero)
                            {
                                memAccessor.ReadArray(0, memMapBytes, 0, lParam.ToInt32());
                                var data = Encoding.UTF8.GetString(memMapBytes, 0, lParam.ToInt32());
                                var sp = data.Split(new[] { "|--|" }, 2, StringSplitOptions.RemoveEmptyEntries);
                                errorCallback?.Invoke(sp[0], sp[1]);
                                waitHande.Set();
                            }
                        }

                        messageCallback += Console.WriteLine;
                        errorCallback += Console.WriteLine;
                        globalWin32Message.MessageHandler += MessageHandler;

                        var arg = $"--launcher={new string(globalWin32Message.Caption.Reverse().ToArray())},{globalWin32Message.Handle.ToInt64():x},{rdyMsg:x},{rdyParam:x},{rdyLparam:x},{uptMsg:x},{exMsg:x},{memMapBytes.Length:x},{memMapName}" + (offline ? " --offline" : "");
                        var sw = Stopwatch.StartNew();
                        var p = Process.Start(applicationPath, arg);
                        
                        while (!isRun && p != null)
                        {
                            if (p.HasExited)
                                break;

                            if (sw.Elapsed.TotalSeconds > 30) /* Timeout */
                            {
                                isTimeout = true;
                                break;
                            }

                            Thread.Sleep(1);
                        }


                        if (isTimeout)
                            p.Kill();

                        if (isTimeout)
                            return 1;

                        if (p == null)
                            return 2;

                        if (p.HasExited)
                            return 3;

                        return 0;
                    }
                }
            });
        }

        internal static Task BringToFront()
        {
            return Task.Factory.StartNew(() => User32Helper.PostMessage(
                User32Helper.FindWindowEx(IntPtr.Zero, IntPtr.Zero, null, "IZEncoderMessageWindow"),
                User32Helper.RegisterWindowMessage("izencoder.recv.bringtofront"), new IntPtr(9), new IntPtr(9)));
        }

        internal Task Update(string launcherPath = null)
        {
            return Task.Factory.StartNew(() =>
            {
                var si = new ProcessStartInfo(launcherPath ?? $"{Assembly.GetExecutingAssembly().Location}.updt",
                        $"--update {Process.GetCurrentProcess().Id}")
                    {UseShellExecute = false};

                var p = Process.Start(si);
                while (p != null && !p.HasExited)
                    Thread.Sleep(1);
            });
        }

        private void DownloadAsync_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.UserState is DownloadAsyncUserState userState)
                userState.CompletedCallback?.Invoke(e.Cancelled, e.Error);
        }

        private void DownloadAsync_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.UserState is DownloadAsyncUserState userState)
                userState.ProgressChangedCallback?.Invoke(e.BytesReceived, e.TotalBytesToReceive);
        }

        public IRestResponse<T> EnsureSuccess<T>(IRestResponse<T> res)
        {
            return (IRestResponse<T>) EnsureSuccess((IRestResponse) res);
        }

        public IRestResponse EnsureSuccess(IRestResponse res)
        {
            if (!res.IsSuccessful)
                throw new LauncherClientException(res.ErrorMessage ?? $"{(int) res.StatusCode} {res.StatusDescription}")
                {
                    StatusCode = res.StatusCode,
                    StatusDescription = res.StatusDescription,
                    RestResponse = res
                };

            return res;
        }

        private delegate void DownloadCompletedCallback(bool cancelled, Exception exception);

        private class DownloadAsyncUserState
        {
            public DownloadProgressChangedCallback ProgressChangedCallback { get; set; }
            public DownloadCompletedCallback CompletedCallback { get; set; }
        }
    }

    public class DownloadAsyncResult
    {
        public bool Cancelled { get; set; }
        public Exception Exception { get; set; }
    }
}