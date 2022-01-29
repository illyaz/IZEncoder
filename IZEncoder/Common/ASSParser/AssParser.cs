namespace IZEncoder.Common.ASSParser
{
    using System;
    using System.Linq;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Collections;

    public class AssParser : IDisposable
    {
        private readonly MyStreamReader assReader;
        public EventHandler<double> PercentChanged;
        public int PercentChangedInterval = 10;

        public AssParser(string file)
        {
            var sw = Stopwatch.StartNew();
            assReader = new MyStreamReader(file);
            //if (assReader.Read() != 65279) // Unicode Character 'ZERO WIDTH NO-BREAK SPACE' (U+FEFF)
            //    assReader.BaseStream.Position = 0;

            //var l = assReader.Peek();
            assReader.LineChanged += (s, e) =>
            {
                if (!(sw.Elapsed.TotalMilliseconds > PercentChangedInterval) && e != assReader.LineCount)
                    return;

                PercentChanged?.Invoke(this, 100 * (double) e / assReader.LineCount);
                sw.Restart();
            };
        }

        public Dictionary<string, List<int>> ExtraFonts { get; } = new Dictionary<string, List<int>>();
        public bool IsVSFilterMOD { get; private set; }

        public bool IsParsing { get; private set; } = true;
        public Subtitle<AssScriptInfo> Subtitle { get; private set; }

        public void Dispose()
        {
            Subtitle?.ScriptInfo?.Clear();
            Subtitle?.StyleSet?.Clear();
            Subtitle?.EventCollection?.Clear();
            assReader?.Dispose();
            GC.Collect();
            //GC.WaitForPendingFinalizers();
        }

        public void Parse()
        {
            var modTags = 0;
            var line = 0;
            Subtitle = ASSParser.Subtitle.Parse<AssScriptInfo>(assReader);
            if (Subtitle.EventCollection.Count == 0 && Subtitle.StyleSet.Count == 0)
                throw new InvalidOperationException("This is not valid ASS/SSA Subtitle");
            IsParsing = false;
            //foreach (var ev in Subtitle.EventCollection)
            var sw = Stopwatch.StartNew();
            var rex = new Regex(
                "^(fsc(?!.*x|y)|fsvp|frs|z|distort|rnd|(1|2|3|4)vc|(1|2|3|4)va|(1|2|3|4)img|move(r|s3|vc)|jitter)",
                RegexOptions.IgnoreCase);
            var rangePartitioner = Partitioner.Create(0, Subtitle.EventCollection.Count,
                Math.Min(5000, Subtitle.EventCollection.Count));
            Parallel.ForEach(rangePartitioner, range =>
            {
                for (var i = range.Item1; i < range.Item2; i++)
                    try
                    {
                        Interlocked.Increment(ref line);
                        var ev = Subtitle.EventCollection[i];
                        if (ev.IsComment || (ev.EndTime - ev.StartTime).TotalMilliseconds == 0)
                            continue;

                        foreach (var textTag in ev.Text.Tags)
                            foreach (var tag in GetSplit(textTag, '\\'))
                                if (tag.StartsWith("fn", StringComparison.OrdinalIgnoreCase))
                                {                                   
                                    var fn = tag.Substring(2);

                                    if (fn[0] == '(')
                                        fn = fn.Substring(1);

                                    if (fn[fn.Length - 1] == ')')
                                        fn = fn.Substring(0, fn.Length - 1);

                                    lock (ExtraFonts)
                                    {
                                        if (!ExtraFonts.ContainsKey(fn))
                                            ExtraFonts.Add(fn, new List<int>());

                                        ExtraFonts[fn].Add(i);
                                    }
                                }
                                else if (rex.IsMatch(tag))
                                    modTags++;                                
                    }
                    finally
                    {
                        if (sw.Elapsed.TotalMilliseconds > PercentChangedInterval ||
                            line == Subtitle.EventCollection.Count)
                        {
                            PercentChanged?.Invoke(this, 100 * (double) line / Subtitle.EventCollection.Count);
                            sw.Restart();
                        }
                    }
            });

            PercentChanged?.Invoke(this, 100);
            IsVSFilterMOD = modTags > 0;
        }

        private static IEnumerable<string> GetSplit(string s, char c)
        {
            var l = s.Length;
            int i = 0, j = s.IndexOf(c, 0, l);
            if (j == -1) // No such substring
            {
                yield return s; // Return original and break
                yield break;
            }

            while (j != -1)
            {
                if (j - i > 0)                          // Non empty? 
                    yield return s.Substring(i, j - i); // Return non-empty match
                i = j + 1;
                j = s.IndexOf(c, i, l - i);
            }

            if (i < l)                              // Has remainder?
                yield return s.Substring(i, l - i); // Return remaining trail
        }

        private class MyStreamReader : StreamReader
        {
            public EventHandler<int> LineChanged;

            public MyStreamReader(string path)
                : base(path)
            {
                while (base.ReadLine() != null) LineCount++;
                BaseStream.Position = 0;
            }

            public int LineCount { get; }

            public int CurrentLine { get; private set; } = -1;

            public override string ReadLine()
            {
                try
                {
                    return base.ReadLine();
                }
                finally
                {
                    CurrentLine++;
                    LineChanged?.Invoke(this, CurrentLine);
                }
            }
        }
    }
}