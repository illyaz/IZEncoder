namespace IZEncoder.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using IZEncoderNative.Avisynth;
    using DynamicExpresso;
    using Helper;
    using LiteDB;
    using Project;

    public class EncodingQueue : PropertyChangedBaseJson
    {
        public ObjectId Id { get; set; }
        public EncodingQueueStatus Status { get; set; } = EncodingQueueStatus.Creating;
        public DateTime? CreatedOn { get; set; } = DateTime.Now;
        public DateTime? LastRun { get; set; }
        public DateTime? CompletedOn { get; set; }
        public List<string> GeneratedCommands { get; set; } = new List<string>();
        public int CommandIndex { get; set; }
        public AvisynthVideoInfo AvisynthInfo { get; set; }
        public string WorkingPath { get; set; }
        public int? ExitCode { get; set; }
        public string Reason { get; set; }

        [BsonIgnore]
        public AvisynthProject Project { get; set; }

        [BsonIgnore]
        public double Processing { get; set; }

        public void LoadProject(bool reload = false)
        {
            if (reload)
                Project = null;

            Project = Project ?? AvisynthProjectHelper.LoadFromFile(GetProejctPath());
        }

        public AvisynthProject GetAvisynthProject(bool reload = false)
        {
            if (reload)
                Project = null;

            return Project = Project ?? AvisynthProjectHelper.LoadFromFile(GetProejctPath());
        }

        public string GetWorkingPath()
        {
            return Path.GetFullPath(WorkingPath);
        }

        public string GetScriptPath()
        {
            return Path.GetFullPath(Path.Combine(WorkingPath, "script.avs"));
        }

        public string GetProejctPath()
        {
            return Path.GetFullPath(Path.Combine(WorkingPath, "project.izproj"));
        }

        public string GetEncoderStatPath(int index)
        {
            return Path.GetFullPath(Path.Combine(WorkingPath, $"stat-{index}.izs"));
        }

        public string GetEncoderOutputLogPath(int index)
        {
            return Path.GetFullPath(Path.Combine(WorkingPath, $"ffmpeg-{index}.log"));
        }

        public string CompileCommand(int index)
        {
            if (!index.InRange(0, GeneratedCommands.Count))
                throw new ArgumentOutOfRangeException(nameof(index));

            var interpreter = new Interpreter();
            interpreter.SetVariable("WorkingPath", GetWorkingPath());
            interpreter.SetVariable("ScriptPath", GetScriptPath());
            interpreter.SetVariable("ProjectPath", GetProejctPath());
            interpreter.SetVariable("Project", GetAvisynthProject());

            var regx = new Regex(@"{([^}]*)}");
            var regx2 = new Regex(@"(?<=\{)[^}]*(?=\})");

            return regx.Replace(GeneratedCommands[index], r =>
            {
                if (r.Value.Trim().StartsWith("{{") && r.Value.Trim().EndsWith("}}"))
                    return r.Value;

                var result = interpreter.Parse(regx2.Match(r.Value).Groups[0].Value).Invoke();
                return result?.ToString() ?? "";
            }).Trim();
        }

        public override string ToString()
        {
            return Id?.ToString() ?? "";
        }
    }

    public enum EncodingQueueStatus
    {
        Creating,
        Waiting,
        Retry,
        Processing,
        Completed,
        Aborted,
        Error
    }
}