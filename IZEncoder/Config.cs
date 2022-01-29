namespace IZEncoder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using Common;
    using Common.AvisynthFilter;
    using Newtonsoft.Json;

    public class Config : PropertyChangedBaseJson
    {
        private readonly string _path;
        private bool _isLoaded;

        public Config(string path)
        {
            _path = path;
        }

        public ApplicationEntry Application { get; set; }
        public MainEntry Main { get; set; }
        public QueueManagementEntry QueueManagement { get; set; }
        public EncodingProcessEntry EncodingProcess { get; set; }
        public SubtitleEntry Subtitle { get; set; }

        public void Init()
        {
            if (File.Exists(_path))
                JsonConvert.PopulateObject(File.ReadAllText(_path), this);
            else
                Reset();

            foreach (var prop in GetType().GetProperties()
                .Where(x => typeof(ConfigEntry).IsAssignableFrom(x.PropertyType)))
                if (prop.GetValue(this) == null)
                    prop.SetValue(this, Activator.CreateInstance(prop.PropertyType));

            Save();
            _isLoaded = true;
        }

        public void Save()
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public void Reset()
        {
            foreach (var prop in GetType().GetProperties()
                .Where(x => typeof(ConfigEntry).IsAssignableFrom(x.PropertyType)))
            {
                if (prop.GetValue(this) == null)
                    prop.SetValue(this, Activator.CreateInstance(prop.PropertyType));

                if (prop.GetValue(this) is ConfigEntry ce)
                    ce.Reset();
            }
        }

        public class ApplicationEntry : ConfigEntry
        {
            public bool ShowTitleBar { get; set; }
            public bool UseDarkTheme { get; set; }
            public string TempPath { get; set; }
            public string FFmpegPath { get; set; }
            public string FFprobePath { get; set; }
            public List<string> DependencySearchPaths { get; set; } = new List<string>();
            public bool PrefetchEnabled { get; set; } = false;
            public int PrefetchThreads { get; set; } = -1;
            public int PrefetchFrames { get; set; } = -1;

            [JsonIgnore]
            public string TemplatePath { get; set; } = "template";

            [JsonIgnore]
            public string EncoderPath { get; set; } = "encoder";

            [JsonIgnore]
            public string AvisynthPath { get; set; } = "avisynth";

            [JsonIgnore]
            public string FilterPath { get; set; } = "filter";

            [JsonIgnore]
            public string LibraryPath { get; set; } = "lib";
            
            public override void Reset()
            {
                ShowTitleBar = UseDarkTheme = false;
                TempPath = "temp";
                FFmpegPath = "ffmpeg\\ffmpeg.exe";
                FFprobePath = "ffmpeg\\ffprobe.exe";
                DependencySearchPaths.Add(AvisynthPath);
                DependencySearchPaths.Add(FilterPath);
                DependencySearchPaths.Add(LibraryPath);
                PrefetchEnabled = false;
                PrefetchThreads = PrefetchFrames = -1;
            }
        }

        public class MainEntry : ConfigEntry
        {
            public Guid SelectedEncoderTemplate { get; set; }
            public EncoderTemplate LastEncoderTemplate { get; set; }

            public override void Reset()
            {
                SelectedEncoderTemplate = Guid.Empty;
            }
        }

        public class QueueManagementEntry : ConfigEntry, IWindowPosition, IWindowSize, IWindowState
        {
            public Point? WindowPosition { get; set; }
            public Size? WindowSize { get; set; }
            public WindowState? WindowState { get; set; }

            public override void Reset()
            {
                WindowPosition = null;
                WindowSize = null;
                WindowState = null;
            }
        }

        public class EncodingProcessEntry : ConfigEntry, IWindowPosition
        {
            public Point? WindowPosition { get; set; }
            public bool AttachToMainWindow { get; set; }

            public override void Reset()
            {
                WindowPosition = null;
                AttachToMainWindow = false;
            }
        }

        public class SubtitleEntry : ConfigEntry
        {
            public SubtitleAnalyzerEntry Analyzer { get; set; } = new SubtitleAnalyzerEntry();
            public SubtitleVSFilterEntry VSFilter { get; set; } = new SubtitleVSFilterEntry();
            public SubtitleVSFilterEntry VSFilterMod { get; set; } = new SubtitleVSFilterEntry();
            public bool VSFilterModMT { get; set; } = false;

            public override void Reset()
            {
                Analyzer.Reset();
                VSFilter.Reset();
                VSFilterMod.Reset();

                VSFilter.Path = @"avisynth\VSFilter.dll";
                VSFilter.FunctionName = "TextSub";
                VSFilterMod.Path = @"avisynth\VSFilterMod.dll";
                VSFilterMod.FunctionName = "TextSubMod";
                VSFilterModMT = false;
            }

            public class SubtitleAnalyzerEntry : ConfigEntry
            {
                public bool Enabled { get; set; }
                public bool MissingFont { get; set; }
                public bool MissingStyle { get; set; }

                public override void Reset()
                {
                    Enabled = true;
                    MissingFont = true;
                    MissingStyle = true;
                }
            }

            public class SubtitleVSFilterEntry : ConfigEntry
            {
                public string Path { get; set; }
                public string FunctionName { get; set; }
                public FilterMtModes MtMode { get; set; }

                public override void Reset()
                {
                    Path = null;
                    FunctionName = null;
                    MtMode = FilterMtModes.Serialized;
                }
            }
        }

        public abstract class ConfigEntry : PropertyChangedBaseJson
        {
            public event EventHandler<ConfigChangedArgs> OnConfigChanged;
            public abstract void Reset();

            public void OnPropertyChanged(string propertyName, object before, object after)
            {
                OnConfigChanged?.Invoke(this, new ConfigChangedArgs(propertyName, before, after));
            }
        }

        public class ConfigChangedArgs
        {
            public ConfigChangedArgs(string name, object before, object after)
            {
                Name = name;
                Before = before;
                After = after;
            }

            public string Name { get; }
            public object Before { get; }
            public object After { get; }
        }

        public interface IWindowPosition
        {
            Point? WindowPosition { get; set; }
        }

        public interface IWindowSize
        {
            Size? WindowSize { get; set; }
        }

        public interface IWindowState
        {
            WindowState? WindowState { get; set; }
        }
    }
}