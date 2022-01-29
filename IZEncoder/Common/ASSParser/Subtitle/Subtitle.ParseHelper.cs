namespace IZEncoder.Common.ASSParser
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Collections;

    public static partial class Subtitle
    {
        private class ParseHelper<T> where T : ScriptInfoCollection
        {
            private readonly bool isExact;

            private readonly TextReader reader;

            private readonly Subtitle<T> subtitle;

            private EntryHeader styleFormat, eventFormat;

            public ParseHelper(TextReader reader, bool isExact, Func<T> factory)
            {
                this.reader = reader;
                this.isExact = isExact;
                subtitle = new Subtitle<T>(factory());
            }

            public Subtitle<T> GetResult()
            {
                var lineNumber = 0;
                string line = null;
                var sec = isExact ? Section.Unknown : Section.ScriptInfo;
                string secStr = null;
                try
                {
                    while (true)
                    {
                        line = reader.ReadLine();
                        lineNumber++;
                        if (line == null)
                            return subtitle;

                        var temp = line.Trim();

                        // Skip empty lines and comment lines.
                        if (temp.Length == 0 || temp[0] == ';')
                            continue;

                        if (temp[0] == '[' && temp[temp.Length - 1] == ']') // Section header
                            switch (temp.ToLower())
                            {
                                case "[script info]":
                                case "[scriptinfo]":
                                    sec = Section.ScriptInfo;
                                    break;
                                case "[v4+ styles]":
                                case "[v4 styles+]":
                                case "[v4+styles]":
                                case "[v4styles+]":
                                    sec = Section.Styles;
                                    break;
                                case "[events]":
                                    sec = Section.Events;
                                    break;
                                default:
                                    sec = Section.Unknown;
                                    secStr = temp.Substring(1, temp.Length - 2);
                                    //if(this.isExact)
                                    //    throw new InvalidOperationException($"Unknown section \"{secStr}\" found.");
                                    break;
                            }
                        else // Section content
                            switch (sec)
                            {
                                case Section.ScriptInfo:
                                    initScriptInfo(temp);
                                    break;
                                case Section.Styles:
                                    initStyle(temp);
                                    break;
                                case Section.Events:
                                    initEvent(temp);
                                    break;
                                case Section.Unknown:
                                    break;
                                default:
                                    if (isExact)
                                        throw new InvalidOperationException("Content found without a section header.");
                                    break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    var exception = new ArgumentException($@"Error occurs during parsing.
Line number: {lineNumber}
Content of the line:
{line}", ex);
                    exception.Data.Add("Line number", lineNumber);
                    exception.Data.Add("Line content", line);
                    exception.Data.Add("Current section", sec.ToString());
                    throw exception;
                }
            }

            public Task<Subtitle<T>> GetResultAsync()
            {
                return Task.Run(() => GetResult());
            }

            private void initScriptInfo(string scriptInfoLine)
            {
                if (isExact)
                    subtitle.ScriptInfo.ParseLineExact(scriptInfoLine);
                else
                    subtitle.ScriptInfo.ParseLine(scriptInfoLine);
            }

            private void initStyle(string styleLine)
            {
                if (FormatHelper.TryPraseLine(out var key, out var value, styleLine))
                    switch (key.ToLower())
                    {
                        case "format":
                            styleFormat = new EntryHeader(value);
                            return;
                        case "style":
                            if (styleFormat == null)
                                styleFormat = DefaultStyleFormat;
                            Style s;
                            if (isExact)
                            {
                                s = Style.ParseExact(styleFormat, value);
                                if (subtitle.StyleSet.ContainsName(s.Name))
                                    throw new ArgumentException(
                                        $"Style with the name \"{s.Name}\" is already in the StyleSet.");
                            }
                            else
                            {
                                s = Style.Parse(styleFormat, value);
                            }

                            subtitle.StyleSet.Add(s);
                            return;
                        default:
                            return;
                    }
            }

            private void initEvent(string eventLine)
            {
                if (FormatHelper.TryPraseLine(out var key, out var value, eventLine))
                {
                    if (string.Equals(key, "format", StringComparison.OrdinalIgnoreCase))
                    {
                        eventFormat = new EntryHeader(value);
                    }
                    else
                    {
                        if (eventFormat == null)
                            eventFormat = DefaultEventFormat;
                        var sub = isExact
                            ? SubEvent.ParseExact(eventFormat,
                                string.Equals(key, "comment", StringComparison.OrdinalIgnoreCase), value)
                            : SubEvent.Parse(eventFormat,
                                string.Equals(key, "comment", StringComparison.OrdinalIgnoreCase), value);
                        subtitle.EventCollection.Add(sub);
                    }
                }
            }

            private enum Section
            {
                Unknown = 0,
                ScriptInfo,
                Styles,
                Events
            }
        }
    }
}