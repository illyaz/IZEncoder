﻿namespace IZEncoder.Common.ASSParser
{
    using System;
    using System.Collections.Generic;
    using Serializer;

    /// <summary>
    ///     Entry of "events" section.
    /// </summary>
    public class SubEvent : Entry
    {
        private bool iscomment;

        /// <summary>
        ///     Name of this <see cref="Entry" />, will be "Comment" or "Dialogue".
        /// </summary>
        protected sealed override string EntryName
        {
            get
            {
                if (IsComment)
                    return "Comment";
                return "Dialogue";
            }
        }

        /// <summary>
        ///     Whether the <see cref="SubEvent" /> is comment or not.
        /// </summary>
        public bool IsComment
        {
            get => iscomment;
            set
            {
                Set(ref iscomment, value);
                RaisePropertyChanged("EntryName");
            }
        }

        /// <summary>
        ///     Parse from <paramref name="fields" />.
        /// </summary>
        /// <param name="fields">A <see cref="string" /> of fields that seperates with ','.</param>
        /// <param name="format">The <see cref="EntryHeader" /> presents its format.</param>
        /// <param name="isComment">Whether the <see cref="SubEvent" /> is comment or not.</param>
        /// <returns><see cref="SubEvent" /> presents the <paramref name="fields" />.</returns>
        /// <exception cref="ArgumentNullException">Parameters are null or empty.</exception>
        /// <exception cref="FormatException">Deserialize failed for some fields.</exception>
        public static SubEvent Parse(EntryHeader format, bool isComment, string fields)
        {
            var re = new SubEvent();
            re.Parse(fields, format);
            re.IsComment = isComment;
            return re;
        }

        /// <summary>
        ///     Parse exactly from <paramref name="fields" />.
        /// </summary>
        /// <param name="fields">A <see cref="string" /> of fields that seperates with ','.</param>
        /// <param name="format">The <see cref="EntryHeader" /> presents its format.</param>
        /// <param name="isComment">Whether the <see cref="SubEvent" /> is comment or not.</param>
        /// <returns><see cref="SubEvent" /> presents the <paramref name="fields" />.</returns>
        /// <exception cref="ArgumentNullException">Parameters are null or empty.</exception>
        /// <exception cref="FormatException">Deserialize failed for some fields.</exception>
        /// <exception cref="KeyNotFoundException">
        ///     Fields of <see cref="SubEvent" /> and fields of <paramref name="format" /> doesn't match
        /// </exception>
        public static SubEvent ParseExact(EntryHeader format, bool isComment, string fields)
        {
            var re = new SubEvent();
            re.ParseExact(fields, format);
            re.IsComment = isComment;
            return re;
        }

        /// <summary>
        ///     Make a copy of this <see cref="SubEvent" />.
        /// </summary>
        /// <returns>A copy of this <see cref="SubEvent" />.</returns>
        public new SubEvent Clone()
        {
            var re = Clone<SubEvent>();
            return re;
        }

        /// <summary>
        ///     Return a string form of this <see cref="SubEvent" /> with <see cref="EntryName" /> and <see cref="Text" />.
        /// </summary>
        /// <returns>A string form of this <see cref="SubEvent" />.</returns>
        public override string ToString()
        {
            return EntryName + ": " + text;
        }

        #region Fields

        [EntryField("Layer")] private int layer;

        /// <summary>
        ///     Layer for this <see cref="SubEvent" />.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="value" /> is less than 0.
        /// </exception>
        /// <remarks>
        ///     If you override positioning with an override tag so that two or more lines are displayed on top of each other, this
        ///     field controls which one is drawn where; higher layer numbers are drawn on top of lower ones.
        /// </remarks>
        public int Layer
        {
            get => layer;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                Set(ref layer, value);
            }
        }

        [TimeSerialize] [EntryField("Start")] private TimeSpan startTime;

        /// <summary>
        ///     Start time for this <see cref="SubEvent" />.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="value" /> late than <see cref="EndTime" />.
        /// </exception>
        public TimeSpan StartTime
        {
            get => startTime;
            set
            {
                if (value > endTime)
                    throw new ArgumentOutOfRangeException(nameof(value), "StartTime must earlier than EndTime.");
                Set(ref startTime, value);
            }
        }

        [TimeSerialize] [EntryField("End")] private TimeSpan endTime;

        /// <summary>
        ///     End time for this <see cref="SubEvent" />.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="value" /> earlier than <see cref="StartTime" />.
        /// </exception>
        public TimeSpan EndTime
        {
            get => endTime;
            set
            {
                if (value < startTime)
                    throw new ArgumentOutOfRangeException(nameof(value), "StartTime must earlier than EndTime.");
                Set(ref endTime, value);
            }
        }

        [EntryField("Style", DefaultValue = "*Default")]
        private string style;

        /// <summary>
        ///     <see cref="Style.Name" /> of the <see cref="IZEncoder.Common.ASSParser.Style" /> used for this
        ///     <see cref="SubEvent" />.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="value" /> contains line breaks.</exception>
        public string Style
        {
            get => style;
            set
            {
                if (!FormatHelper.FieldStringValueValid(ref value))
                    value = null;
                Set(ref style, value);
            }
        }

        [EntryField("Name", Alias = "Actor", DefaultValue = "")]
        private string name;

        /// <summary>
        ///     The actor speaking this line.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="value" /> contains line breaks.</exception>
        /// <remarks>
        ///     Has no actual effect on subtitle display but can be useful for editing purposes.
        /// </remarks>
        public string Name
        {
            get => name;
            set
            {
                if (!FormatHelper.FieldStringValueValid(ref value))
                    value = null;
                Set(ref name, value);
            }
        }

        [EntryField("MarginL")] private int marginL;

        /// <summary>
        ///     Left margin of the <see cref="SubEvent" />.
        ///     0 means use the margin specified in the <see cref="Style" />.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="value" /> is less than 0.
        /// </exception>
        public int MarginL
        {
            get => marginL;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                Set(ref marginL, value);
            }
        }

        [EntryField("MarginR")] private int marginR;

        /// <summary>
        ///     Right margin of the <see cref="SubEvent" />.
        ///     0 means use the margin specified in the <see cref="Style" />.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="value" /> is less than 0.
        /// </exception>
        public int MarginR
        {
            get => marginR;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                Set(ref marginR, value);
            }
        }

        [EntryField("MarginV")] private int marginV;

        /// <summary>
        ///     Vetical margin of the <see cref="SubEvent" />.
        ///     0 means use the margin specified in the <see cref="Style" />.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="value" /> is less than 0.
        /// </exception>
        public int MarginV
        {
            get => marginV;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                Set(ref marginV, value);
            }
        }

        [EntryField("Effect", DefaultValue = "")]
        private string effect;

        /// <summary>
        ///     Effect for this <see cref="SubEvent" />.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="value" /> contains line breaks.</exception>
        /// <remarks>
        ///     There are a few predefined effects which can be applied via this field,
        ///     but renderer support for them is spotty and using override tags is nearly always a better idea.
        ///     This is commonly used as a metadata field for automation scripts.
        /// </remarks>
        public string Effect
        {
            get => effect;
            set
            {
                if (!FormatHelper.FieldStringValueValid(ref value))
                    value = null;
                Set(ref effect, value);
            }
        }

        [TextSerialize] [EntryField("Text")] private TextContent text;

        /// <summary>
        ///     Text of this <see cref="SubEvent" />.
        /// </summary>
        public TextContent Text
        {
            get => text;
            set => Set(ref text, value);
        }

        #endregion
    }
}