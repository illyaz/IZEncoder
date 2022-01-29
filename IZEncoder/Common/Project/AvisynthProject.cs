namespace IZEncoder.Common.Project
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using AvisynthFilter;
    using Caliburn.Micro;
    using Helper;

    public class AvisynthProject : PropertyChangedBaseJson
    {
        public AvisynthProject()
        {
            Name = "Unnamed project";
            Guid = Guid.NewGuid();
            Filters.Add(new AvisynthProjectFilter
            {
                Name = "Subtitle",
                FilterName = "__SUBTITLE__",
                FilterGuid = Guid.Empty
            });
        }

        public Guid Guid { get; set; }
        public string Name { get; set; }

        public MediaInfomation Input { get; set; }
        public int? InputAudioTrack { get; set; }

        public BindableCollection<AvisynthSubtitle> Subtitles { get; set; } =
            new BindableCollection<AvisynthSubtitle>();

        public BindableCollection<AvisynthProjectFilter> Filters { get; set; } =
            new BindableCollection<AvisynthProjectFilter>();

        public Guid VideoEncoder { get; set; }
        public Dictionary<string, object> VideoEncoderSettings { get; set; } = new Dictionary<string, object>();

        public Guid AudioEncoder { get; set; }
        public Dictionary<string, object> AudioEncoderSettings { get; set; } = new Dictionary<string, object>();

        public Guid Container { get; set; }
        public Dictionary<string, object> ContainerSettings { get; set; } = new Dictionary<string, object>();

        public string Output { get; set; }


        public string ToScript(Config config, IEnumerable<AvisynthFilter> filterCollection)
        {
            var script = new StringBuilder();
            var deps = new List<string>
            {
                DependencySearcher.Search("IZUnicodeAvisynth.dll",
                    config.Application.DependencySearchPaths.Select(Path.GetFullPath)),
                DependencySearcher.Search("ffms2.dll",
                    config.Application.DependencySearchPaths.Select(Path.GetFullPath))
            };
                     
            var mts = new Dictionary<string, FilterMtModes>
            {
                { "DEFAULT_MT_MODE", FilterMtModes.MultiInstance },
                { "FFVideoSource", FilterMtModes.Serialized },
                { "FFAudioSource", FilterMtModes.Serialized },
                { "FFMS2", FilterMtModes.Serialized },
            };
          
            var avisynthFilters = filterCollection as AvisynthFilter[] ?? filterCollection.ToArray();
            foreach (var pfilter in Filters)
                if (pfilter.FilterGuid == Guid.Empty && pfilter.FilterName == "__SUBTITLE__")
                {
                    if (Subtitles.Any(x => !x.IsMod))
                    {
                        deps.Add(DependencySearcher.Search(config.Subtitle.VSFilter.Path,
                            config.Application.DependencySearchPaths.Select(Path.GetFullPath)));
                        if (!mts.ContainsKey(config.Subtitle.VSFilter.FunctionName))
                            mts.Add(config.Subtitle.VSFilter.FunctionName, config.Subtitle.VSFilter.MtMode);

                        mts[config.Subtitle.VSFilter.FunctionName] = config.Subtitle.VSFilter.MtMode;
                    }

                    if (Subtitles.Any(x => x.IsMod))
                    {
                        if (config.Subtitle.VSFilterModMT)
                            deps.Add(DependencySearcher.Search("IZTextSubMT.dll",
                                config.Application.DependencySearchPaths.Select(Path.GetFullPath)));
                        else
                        {
                            deps.Add(DependencySearcher.Search(config.Subtitle.VSFilterMod.Path,
                                config.Application.DependencySearchPaths.Select(Path.GetFullPath)));
                            if (!mts.ContainsKey(config.Subtitle.VSFilterMod.FunctionName))
                                mts.Add(config.Subtitle.VSFilterMod.FunctionName, config.Subtitle.VSFilterMod.MtMode);

                            mts[config.Subtitle.VSFilterMod.FunctionName] = config.Subtitle.VSFilterMod.MtMode;
                        }

                    }
                }
                else
                {
                    var filter = avisynthFilters.FirstOrDefault(x => x.Guid == pfilter.FilterGuid);
                    if (filter == null)
                        throw new Exception($"Could not find filter '[{pfilter.FilterGuid}] {pfilter.FilterName}'");

                    deps.AddRange(filter.Dependencies.Select(x =>
                        DependencySearcher.Search(x,
                            config.Application.DependencySearchPaths.Select(Path.GetFullPath))));

                    foreach (var mt in filter.MtModes)
                    {
                        if (!mts.ContainsKey(mt.Key))
                            mts.Add(mt.Key, mt.Value);

                        mts[mt.Key] = mt.Value;
                    }
                }

            deps = deps.Distinct().ToList();
            deps.Apply(x => script.AppendLine(GetLoadPluginOrImportString(x)));
            if (Subtitles.Any(x => x.IsMod) && config.Subtitle.VSFilterModMT)
                script.AppendLine(string.Format("IZTextSubMT_Configure(\"{0}\", \"{1}\")", config.Subtitle.VSFilterMod.FunctionName, 
                    DependencySearcher.Search(config.Subtitle.VSFilterMod.Path, config.Application.DependencySearchPaths.Select(Path.GetFullPath))));
            script.AppendLine();
            if(config.Application.PrefetchEnabled)
                mts.Apply(x => script.AppendLine($"SetFilterMTMode(\"{x.Key}\", {x.Value.ToAvisynthPlus()})"));
            script.AppendLine();
            script.AppendLine($"Source = {GetSourceEvalString(config, false)}");
            script.AppendLine("Filtered = Source");

            foreach (var pfilter in Filters)
                if (pfilter.FilterGuid == Guid.Empty && pfilter.FilterName == "__SUBTITLE__")
                {
                    foreach (var subtitle in Subtitles)
                    {                        
                        if (subtitle.IsMod)
                        {
                            if (config.Subtitle.VSFilterModMT)
                                script.AppendLine($"Filtered = Filtered.IZTextSubMT(\"{config.Subtitle.VSFilterMod.FunctionName}\", \"{subtitle.Filename}\")");
                            else
                                script.AppendLine($"Filtered = Filtered.{config.Subtitle.VSFilterMod.FunctionName}(\"{subtitle.Filename}\")");
                        }
                        else
                            script.AppendLine($"Filtered = Filtered.{config.Subtitle.VSFilter.FunctionName}(\"{subtitle.Filename}\")");
                    }
                }
                else
                {
                    var filter = avisynthFilters.FirstOrDefault(x => x.Guid == pfilter.FilterGuid);
                    if (filter == null)
                        throw new Exception($"Could not find filter '[{pfilter.FilterGuid}] {pfilter.FilterName}'");

                    script.AppendLine($"Filtered = Filtered.{filter.GetEvalString(pfilter.Parameters)}");
                }

            script.AppendLine("Filtered");
            if (config.Application.PrefetchEnabled)
                script.AppendLine($"Prefetch({(config.Application.PrefetchThreads > 0 ? config.Application.PrefetchThreads : Environment.ProcessorCount)})");
            return script.ToString();
        }

        public string GetSourceEvalString(Config config, bool ignoreIndex = true, bool forceYv12 = true)
        {
            if (Input == null)
                return null;

            if (Input.FileExtension.Equals("avs", StringComparison.OrdinalIgnoreCase))
                return $"Import(\"{Input.Filename}\").ConvertBits(8).ConvertToYV12()";

            var ffms2 = $"FFMS2(\"{Input.Filename}\", atrack={InputAudioTrack ?? -1}";

            if (!ignoreIndex)
                ffms2 += $", cachefile=\"{Input.GetFFIndexPath(config)}\"";

            if (Input.VideoTracks[0].IsVfr)
                ffms2 += $", fpsnum={Input.VideoTracks[0].FrameRate * 10000:00000}, fpsden=10000";
            else
                ffms2 += $", fpsnum={Input.VideoTracks[0].FrameRate * 1000:00000}, fpsden=1000";

            ffms2 += ")";

            return forceYv12 ? ffms2 + ".ConvertBits(8).ConvertToYV12()" : ffms2;
        }

        private string GetLoadPluginOrImportString(string path)
        {
            if (path.EndsWith(".avsi"))
                return $"Import(\"{path}\")";

            return $"LoadPlugin(\"{path}\")";
        }
    }

    public class AvisynthProjectFilter : PropertyChangedBaseJson
    {
        public string Name { get; set; }
        public Guid Guid { get; set; } = Guid.NewGuid();
        public string FilterName { get; set; }
        public Guid FilterGuid { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public void SetFilter(AvisynthFilter filter)
        {
            FilterName = filter.Name;
            FilterGuid = filter.Guid;
        }
    }
}