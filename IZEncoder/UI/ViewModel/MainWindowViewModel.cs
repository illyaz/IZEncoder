namespace IZEncoder.UI.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using IZEncoderNative.Avisynth;
    using Caliburn.Micro;
    using Common;
    using Common.FFmpegEncoder;
    using Common.FFMSIndexer;
    using Common.Helper;
    using Common.MessageBox;
    using Common.Project;
    using Common.SubtitleAnalyzer;
    using Microsoft.Win32;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using View;
    using MahApps.Metro.Controls;
    using System.Windows.Media.Animation;
    using MahApps.Metro;

    public class MainWindowViewModel : IZEScreen<MainWindowView>
    {
        private readonly IWindowManager _windowManager;

        public MainWindowViewModel(Global g, IWindowManager windowManager)
        {
            _windowManager = windowManager;
            G = g;
            G.MainWindowVm = this;
        }

        public Global G { get; }
        public LoadingIndicator LoadingIndicator { get; set; }
        public EncoderTemplate SelectedTemplate { get; set; }
        public EncodingProgressViewModel EncodingProgress { get; set; }
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            LoadingIndicator = new LoadingIndicator(View.Content as Grid, View);
            SelectedTemplate = G.FFmpegTemplates.FirstOrDefault(x => x.Guid == G.Config.Main.SelectedEncoderTemplate) ??
                              G.FFmpegTemplates.FirstOrDefault();

            ActionValidationRule.Attach(View.OutputFile, TextBox.TextProperty, value =>
            {
                if (value is string v)
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    Path.GetFullPath(v);

                return null;
            });

            View.Title = $"IZEncoder V{G.Version}";

            if (G.Config.Application.ShowTitleBar)
            {
                View.Height += 30;
                View.UseNoneWindowStyle = false;
                View.WindowStyle = WindowStyle.SingleBorderWindow;
                View.ShowTitleBar = true;
                View.Top -= 30;
                View.VersionTextBlock.Visibility = Visibility.Collapsed;
            }

            if (G.Config.Application.UseDarkTheme)
                ThemeManager.ChangeAppTheme(Application.Current, "IZEDark");            
        }

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);

            if (!LauncherHelper.IsBypass() && LauncherHelper.IsValid())
                LauncherHelper.Ready();

            if (await G.FontIndexer.HasNewFont())
                await LoadingIndicator.Run(async () =>
                {
                    await G.FontIndexer.Index((percent, file) =>
                    {
                        LoadingIndicator.Set(percent, "{0:N2} % Indexing fonts ...");
                        return true;
                    });

                    LoadingIndicator.Set(-1, "Writing index ...");
                    await G.FontIndexer.Save();
                }, text: "Indexing fonts ...");
            
#if DEBUG
            await LoadingIndicator.Run(async () =>
            {
                await InputExecute(@"D:\Downloads\Video\D_Zeal「餞の鳥」 _ ときのそら × 星街すいせい(Cover).mkv");
                //await SubtitleExecute(new[] { @"D:\Downloads\Somali_to_Mori_no_Kami_OP_timed_test_enc_2.ass" });
                G.ActiveProject.Output = @"C:\Users\Illyaz\Desktop\test.mp4";
                UpdateProjectOutputExtension();
            });
