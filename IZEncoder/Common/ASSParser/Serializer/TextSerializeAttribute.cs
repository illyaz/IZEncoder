namespace IZEncoder.Common.ASSParser.Serializer
{
    using System;

    /// <summary>
    ///     Custom serializer for <see cref="TextContent" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TextSerializeAttribute : SerializeAttribute
    {
        /// <summary>
        ///     Convert <see cref="TextContent" /> to <see cref="string" />.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The result of convertion.</returns>
        public override string Serialize(object value)
        {
            if (value == null)
                return string.Empty;
            return value.ToString();
        }

        /// <summary>
        ///     Convert <see cref="string" /> to <see cref="TextContent" />.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The result of convertion.</returns>
        public override object Deserialize(string value)
        {
            return TextContent.Parse(value);
        }
    }
}