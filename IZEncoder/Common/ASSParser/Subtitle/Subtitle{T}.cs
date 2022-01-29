namespace IZEncoder.Common.ASSParser
{
    using System;
    using System.IO;
    using Collections;

    /// <summary>
    ///     Subtitle file.
    /// </summary>
    /// <typeparam name="TScriptInfo">
    ///     Type of the container of the "script info" section of the ass file.
    /// </typeparam>
    public class Subtitle<TScriptInfo> where TScriptInfo : ScriptInfoCollection
    {
        /// <summary>
        ///     Create a new instance of <see cref="Subtitle{TScriptInfo}" />.
        /// </summary>
        /// <param name="scriptInfo">
        ///     The <typeparamref name="TScriptInfo" />of the <see cref="Subtitle{TScriptInfo}" />
        /// </param>
        public Subtitle(TScriptInfo scriptInfo)
        {
            EventCollection = new EventCollection();
            StyleSet = new StyleSet();
            ScriptInfo = scriptInfo;
        }

        /// <summary>
        ///     Container of information of the "script info" section.
        /// </summary>
        public TScriptInfo ScriptInfo { get; }

        /// <summary>
        ///     Container of information of the "style" section.
        /// </summary>
        public StyleSet StyleSet { get; }

        /// <summary>
        ///     Container of information of the "event" section.
        /// </summary>
        public EventCollection EventCollection { get; }

        /// <summary>
        ///     Write the ass file to <paramref name="writer" />.
        /// </summary>
        /// <param name="writer">A <see cref="TextWriter" /> to write into.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer" /> is null.</exception>
        public void Serialize(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.WriteLine("[Script Info]");
            foreach (var line in Subtitle.EditorInfo) writer.WriteLine(line);
            ScriptInfo.Serialize(writer);
            writer.WriteLine();

            writer.WriteLine("[V4+ Styles]");
            saveStyle(writer);
            writer.WriteLine();

            writer.WriteLine("[Events]");
            saveEvent(writer);

            writer.Flush();
        }

        private void saveStyle(TextWriter writer)
        {
            writer.WriteLine(Subtitle.DefaultStyleFormat.ToString());
            foreach (var item in StyleSet)
                writer.WriteLine(item.Serialize(Subtitle.DefaultStyleFormat));
        }

        private void saveEvent(TextWriter writer)
        {
            writer.WriteLine(Subtitle.DefaultEventFormat.ToString());
            foreach (var item in EventCollection)
                writer.WriteLine(item.Serialize(Subtitle.DefaultEventFormat));
        }

        /// <summary>
        ///     Write the ass file to <see cref="string" />.
        /// </summary>
        /// <returns>A <see cref="string" /> presents the ass file.</returns>
        public string Serialize()
        {
            using (var writer = new StringWriter(FormatHelper.DefaultFormat))
            {
                Serialize(writer);
                return writer.ToString();
            }
        }
    }
}