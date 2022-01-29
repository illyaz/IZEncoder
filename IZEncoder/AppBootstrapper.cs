namespace IZEncoder
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Reflection;
    using System.Threading;
    using System.Windows;

    using Caliburn.Micro;

    using Common;
    using Common.AvisynthFilter;
    using Common.FFmpegEncoder;
    using Common.Helper;
    using Common.Process;
    using Common.Project;

    using IZEncoder.Common.FontIndexer;
    using IZEncoder.UI.ViewModel;

    using log4net.Config;

    using MahApps.Metro;
    using MahApps.Metro.Controls;

    public class AppBootstrapper : BootstrapperBase
    {
        private SimpleContainer container;
        public AppBootstrapper()
        {
            var args = EnvironmentHelper.GetCommandLineArgs();
            if (!LauncherHelper.IsBypass())
            {
                if (args.ContainsKey("type") && args["type"] == "background-process-killer" && int.TryParse(args["main-process-id"], out var hostPid))
                {
                    try
                    {
                        App.ChildKillerMutex = new Mutex(true, "cea7f00b-b5b6-4f7b-acd7-e63269e6b326");
                        if (!App.ChildKillerMutex.WaitOne(TimeSpan.Zero))
                            Environment.Exit(0);
                        var p = Process.GetProcessById(hostPid);
                        var pids = new List<int>();
                        var exitReq = false;
                        var processWaitThread = new Thread(() =>
                        {
                            while (!p.HasExited && !exitReq)
                                Thread.Sleep(1);

                            pids.Reverse();
                            foreach (var pid in pids)
                                KillProcessAndChildren(pid);
                        });

                        var inputThread = new Thread(() =>
                        {
                            var buf = new char[1];
                            var text = string.Empty;
                            var isEof = false;

                            while (!exitReq
                                   && Console.In.Read(buf, 0, buf.Length) > 0)
                                if (buf[0] == '$')
                                {
                                    var input = text;

                                    if (input.StartsWith("add ") &&
                                        int.TryParse(input.Replace("add ", ""), out var addPid))
                                    {
                                        if (!pids.Contains(addPid))
                                            try
                                            {
                                                var childP = Process.GetProcessById(addPid);
                                                pids.Add(childP.Id);
                                                Console.WriteLine($"[{addPid}] Added");
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine($"[{addPid}] Added, Failed: {e.Message}");
                                            }
                                        else
                                            Console.WriteLine($"[{addPid}] Duplicated");
                                    }
                                    else if (input.StartsWith("remove ") &&
                                             int.TryParse(input.Replace("remove ", ""), out var remPid))
                                    {
                                        if (!pids.Contains(remPid))
                                        {
                                            Console.WriteLine($"[{remPid}] Not exists");
                                        }
                                        else
                                        {
                                            pids.Remove(remPid);
                                            Console.WriteLine($"[{remPid}] Removed");
                                        }
                                    }
                                    else if (input == "clear")
                                    {
                                        pids.Clear();
                                        Console.WriteLine("Cleared");
                                    }
                                    else if (input == "exit")
                                    {
                                        exitReq = true;
                                        Console.WriteLine("Exit requested.");
                                    }

                                    text = string.Empty;
                                }
                                else
                                {
                                    text += buf[0];
                                }
                        });

                        processWaitThread.Start();
                        inputThread.Start();

                        while (processWaitThread.IsAlive || inputThread.IsAlive)
                            Thread.Sleep(1);
                    }
                    catch
                    {
                        /* SHUTUP BITCH */
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                }

                if (!LauncherHelper.IsValid())
                    LauncherHelper.StartLauncherAndExit();
            }

            App.Mutex = new Mutex(true, "4ef9bac7-19f3-4760-ac94-7d8821f84cb6");
            if (!App.Mutex.WaitOne(TimeSpan.Zero))
                LauncherHelper.StartLauncherAndExit();
            else
                Initialize();
        }

        protected override void Configure()
        {
            try
            {
                BasicConfigurator.Configure();

                var config = new TypeMappingConfiguration
                {
                    DefaultSubNamespaceForViewModels = "UI.ViewModel",
                    DefaultSubNamespaceForViews = "UI.View",
                    IncludeViewSuffixInViewModelNames = false
                };

                ViewLocator.ConfigureTypeMappings(config);
                ViewModelLocator.ConfigureTypeMappings(config);
                EnableNestedViewModelActionBinding();

                var G = new Global();
                container = new SimpleContainer();

                container.Instance(G);
                container.Singleton<IWindowManager, MetroWindowManager>();
                container.Singleton<IEventAggregator, EventAggregator>();
                container.PerRequest<MainWindowViewModel>();
                container.PerRequest<SubtitleSettingsViewModel>();
                container.PerRequest<EncodingProgressViewModel>();
                container.PerRequest<QueueManagementViewModel>();
                container.PerRequest<TemplateSettingsViewModel>();
                container.PerRequest<TemplateVideoSettingsViewModel>();
                container.PerRequest<TemplateAudioSettingsViewModel>();
                container.PerRequest<TemplateContainerSettingsViewModel>();
                // container.PerRequest<PlayerViewModel>();

                // Load Config
                LauncherHelper.TrySendMessage("Loading configuration ...");

                G.Config = new Config($"{Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)}.json");
                G.Config.Init();

                G.Config.Application.DependencySearchPaths.Select(Path.GetFullPath).Apply(EnvironmentHelper.AddPath);

                foreach (var propertyInfo in G.Config.Application.GetType().GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public))
                {
                    if (!propertyInfo.Name.StartsWith("FF") && propertyInfo.Name.EndsWith("Path") &&
                        propertyInfo.PropertyType == typeof(string))
                        Path.GetFullPath((string)propertyInfo.GetValue(G.Config.Application)).EnsureDirectoryExists();
                }

                LauncherHelper.TrySendMessage("Loading filters ...");
                // Load Filters
                foreach (var file in Directory.GetFiles(G.Config.Application.FilterPath, "*.json"))
                    G.AvisynthFilters.Add(AvisynthFilterHelper.LoadFromFile(file));

                LauncherHelper.TrySendMessage("Loading encoders ...");
                // Load Encoders
                foreach (var file in Directory.GetFiles(G.Config.Application.EncoderPath, "*.json"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var param = (FFmpegParameter)null;
                    if (name.StartsWith("video.", StringComparison.OrdinalIgnoreCase))
                        param = FFmpegParameterHelper.LoadFromFile<FFmpegVideoParameter>(file);
                    else if (name.StartsWith("audio.", StringComparison.OrdinalIgnoreCase))
                        param = FFmpegParameterHelper.LoadFromFile<FFmpegAudioParameter>(file);
                    else if (name.StartsWith("container.", StringComparison.OrdinalIgnoreCase))
                        param = FFmpegParameterHelper.LoadFromFile<FFmpegContainerParameter>(file);

                    if (param != null)
                    {
                        param.IsEditable = !name.EndsWith(".default", StringComparison.OrdinalIgnoreCase);
                        G.FFmpegParameters.Add(param);
                    }
                }

                LauncherHelper.TrySendMessage("Loading ffmpeg ...");
                // Load FFmpeg Infomation
                G.FFmpegSupportedEncoders.AddRange(FFmpeg.GetEncoders(Path.GetFullPath(G.Config.Application.FFmpegPath)));
                G.FFmpegSupportedFormats.AddRange(FFmpeg.GetFormats(Path.GetFullPath(G.Config.Application.FFmpegPath)));

                // Check Available Encoders
                foreach (var vp in G.FFmpegParameters.OfType<FFmpegVideoParameter>())
                    vp.IsAvailable = G.FFmpegSupportedEncoders.FirstOrDefault(x =>
                                         x.Type == FFmpegEncoderTypes.Video &&
                                         x.Name == vp.Codec) != null;

                foreach (var ap in G.FFmpegParameters.OfType<FFmpegAudioParameter>())
                    ap.IsAvailable = G.FFmpegSupportedEncoders.FirstOrDefault(x =>
                                         x.Type == FFmpegEncoderTypes.Audio &&
                                         x.Name == ap.Codec) != null;

                // Check Available Formats
                foreach (var p in G.FFmpegParameters.OfType<FFmpegContainerParameter>())
                    p.IsAvailable = G.FFmpegSupportedFormats.FirstOrDefault(x => x.IsMuxer && x.Name == p.Format) != null;

                LauncherHelper.TrySendMessage("Loading template ...");
                // Load Encoder Template
                foreach (var file in Directory.GetFiles(G.Config.Application.TemplatePath, "*.json"))
                {
                    var tpl = EncoderTemplateHelper.LoadFromFile(file);
                    tpl.Filepath = file;
                    G.FFmpegTemplates.Add(tpl);
                }

                LauncherHelper.TrySendMessage("Creating empty project ...");
                // Create Empty Project
                G.ActiveProject = new AvisynthProject();

                LauncherHelper.TrySendMessage("Loading font indexer ...");
                // Load Font Index
                G.FontIndexer = new IZFontIndexer();

                int fontIndexerTryCount = 0;
                initFontIndexer:
                try
                {
                    G.FontIndexer.Init(Path.Combine(G.Config.Application.TempPath.EnsureDirectoryExists(), "fonts.izf"));
                }
                catch
                {
                    if (++fontIndexerTryCount >= 3)
                        throw;

                    try { File.Delete(Path.Combine(G.Config.Application.TempPath.EnsureDirectoryExists(), "fonts.izf")); } catch { /* SHUT-UP */ }
                    goto initFontIndexer;
                }

                LauncherHelper.TrySendMessage("Creating global message window ...");
                G.GlobalWin32Message = new GlobalWin32Message("IZEncoderMessageWindow");

                LauncherHelper.TrySendMessage("Creating child process ...");
                G.ChildProcessKiller = new IZChildProcessKiller();

                // Load Encoding Queue
                G.EncodingQueueProcessor = new EncodingQueueProcessor(Path.Combine(
                    G.Config.Application.TempPath.EnsureDirectoryExists(),
                    "queues.izdb"), G.Config, G.ChildProcessKiller);

                LauncherHelper.TrySendMessage("Starting ...");
            }
            catch(Exception e)
            {
                File.WriteAllText("last_error.txt", $"{LauncherHelper.TrySendException(e, "An error occurred while starting application")}\n{e.Message}\n{e.StackTrace}");
                throw;                
            }
        }

        protected override object GetInstance(Type service, string key)
        {
            return container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }
        
        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
        {
            try
            {
                FrameworkElement.StyleProperty.OverrideMetadata(typeof(MetroWindow), new FrameworkPropertyMetadata
                {
                    DefaultValue = Application.FindResource("DefaultMetroWindowStyle")
                });


                IoC.Get<Global>().GlobalWin32Message.MessageHandler += (hwnd, msg, param, lParam) =>
                {
                    if (msg == App.MessageBringToFront)
                    {
                        foreach (Window applicationWindow in Application.Windows)
                        {
                            if (applicationWindow.DataContext is MainWindowViewModel mvm)
                                mvm.BringToFront();
                        }
                    }
                };

                ThemeManager.AddAppTheme("IZEDark", new Uri("/UI/IZEDark.xaml", UriKind.Relative));
                DisplayRootViewFor<MainWindowViewModel>();
            }
            catch (Exception ex)
            {
                LauncherHelper.TrySendException(ex, "An error occurred while starting application");
                throw;
            }
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            var G = IoC.Get<Global>();

            G?.Config?.Save();
            G?.EncodingQueueProcessor?.Dispose();
            G?.ChildProcessKiller?.Dispose();
        }
        
        private static void KillProcessAndChildren(int pid)
        {
            // Cannot close 'system idle process'.
            if (pid == 0) return;
            var searcher = new ManagementObjectSearcher
                ("Select * From Win32_Process Where ParentProcessID=" + pid);
            var moc = searcher.Get();
            foreach (ManagementObject mo in moc) KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch
            {
                // SHUTUP BITCH
            }
        }

        #region Caliburn.Micro NestedViewModelActionBinding
        // Source: https://stackoverflow.com/a/33005816
        private static void EnableNestedViewModelActionBinding()
        {
            var baseGetTargetMethod = ActionMessage.GetTargetMethod;
            ActionMessage.GetTargetMethod = (message, target) =>
            {
                var methodName = GetRealMethodName(message.MethodName, ref target);
                if (methodName == null)
                    return null;

                var fakeMessage = new ActionMessage { MethodName = methodName };
                foreach (var p in message.Parameters)
                    fakeMessage.Parameters.Add(p);
                return baseGetTargetMethod(fakeMessage, target);
            };

            var baseSetMethodBinding = ActionMessage.SetMethodBinding;
            ActionMessage.SetMethodBinding = context =>
            {
                baseSetMethodBinding(context);
                var target = context.Target;
                if (target != null)
                {
                    GetRealMethodName(context.Message.MethodName, ref target);
                    context.Target = target;
                }
            };
        }

        private static string GetRealMethodName(string methodName, ref object target)
        {
            var parts = methodName.Split('.');
            var model = target;
            foreach (var propName in parts.Take(parts.Length - 1))
            {
                if (model == null)
                    return null;

                var prop = model.GetType().GetPropertyCaseInsensitive(propName);
                if (prop == null || !prop.CanRead)
                    return null;

                model = prop.GetValue(model);
            }
            target = model;
            return parts.Last();
        }
        #endregion
    }
}
