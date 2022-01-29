namespace IZEncoder.Common.ASSParser.Serializer
{
    using System;

    /// <summary>
    ///     Custom serializer for <see cref="Color" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ColorSerializeAttribute : SerializeAttribute
    {
        /// <summary>
        ///     Convert <see cref="Color" /> to <see cref="string" />.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The result of convertion.</returns>
        public override string Serialize(object value)
        {
            return value.ToString();
        }

        /// <summary>
        ///     Convert <see cref="string" /> to <see cref="Color" />.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The result of convertion.</returns>
        /// <exception cref="FormatException"><paramref name="value" /> is not a valid color string.</exception>
        public override object Deserialize(string value)
        {
            return Color.Parse(value);
        }
    }
}