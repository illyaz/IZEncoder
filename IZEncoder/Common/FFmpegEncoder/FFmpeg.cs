namespace IZEncoder.Common.FFmpegEncoder
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    public static class FFmpeg
    {
        public static IEnumerable<FFmpegEncoder> GetEncoders(string execPath)
        {
            var startInfo = new ProcessStartInfo(execPath, "-encoders")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            var p = Process.Start(startInfo);
            if (p == null)
                throw new InvalidOperationException("Process not started");

            var lines = Regex.Split(p.StandardOutput.ReadToEnd(), "\r\n|\r|\n");

            for (var i = 0; i < lines.Length; i++)
                if (i == 0)
                {
                    if (!lines[i].StartsWith("Encoders:"))
                        throw new FormatException("FFmpeg output is invalid");

                    i = 9; // Skip 
                }
                else
                {
                    var line = lines[i];
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var encoder = new FFmpegEncoder();

                    switch (line[1])
                    {
                        case 'V':
                            encoder.Type = FFmpegEncoderTypes.Video;
                            break;
                        case 'A':
                            encoder.Type = FFmpegEncoderTypes.Audio;
                            break;
                        case 'S':
                            encoder.Type = FFmpegEncoderTypes.Subtitle;
                            break;
                        default:
                            throw new FormatException("FFmpeg output is invalid");
                    }

                    if (line[2] == 'F')
                        encoder.FrameLevelMultiThreading = true;

                    if (line[3] == 'S')
                        encoder.SliceLevelMultiThreading = true;

                    if (line[4] == 'X')
                        encoder.IsExperimental = true;

                    for (var j = 8; j < line.Length; j++)
                    {
                        var c = line[j];

                        if (char.IsWhiteSpace(c))
                        {
                            encoder.Description = line.Substring(j).Trim();
                            break;
                        }

                        encoder.Name += c;
                    }

                    yield return encoder;
                }
        }

        public static IEnumerable<FFmpegFormat> GetFormats(string execPath)
        {
            var startInfo = new ProcessStartInfo(execPath, "-formats")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            var p = Process.Start(startInfo);
            if (p == null)
                throw new InvalidOperationException("Process not started");

            var lines = Regex.Split(p.StandardOutput.ReadToEnd(), "\r\n|\r|\n");

            for (var i = 0; i < lines.Length; i++)
                if (i == 0)
                {
                    if (!lines[i].StartsWith("File formats:"))
                        throw new FormatException("FFmpeg output is invalid");

                    i = 3; // Skip 
                }
                else
                {
                    var line = lines[i];
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var format = new FFmpegFormat();


                    if (line[1] == 'D')
                        format.IsDemuxer = true;

                    if (line[2] == 'E')
                        format.IsMuxer = true;

                    for (var j = 4; j < line.Length; j++)
                    {
                        var c = line[j];

                        if (char.IsWhiteSpace(c))
                        {
                            format.Description = line.Substring(j).Trim();
                            break;
                        }

                        format.Name += c;
                    }

                    yield return format;
                }
        }
    }
}