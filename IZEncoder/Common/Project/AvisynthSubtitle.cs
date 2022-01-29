namespace IZEncoder.Common.Project
{
    using System;
    using JetBrains.Annotations;

    public class AvisynthSubtitle : PropertyChangedBaseJson
    {
        public AvisynthSubtitle() { }

        public AvisynthSubtitle([NotNull] string file, bool isMod = false)
        {
            Filename = file ?? throw new ArgumentNullException(nameof(file));
            IsMod = isMod;
        }

        public string Filename { get; set; }
        public bool IsMod { get; set; }
        public string AnalyzerMessage { get; set; }
    }
}