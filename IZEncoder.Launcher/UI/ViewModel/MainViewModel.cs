namespace IZEncoder.Launcher.UI.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Caliburn.Micro;
    using Common.Client;
    using Common.Helper;

    public class MainViewModel : Screen
    {
        private readonly LauncherClient _launcherClient;

        private bool _hasApp;

        private static string LaunchedFile =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".izencoder-launched");
        public MainViewModel(Global g, LauncherClient launcherClient)
        {
            G = g;
            _launcherClient = launcherClient;
        }

        public Global G { get; }
        public double MaxPercent { get; set; }
        public double Percent { get; set; }
        public string Text { get; set; } = "Loading ...";
        public bool IsIndeterminate { get; set; } = true;

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            await Startup();
            TryClose();
        }

        private async Task<bool> Startup()
        {
            IsIndeterminate = true;

#if !DEBUG
            if (await InternetCheck())
            {
                if (!await LauncherUpdate())
                    return false;

                if (!await AvisynthUpdate())
                    return false;

                if (!await VcRuntimeUpdate())
                    return false;

                IsIndeterminate = true;
                Text = "Loading info ...";

                var patchInfo = await _launcherClient.GetPatchInfosAsync();

                IsIndeterminate = false;
                MaxPercent = patchInfo.Count - 1;
                Percent = 0;

                foreach (var info in patchInfo)
                {
                    Text = $"Checking: {info.Key}";
                    var fi = new FileInfo(info.Key);
                    info.Value.Exists = fi.Exists;
                    if (fi.Exists)
                        using (var stream = fi.OpenRead())
                            info.Value.IsMatched =
                                await _launcherClient.ComputeHashStringAsync(stream) == info.Value.Hash;

                    Percent++;
                }

                var downloadLists = patchInfo.Where(x => !x.Value.Exists || !x.Value.IsMatched && !x.Value.CanChange)
                    .ToDictionary(x => x.Key, x => x.Value);
                var offset = 0L;
                var i = 0;

                MaxPercent = downloadLists.Sum(x => x.Value.CompressSize);
                Percent = 0;

                void Report() =>
                    Text =
                        $"[{i}/{downloadLists.Count}] Updating ({((long) Percent).SizeSuffix(2)} / {((long) MaxPercent).SizeSuffix(2)})"
                ;

                foreach (var info in downloadLists)
                    try
                    {
                        i++;
                        Report();

                        var dir = Path.GetDirectoryName(info.Key);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        var dlResult = await _launcherClient.DownloadFileAndDecompressAsync(info.Value.Hash,
                            $"{info.Key}", (received, receive) =>
                            {
                                Report();
                                Percent = offset + received;
                            });

                        offset += info.Value.CompressSize;

                        if (dlResult.Exception != null)
                            throw dlResult.Exception;
                    }
                    catch (Exception e)
                    {
                        G.ShowMessage(e, "Failure to update application");
                        return false;
                    }

                foreach (var info in patchInfo)
                {
                    File.SetLastWriteTimeUtc(info.Key, info.Value.Time);
                }
            }
            else
            {
                if (!File.Exists(LaunchedFile))
                {
                    G.ShowMessage("Failure to update application", "Internet connection not available.");
                    return false;
                }

                if (G.ShowMessage("Failure to update application",
                        "Internet connection not available.\nYou can launch IZEncoder in offline mode", this,
                        ex => ex.ClearButton().AddButton("Exit").AddSuccessButton("Launch").FocusButton()) ==
                    "Launch")
                    return await Launch(true);

                return false;
            }
#endif

            //Launch main application
            return await Launch();
        }

        private bool VisualStudioVersionCheck()
        {
            var vc = VisualStudioVersionHelper.Get14();
            if (vc == null)
                return false;

            return vc >= new Version(14, 23, 27820, 0);
        }

        private Task<bool> InternetCheck()
        {
            IsIndeterminate = true;
            Text = "Checking internet connection ...";
            return Task.Factory.StartNew(LauncherClient.IsConnectedToInternet);
        }
        private async Task<bool> LauncherUpdate()
        {
            IsIndeterminate = true;
            Text = "Loading launcher info ...";

            try
            {
                var launcherInfo = await _launcherClient.GetLauncherInfoAsync();

                using (var launcherStream = File.OpenRead(Assembly.GetExecutingAssembly().Location))
                {
                    launcherInfo.IsMatched =
                        await _launcherClient.ComputeHashStringAsync(launcherStream) == launcherInfo.Hash;
                }

                if (!launcherInfo.IsMatched)
                {
                    Text = "Updating Launcher";

                    var dlResult = await _launcherClient.DownloadFileAndDecompressAsync(launcherInfo.Hash,
                        $"{Assembly.GetExecutingAssembly().Location}.updt", (received, receive) =>
                        {
                            IsIndeterminate = false;
                            Text =
                                $"Updating Launcher ({received.SizeSuffix(2)} / {receive.SizeSuffix(2)})";
                            Percent = received;
                            MaxPercent = receive;
                        });

                    try
                    {
                        if (dlResult.Exception != null)
                            throw dlResult.Exception;

                        await _launcherClient.Update();
                    }
                    catch (Exception e)
                    {
                        G.ShowMessage(e, "Failure to update launcher");
                        return false;
                    }
                }
            }
            catch (LauncherClientException e)
            {
                G.ShowMessage(e, "Launcher info download failure");
                return false;
            }

            return true;
        }

        private async Task<bool> AvisynthUpdate()
        {
            IsIndeterminate = true;
            Text = "Checking avisynth ...";

            try
            {
                var avisynthInfo = await _launcherClient.GetAvisynthInfoAsync();

                if (!(AvisynthVersionHelper.GetPlus() >= avisynthInfo.Version))
                {
                    Text = "Downloading Avisynth Installer";

                    var dlResult = await _launcherClient.DownloadFileAsync(avisynthInfo.Url, "avisynth-installer.exe",
                        (received, receive) =>
                        {
                            IsIndeterminate = false;
                            Text =
                                $"Downloading Avisynth ({received.SizeSuffix(2)} / {receive.SizeSuffix(2)})";
                            Percent = received;
                            MaxPercent = receive;
                        });

                    try
                    {
                        if (dlResult.Exception != null)
                            throw dlResult.Exception;

                        IsIndeterminate = true;
                        Text = "Installing Avisynth ...";
                        var si = new ProcessStartInfo("avisynth-installer.exe",
                            "/silent /nocancel /suppressmsgboxes /norestart") {UseShellExecute = false};

                        var installerProcess = Process.Start(si);

                        // ReSharper disable once PossibleNullReferenceException
                        while (!installerProcess.HasExited)
                            await Task.Delay(1);

                        Text = "Deleting avisynth installer ...";
                        while (File.Exists("avisynth-installer.exe"))
                            try
                            {
                                File.Delete("avisynth-installer.exe");
                            }
                            catch
                            {
                                /* ignored */
                            }

                        return true;
                    }
                    catch (Exception e)
                    {
                        G.ShowMessage(e, "Failure to update avisynth");
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (LauncherClientException e)
            {
                G.ShowMessage(e, "Avisynth info download failure");
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists("avisynth-installer.exe"))
                        File.Delete("avisynth-installer.exe");
                }
                catch
                {
                    /* ignored */
                }
            }

            return false;
        }

        private async Task<bool> VcRuntimeUpdate()
        {
            IsIndeterminate = true;
            Text = "Checking Visual C++ Runtime ...";

            try
            {
                var vcruntimeInfo = await _launcherClient.GetVcRuntimeInfoAsync();
                var vc = VisualStudioVersionHelper.Get14();

                if (vc == null || !(vc >= new Version(vcruntimeInfo.Version)))
                {
                    Text = "Downloading VC++ Installer";

                    var dlResult = await _launcherClient.DownloadFileAsync(vcruntimeInfo.Url, "vc++-installer.exe",
                        (received, receive) =>
                        {
                            IsIndeterminate = false;
                            Text =
                                $"Downloading VC++ ({received.SizeSuffix(2)} / {receive.SizeSuffix(2)})";
                            Percent = received;
                            MaxPercent = receive;
                        });

                    try
                    {
                        if (dlResult.Exception != null)
                            throw dlResult.Exception;

                        IsIndeterminate = true;
                        Text = "Installing VC++ Runtime ...";

                        var si = new ProcessStartInfo("vc++-installer.exe",
                                "/install /passive /norestart")
                            {UseShellExecute = false};

                        var installerProcess = Process.Start(si);

                        // ReSharper disable once PossibleNullReferenceException
                        while (!installerProcess.HasExited)
                            await Task.Delay(1);

                        Text = "Deleting vc++ installer ...";
                        while (File.Exists("vc++-installer.exe"))
                            try
                            {
                                File.Delete("vc++-installer.exe");
                            }
                            catch
                            {
                                /* ignored */
                            }

                        return true;
                    }
                    catch (Exception e)
                    {
                        G.ShowMessage(e, "Failure to update visual c++ runtime");
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (LauncherClientException e)
            {
                G.ShowMessage(e, "Visual C++ Runtime info download failure");
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists("vc++-installer.exe"))
                        File.Delete("vc++-installer.exe");
                }
                catch
                {
                    /* ignored */
                }
            }

            return false;
        }

        private async Task<bool> Launch(bool offline = false)
        {
            Text = "Starting IZEncoder ...";
            IsIndeterminate = true;
            int launchCode;
            int waitCount = 0;
            void MessageCallback(string message)
            {
                try
                {

                    Volatile.Write(ref waitCount, Volatile.Read(ref waitCount) + 1);
                    Text = message;
                }
                finally
                {
                    Volatile.Write(ref waitCount, Volatile.Read(ref waitCount) - 1);
                }
            }

            void ErrorCallback(string message, string trace)
            {
                try
                {
                    Volatile.Write(ref waitCount, Volatile.Read(ref waitCount) + 1);
                    G.ShowMessage(message, trace);
                }
                finally
                {
                    Volatile.Write(ref waitCount, Volatile.Read(ref waitCount) - 1);
                }
            }

            if ((launchCode = await LauncherClient.Launch("IZEncoder.exe", G.GlobalWin32Message, MessageCallback, ErrorCallback, offline)) != 0)
            {
                while (Volatile.Read(ref waitCount) > 0)
                    await Task.Delay(1);

                switch (launchCode)
                {
                    case 1:
                        G.ShowMessage("Failed to start application", "Timedout");
                        break;
                    case 2:
                        G.ShowMessage("Failed to start application", "Unknown");
                        break;
                    case 3:
                        G.ShowMessage("Failed to start application",
                            "Application has exited before successfully startup");
                        break;
                }

                if(File.Exists(LaunchedFile))
                    File.Delete(LaunchedFile);

                return false;
            }

            File.WriteAllBytes(LaunchedFile, BitConverter.GetBytes(DateTime.UtcNow.ToOADate()));
            return true;
        }
    }
}