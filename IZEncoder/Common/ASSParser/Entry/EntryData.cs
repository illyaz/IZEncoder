namespace IZEncoder.Common.ASSParser
{
    using System.Collections;
    using System.Collections.Generic;

    internal class EntryData : IReadOnlyList<string>
    {
        private static readonly char[] splitChar = {','};

        private readonly string[] fields;

        internal EntryData(string fields, int count)
        {
            this.fields = fields.Split(splitChar, count);
            for (var i = 0; i < this.fields.Length; i++)
                this.fields[i] = this.fields[i].Trim();
        }

        internal EntryData(params string[] fields)
        {
            this.fields = fields;
            for (var i = 0; i < this.fields.Length; i++)
                FormatHelper.SingleLineStringValueValid(ref this.fields[i]);
        }

        #region IEnumerable<string> 成员

        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>) fields).GetEnumerator();
        }

        #endregion

        #region IEnumerable 成员

        IEnumerator IEnumerable.GetEnumerator()
        {
            return fields.GetEnumerator();
        }

        #endregion

        public override string ToString()
        {
            return string.Join(",", fields);
        }

        #region IReadOnlyCollection<string> 成员

        public string this[int index] => fields[index];

        public int Count => fields.Length;

        #endregion
    }
}