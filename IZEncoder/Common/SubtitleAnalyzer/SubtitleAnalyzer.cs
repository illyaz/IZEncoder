namespace IZEncoder.Common.SubtitleAnalyzer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ASSParser;
    using FontIndexer;
    using Newtonsoft.Json;

    internal class SubtitleAnalyzer
    {
        private readonly IZFontIndexer _fontIndexer;
        private readonly bool _missingFontCheck;
        private readonly bool _missingStyleCheck;
        private readonly string _path;

        public SubtitleAnalyzer(string path, IZFontIndexer fontIndexer, bool missingFontCheck = true,
            bool missingStyleCheck = true)
        {
            _path = path;
            _fontIndexer = fontIndexer;
            _missingFontCheck = missingFontCheck;
            _missingStyleCheck = missingStyleCheck;
        }

        public event EventHandler<SubtitleAnalysisStatus> StatusChanged;

        public async Task<SubtitleAnalysisResult> RunAsync()
        {
            var assParser = (AssParser) null;
            var result = new SubtitleAnalysisResult();

            await Task.Factory.StartNew(() =>
            {
                try
                {
                    StatusChanged?.Invoke(this, new SubtitleAnalysisStatus
                    {
                        Percent = -1,
                        Message = "Initializing Subtitle Parser ..."
                    });

                    assParser = new AssParser(_path);
                    assParser.PercentChanged += PercentChanged;
                    assParser.Parse();
                    result.HasMod = assParser.IsVSFilterMOD;

                    if (_missingFontCheck)
                    {
                        foreach (var style in assParser.Subtitle.StyleSet.Where(x =>
                            assParser.Subtitle.EventCollection.Any(w => !w.IsComment && x.Name == w.Style)))
                        {
                            StatusChanged?.Invoke(this, new SubtitleAnalysisStatus
                            {
                                Percent = -1,
                                Message = $"Finding font: {style.FontName}"
                            });

                            if (!_fontIndexer.Find(style.FontName.TrimStart('@')).Any())
                                result.MissingFontStyles.Add(DeepCopy(style));
                        }

                        foreach (var assExtraFont in assParser.ExtraFonts)
                        {
                            StatusChanged?.Invoke(this, new SubtitleAnalysisStatus
                            {
                                Percent = -1,
                                Message = $"Finding font: {assExtraFont.Key}"
                            });

                            if (_fontIndexer.Find(assExtraFont.Key.TrimStart('@')).Any())
                                continue;

                            if (!result.MissingInlineFonts.ContainsKey(assExtraFont.Key))
                                result.MissingInlineFonts.Add(assExtraFont.Key, new List<int>());

                            result.MissingInlineFonts[assExtraFont.Key].AddRange(assExtraFont.Value);
                        }
                    }

                    if (_missingStyleCheck)
                    {
                        var eventCount = 0;

                        StatusChanged?.Invoke(this, new SubtitleAnalysisStatus
                        {
                            Percent = -1,
                            Message = "Finding missing style ..."
                        });

                        foreach (var subEvent in assParser.Subtitle.EventCollection)
                        {
                            eventCount++;

                            if (subEvent.IsComment
                                || subEvent.StartTime - subEvent.EndTime == TimeSpan.Zero)
                                continue;

                            if (assParser.Subtitle.StyleSet.ContainsName(subEvent.Style))
                                continue;

                            if (!result.MissingStyles.ContainsKey(subEvent.Style))
                                result.MissingStyles.Add(subEvent.Style, new List<int>());

                            result.MissingStyles[subEvent.Style].Add(eventCount);
                        }
                    }
                }
                catch (Exception e)
                {
                    result.Exception = e;
                }
                finally
                {
                    assParser?.Dispose();
                }
            });

            return result;
        }

        private void PercentChanged(object sender, double e)
        {
            var assParser = (AssParser) sender;
            StatusChanged
                ?.Invoke(this, new SubtitleAnalysisStatus {IsParsing = assParser.IsParsing, Percent = e});
        }

        private T DeepCopy<T>(T o)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(o));
        }
    }

    internal struct SubtitleAnalysisStatus
    {
        public bool IsParsing { get; set; }
        public double Percent { get; set; }
        public string Message { get; set; }
    }

    public class SubtitleAnalysisResult
    {
        public List<Style> MissingFontStyles { get; set; } = new List<Style>();
        public Dictionary<string, List<int>> MissingStyles { get; set; } = new Dictionary<string, List<int>>();
        public Dictionary<string, List<int>> MissingInlineFonts { get; set; } = new Dictionary<string, List<int>>();
        public Exception Exception { get; set; }
        public bool HasMod { get; set; }

        public bool HasError => Exception != null ||
                                MissingFontStyles.Count != 0 ||
                                MissingStyles.Count != 0 ||
                                MissingInlineFonts.Count != 0;
    }
}