#endif
        }

        public void OnPropertyChanged(string propertyName, object before, object after)
        {
            if (propertyName.Equals(nameof(SelectedTemplate)))
            {
                if (before is EncoderTemplate b)
                    b.PropertyChanged -= SelectedEncoder_PropertyChanged;

                if (after is EncoderTemplate a)
                {
                    a.PropertyChanged += SelectedEncoder_PropertyChanged;
                    G.Config.Main.SelectedEncoderTemplate = SelectedTemplate.Guid;
                }


                UpdateProjectOutputExtension();
            }

            NotifyOfPropertyChange(propertyName);
        }

        private void SelectedEncoder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateProjectOutputExtension();
        }

        public void UpdateProjectOutputExtension()
        {
            if (G?.ActiveProject?.Output == null || SelectedTemplate == null)
                return;

            var container = G.FFmpegParameters.Find<FFmpegContainerParameter>(SelectedTemplate.Container);
            G.ActiveProject.Output = Path.ChangeExtension(G.ActiveProject.Output, container.Output);
        }

        public void InputPreviewDragOver(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.None;

            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] datas
                  && datas.Length == 1))
                return;

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        public async Task InputPreviewDrop(DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] datas
                && datas.Length == 1)
                await LoadingIndicator.Run(async () => await InputExecute(datas[0]));
        }

        public async Task InputPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;

            var ofd = new OpenFileDialog {FileName = G.ActiveProject.Input?.Filename ?? string.Empty};
            var result = ofd.ShowDialog();
            if (result != null && result.Value)
                await LoadingIndicator.Run(async () => await InputExecute(ofd.FileName));
        }

        public void OutputFileLostFocus()
        {
            try
            {
                G.ActiveProject.Output = Path.GetFullPath(G.ActiveProject.Output);
                UpdateProjectOutputExtension();
            }
            catch { /* ignored */ }
        }

        public void OutputPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;
                
            var container = G.FFmpegParameters.Find<FFmpegContainerParameter>(SelectedTemplate.Container);
            var format = G.FFmpegSupportedFormats.First(x =>
                x.IsMuxer && x.Name.Equals(container.Format, StringComparison.OrdinalIgnoreCase));

            var sfd = new SaveFileDialog
            {
                FileName = G.ActiveProject.Output,
                Filter = $"{format.Description}|*.{container.Output}"
            };
            var result = sfd.ShowDialog();
            if (result != null && result.Value)
                G.ActiveProject.Output = sfd.FileName;
        }

        private async Task InputExecute(string file, bool indicator = true)
        {
            retry:

            if (indicator)
                LoadingIndicator.Set(-1, "Reading media infomation ...");

            try
            {
                MediaInfomation inputInfo = null;

                if (file.EndsWith(".avs"))
                    await Task.Factory.StartNew(() =>
                    {
                        using (var bridge = new AvisynthBridge())
                        {
                            bridge.LoadPlugin(DependencySearcher.Search("IZUnicodeAvisynth.dll",
                                G.Config.Application.DependencySearchPaths.Select(Path.GetFullPath)));

                            inputInfo = MediaInfomationHelper.Create(file, bridge);
                        }
                    });
                else
                    inputInfo = await MediaInfomationHelper.CreateAsync(file);

                if (!inputInfo.VideoTracks.Any())
                {
                    G.ShowMessage("Couldn't find any video track",
                        extend: ex => ex.AddText("File: ").AddShowInExplorer(file, new Uri(file)));

                    goto abort;
                }

                if (!inputInfo.AudioTracks.Any() && G.ShowMessage("Couldn't find any audio track",
                        extend: ex =>
                            ex.AddText("You want to use ").AddShowInExplorer("this", new Uri(file))
                                .AddText(" file ?")
                                .ClearButton().AddButton("No")
                                .AddButton("Yes").FocusButton()) == "No")
                    goto abort;

                if (inputInfo.AudioTracks.Any())
                    G.ActiveProject.InputAudioTrack =
                        inputInfo.AudioTracks.FirstOrDefault(x => x.Default || x.Forced)?.Index ??
                        inputInfo.AudioTracks.First().Index;
                else
                    G.ActiveProject.InputAudioTrack = -2; // No Audio

                if (inputInfo.VideoTracks.Any(x => x.FrameRate == 0 || x.IsVfr))
                    if (G.ShowMessage("Variable Frame Rate Detected",
                            extend: ex =>
                                ex.AddText(
                                        "Variable frame rate is not fully supported by avisynth\nPosible incorrect audio/video sync")
                                    .AddLine(2)
                                    .AddText("You want to use ").AddShowInExplorer("this", new Uri(file))
                                    .AddText(" file ?")
                                    .ClearButton().AddButton("No")
                                    .AddButton("Yes").FocusButton()) == "No")
                        goto abort;

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                foreach (var videoTrackInfomation in inputInfo.VideoTracks.Where(x => x.FrameRate == 0))
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (videoTrackInfomation.FrameRate != 0)
                        continue;

                    if (indicator)
                        LoadingIndicator.Set(-1, "Finding video frame rate ...");

                    await Task.Factory.StartNew(() =>
                    {
                        var si = new ProcessStartInfo(Path.GetFullPath("ffmpeg\\ffprobe.exe"),
                            $@"""{file}"" -v quiet -print_format json -show_streams -hide_banner")
                        {
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        var p = Process.Start(si);
                        p.WaitForExit();

                        var result = JsonConvert.DeserializeObject<JObject>(p.StandardOutput.ReadToEnd());
                        foreach (var jToken in result.SelectToken("streams").ToObject<JArray>())
                        {
                            var index = jToken["index"].ToObject<int>();
                            if (index != videoTrackInfomation.Index)
                                continue;

                            var rFrameRate = jToken["r_frame_rate"].ToObject<string>().Split('/').Select(double.Parse)
                                .ToArray();

                            // ReSharper disable CompareOfFloatsByEqualityOperator
                            if (rFrameRate[0] == 90000 && rFrameRate[1] == 1)
                                // ReSharper restore CompareOfFloatsByEqualityOperator
                                videoTrackInfomation.FrameRate = 25;
                            else
                                videoTrackInfomation.FrameRate = rFrameRate[0] / rFrameRate[1];
                        }
                    });
                }

                if (indicator)
                    LoadingIndicator.Set(-1, "Initializing indexer ...");

                if (!inputInfo.FileExtension.Equals("avs", StringComparison.OrdinalIgnoreCase))
                {
                    using (var indexer = new FFMSIndexer(inputInfo.Filename))
                    {
                        var indexPath = Path.Combine(G.Config.Application.TempPath, "ffindex",
                            inputInfo.Filename.CRC32String());

                        Path.GetDirectoryName(indexPath).EnsureDirectoryExists();

                        if (!await indexer.IndexBelongsToFileAsync(indexPath))
                        {
                            if (await indexer.IndexBelongsToFileAsync($"{inputInfo.Filename}.ffindex"))
                            {
                                if (indicator)
                                    LoadingIndicator.Set(-1, "Copying existing index file ...");

                                await Task.Factory.StartNew(() =>
                                    File.Copy($"{inputInfo.Filename}.ffindex", indexPath));
                            }
                            else
                            {
                                await indexer.DoIndexAsync((long current, long total, ref bool cancel) =>
                                {
                                    if (indicator)
                                        LoadingIndicator.Set(current / (double) total * 100,
                                            "Indexing source: {0:N2}%");
                                });

                                if (indicator)
                                    LoadingIndicator.Set(-1, "Writing index ...");

                                await indexer.WriteIndexAsync(indexPath);
                            }
                        }
                    }
                }

                G.ActiveProject.Input = inputInfo;
            }
            catch (Exception e)
            {
                G.LogException(e);

                if (G.ShowMessage(e, "Can't read media infomation",
                        extend: ext =>
                            ext.AddButton("Retry").FocusButton()
                                .AddLine(2).AddText("File: ")
                                .AddShowInExplorer(file, new Uri(file))) == "Retry")
                    goto retry;

                goto abort;
            }

            return;

            abort:
            G.ActiveProject.Input = null;
        }

        public void SubtitlePreviewDragOver(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.None;

            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] datas
                  && datas.Any(x => x.EndsWith(".ass", StringComparison.OrdinalIgnoreCase))))
                return;

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        public async void SubtitlePreviewDrop(DragEventArgs e)
        {
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] datas))
                return;

            var assFiles = datas.Where(x => x.EndsWith(".ass", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (assFiles.Length > 0)
                await LoadingIndicator.Run(async () => await SubtitleExecute(assFiles));
        }

        public async Task SubtitlePreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;

            var ofd = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Advanced SSA subtitles|*.ass"
            };

            var result = ofd.ShowDialog();
            if (result != null && result.Value)
                await LoadingIndicator.Run(async () => await SubtitleExecute(ofd.FileNames));
        }

        public async Task SubtitleExecute(IEnumerable<string> assFiles, bool indicator = true)
        {
            var files = assFiles as string[] ?? assFiles.ToArray();
            for (var i = 0; i < files.Length; i++)
            {
                retryAnalysis:
                if (await G.FontIndexer.HasNewFont())
                {
                    if (indicator)
                        LoadingIndicator.Set(-1, "Indexing fonts ...");

                    await G.FontIndexer.Index((percent, file) =>
                    {
                        if (indicator)
                            LoadingIndicator.Set(percent, "{0:N2} % Indexing fonts ...");

                        return true;
                    });

                    if (indicator)
                        LoadingIndicator.Set(-1, "Writing index ...");
                    await G.FontIndexer.Save();
                }

                var assFile = files[i];
                var assAnalyzer = new SubtitleAnalyzer(assFile, G.FontIndexer,
                    G.Config.Subtitle.Analyzer.MissingFont, G.Config.Subtitle.Analyzer.MissingStyle);
                var assAnalyzerMessage = (string) null;

                assAnalyzer.StatusChanged += (sender, status) =>
                {
                    if (indicator)
                        LoadingIndicator.Set(status.Percent,
                            status.Percent > 0
                                ? $"[{i + 1}/{files.Length}] {(status.IsParsing ? "Parsing" : "Analyzing")}: {{0:N2}}%"
                                : $"[{i + 1}/{files.Length}] {status.Message}");
                };

                try
                {
                    var result = await assAnalyzer.RunAsync();
                    if (indicator)
                        LoadingIndicator.Set(-1, "Waiting ...");

                    if (result.HasError)
                    {
                        if (result.Exception != null)
                            throw result.Exception;

                        var mresult = G.ShowMessage("Subtitle Analyzer", extend: ex =>
                        {
                            ex.BuildAnalysisResult(result);
                            assAnalyzerMessage = ex.GetMessageText().Trim();

                            ex.TrimLines()
                                .AddLine(2)
                                .AddErrorText("You want to add ", fontWeight: FontWeights.Bold)
                                .AddErrorShowInExplorer("this", assFile,
                                    fontWeight: FontWeights.Bold)
                                .AddErrorText(" file ?", fontWeight: FontWeights.Bold)
                                .ClearButton()
                                .AddButton(IZMessageBoxButton.YesNoRetry);
                        });

                        switch (mresult)
                        {
                            case "No":
                                continue;
                            case "Retry":
                                goto retryAnalysis;
                        }
                    }

                    var assSub = G.ActiveProject.Subtitles.FirstOrDefault(x => x.Filename == assFile);
                    if (assSub != null)
                    {
                        G.ActiveProject.Subtitles[G.ActiveProject.Subtitles.IndexOf(assSub)].IsMod =
                            result.HasMod;

                        G.ActiveProject.Subtitles[G.ActiveProject.Subtitles.IndexOf(assSub)].AnalyzerMessage =
                            assAnalyzerMessage;
                    }
                    else
                    {
                        OnUIThread(() =>
                            G.ActiveProject.Subtitles.Add(
                                new AvisynthSubtitle(assFile, result.HasMod)
                                {
                                    AnalyzerMessage = assAnalyzerMessage
                                }));
                    }
                }
                catch (Exception ex)
                {
                    G.LogException(ex);
                    if (G.ShowMessage(ex, "Subtitle Analyzer",
                            extend: ext =>
                                ext.AddButton("Retry").FocusButton()
                                    .AddLine().AddLine().AddText("File: ")
                                    .AddShowInExplorer(files[i], new Uri(files[i]))) == "Retry")
                        goto retryAnalysis;
                }
            }
        }

        public void SubtitleSettingsClick(MouseEventArgs e)
        {
            var popup = IoC.Get<SubtitleSettingsViewModel>();
            _windowManager.ShowPopup(popup);
        }

        public void EncoderSettingsClick()
        {
            var es = IoC.Get<TemplateSettingsViewModel>();
            es.ParentVm = this;
            _windowManager.ShowDialog(es);
        }
       
        public Task Queue()
        {
            if (View.HasValidationError())
                View.GetValidationErrors().Select(x => x.BindingInError).OfType<BindingExpressionBase>()
                    .Select(x => x.Target).OfType<UIElement>().FirstOrDefault()?.Focus();
            else
                return Queue(null);

            return Task.CompletedTask;
        }

        public async Task Queue(DateTime? fromDate)
        {
            var abort = false;
            EncodingQueue q = null;
            if (G.ActiveProject.Input != null && G.ActiveProject.Output != null)
            {
                await LoadingIndicator.Run(() =>
                {
                    try
                    {
                        G.ActiveProject.VideoEncoder = SelectedTemplate.Video;
                        G.ActiveProject.VideoEncoderSettings = SelectedTemplate.VideoSettings.DeepCopy();
                        G.ActiveProject.AudioEncoder = SelectedTemplate.Audio;
                        G.ActiveProject.AudioEncoderSettings = SelectedTemplate.AudioSettings.DeepCopy();
                        G.ActiveProject.Container = SelectedTemplate.Container;
                        G.ActiveProject.ContainerSettings = SelectedTemplate.ContainerSettings.DeepCopy();

                        q = G.EncodingQueueProcessor.Create(G.ActiveProject, G.FFmpegParameters, G.AvisynthFilters);
#if !DEBUG
                        G.ActiveProject = new AvisynthProject();
#endif

                        //using (var preview = View.Dispatcher.Invoke(() => new PlayerViewModel()))
                        //{
                        //    preview.Open(Path.Combine(q.WorkingPath, "script.avs"), mode: PlayerViewModel.OpenModes.Import);
                        //    View.Dispatcher.Invoke(() => _windowManager.ShowDialog(preview));
                        //}
                    }
                    catch (Exception e)
                    {
                        abort = true;
                        G.ShowMessage(e);
                        if (Debugger.IsAttached)
                            throw;
                    }
                }, text: "Creating Queue ...");

                if (!abort && (G.EncodingQueueProcessor.IsRunning || G.ShowMessage("Queue",
                        "You want to start from current queue ?",
                        this, ex => ex.ClearButton().AddButton(IZMessageBoxButton.YesNo)) != "Yes"))
                    return;
            }
            else if (!G.EncodingQueueProcessor.HasUnfinishQueue())
            {
                G.ShowMessage("Queue", "Please specify input/output", this);
                return;
            }

            if (!abort)
                StartQueue(q?.CreatedOn);
        }

        public void StartQueue(DateTime? fromDate = null)
        {
            if (G.EncodingProgressVm != null && !G.EncodingProgressVm.IsClosed())
            {
                if (!G.Config.EncodingProcess.AttachToMainWindow)
                    G.EncodingProgressVm.BringToFront();
            }
            else
            {
                G.EncodingProgressVm = IoC.Get<EncodingProgressViewModel>();
                G.EncodingProgressVm.StartDate = fromDate;
                
                void BringMeUp(object sender, EventArgs e)
                {
                    G.EncodingQueueProcessor.OnCompleted -= BringMeUp;
                    View.Dispatcher.Invoke(() => BringToFront());
                }

                G.EncodingQueueProcessor.OnCompleted += BringMeUp;
                if (G.Config.EncodingProcess.AttachToMainWindow)
                {
                    View.EncodingProgressGrid.Visibility = Visibility.Visible;
                    var anim = new DoubleAnimation(View.Width + 180, new Duration(TimeSpan.FromMilliseconds(300)));
                    View.BeginAnimation(FrameworkElement.WidthProperty, anim);

                    EncodingProgress = G.EncodingProgressVm;
                    (EncodingProgress.GetView() as EncodingProgressView)
                        .InnerGrid.Margin = new Thickness(10, 10, 10, 5);

                    void OnCompleted(object sender, EventArgs e)
                    {
                        G.EncodingQueueProcessor.OnCompleted -= OnCompleted;
                       
                        View.Dispatcher.Invoke(() =>
                        {
                            var anim2 = new DoubleAnimation(View.Width - 180, new Duration(TimeSpan.FromMilliseconds(300)));
                            anim2.Completed += (s, _) =>
                            {
                                View.EncodingProgressGrid.Visibility = Visibility.Collapsed;
                                EncodingProgress = null;
                            };

                            View.BeginAnimation(FrameworkElement.WidthProperty, anim2);
                            EncodingProgress.UnRegisterEvent();
                        });
                    }

                    G.EncodingQueueProcessor.OnCompleted += OnCompleted;
                }
                else
                    _windowManager.ShowWindow(G.EncodingProgressVm, settings: new Dictionary<string, object>
                    {
                        { "Title", "Encoding Status"  },
                        { "ShowCloseButton", false  },
                        { "TitleCharacterCasing", CharacterCasing.Normal },
                        { "WindowTransitionsEnabled", false },
                        { "SizeToContent", SizeToContent.Height },
                        { "Width", 160 },
                        { "ResizeMode", ResizeMode.CanMinimize },
                        { "GlowBrush", Application.Current.FindResource("AccentColorBrush") }
                    });
                
            }
        }

        public void Advance()
        {
            G.ShowMessage(new NotImplementedException(), ownerView: this);
        }

        public void SettingsTab(MouseButtonEventArgs e)
        {
            e.Handled = true;
            G.ShowMessage(new NotImplementedException(), ownerView: this);
        }

        public async Task QueueTab(MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (G.QueueManagementVm != null && !G.QueueManagementVm.IsClosed())
            {
                G.QueueManagementVm.BringToFront();
            }
            else
            {
                G.QueueManagementVm = IoC.Get<QueueManagementViewModel>();
                _windowManager.ShowWindow(G.QueueManagementVm);
                await G.QueueManagementVm.WaitClosed();
            }
        }

        public override async void CanClose(Action<bool> callback)
        {
            await LoadingIndicator.Run(() =>
            {
                if (G.EncodingQueueProcessor.IsRunning)
                {
                    G.EncodingProgressVm.BringToFront();
                    G.EncodingProgressVm.Abort().Wait();
                }

                if (G.EncodingQueueProcessor.IsRunning)
                {
                    callback(false);
                    return;
                }

                G.QueueManagementVm?.TryClose();
                callback(true);
            }, -1, "Please wait ...");
        }

        public void Exit()
        {
            TryClose();
        }
    }
}