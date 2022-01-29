namespace IZEncoder.Launcher
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Threading;
    using System.Windows;
    using Caliburn.Micro;
    using Common.Client;
    using IZEncoder.Launcher.Common.Helper;
    using UI.ViewModel;

    public class AppBootstrapper : BootstrapperBase
    {

        public static Mutex LauncherMutex = new Mutex(true, "3a98582e-33ad-4c03-b545-5e9a0c6454e1");

        SimpleContainer container;

        public AppBootstrapper()
        {
            var args = EnvironmentHelper.GetCommandLineArgs();
            var i = 0;

            try
            {
                if (args.ContainsKey("update") && int.TryParse(args["update"], out var pid))
                {
                    var p = Process.GetProcessById(pid);
                    var path = p.MainModule.FileName;
                    p.Kill();

                    while (!p.HasExited)
                        Thread.Sleep(1);

                    Thread.Sleep(1000);
                    using (var updtStream = File.OpenRead(Assembly.GetExecutingAssembly().Location))
                    {
                        i++;
                        using (var targetStream = File.OpenWrite(path))
                        {
                            targetStream.SetLength(0);
                            updtStream.CopyTo(targetStream);
                        }
                    }

                    Process.Start(path, $"--update_success {Process.GetCurrentProcess().Id}");
                    Environment.Exit(0);
                }
                else if (args.ContainsKey("update_success") && int.TryParse(args["update_success"], out var pids))
                {
                    try
                    {
                        var p = Process.GetProcessById(pids);
                        while (!p.HasExited)
                            Thread.Sleep(1);
                    }
                    catch
                    {
                        /* ignored */
                    }

                    if (File.Exists($"{Assembly.GetExecutingAssembly().Location}.updt"))
                        File.Delete($"{Assembly.GetExecutingAssembly().Location}.updt");
                }
                else if (!LauncherMutex.WaitOne(TimeSpan.Zero, true))
                    Environment.Exit(0);
                else if (Mutex.TryOpenExisting("4ef9bac7-19f3-4760-ac94-7d8821f84cb6", out _))
                {
                    LauncherClient.BringToFront().Wait();
                    Environment.Exit(0);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(i + "\n" + e.Message + "\n" + e.StackTrace);
                Environment.Exit(0);
            }

            Initialize();
        }

        protected override void Configure()
        {
            var config = new TypeMappingConfiguration
            {
                DefaultSubNamespaceForViewModels = "UI.ViewModel",
                DefaultSubNamespaceForViews = "UI.View",
                IncludeViewSuffixInViewModelNames = false
            };

            ViewLocator.ConfigureTypeMappings(config);
            ViewModelLocator.ConfigureTypeMappings(config);

            container = new SimpleContainer();
            var g = new Global();

            container.Singleton<IWindowManager, WindowManager>();
            container.Singleton<IEventAggregator, EventAggregator>();
            container.PerRequest<MainViewModel>();
            container.Instance(g);
            if(File.Exists("ize.update.server.override"))
                container.Instance(new LauncherClient(File.ReadAllText("ize.update.server.override")));
            else
                container.Instance(new LauncherClient("https://raw.githubusercontent.com/illyaz/IZEncoder/master/build/publish/"));
            g.GlobalWin32Message = new GlobalWin32Message();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
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
            DisplayRootViewFor<MainViewModel>();
        }
    }
}