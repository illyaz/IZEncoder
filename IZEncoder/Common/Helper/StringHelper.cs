namespace IZEncoder.Common.Helper
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Force.Crc32;

    public static class StringHelper
    {
        public static string EllipsisString(this string rawString, int maxLength = 30, char delimiter = '\\')
        {
            maxLength -= 3; //account for delimiter spacing

            if (rawString.Length <= maxLength) return rawString;

            var final = rawString;
            List<string> parts;

            var loops = 0;
            while (loops++ < 100)
            {
                parts = rawString.Split(delimiter).ToList();
                parts.RemoveRange(parts.Count - 1 - loops, loops);
                if (parts.Count == 1) return parts.Last();

                parts.Insert(parts.Count - 1, "...");
                final = string.Join(delimiter.ToString(), parts);
                if (final.Length < maxLength) return final;
            }

            return rawString.Split(delimiter).ToList().Last();
        }

        public static bool IsASCIIString(this string input)
        {
            return input.All(x => x < 128);
        }

        public static string CRC32String(this string input)
        {
            return string.Join("",
                new Crc32Algorithm().ComputeHash(Encoding.UTF8.GetBytes(input)).Select(b => b.ToString("x2"))
                    .ToArray());
        }

        public static string GetNextAvailableName(this string _name, IEnumerable<string> availableNames)
        {
            var name = Regex.Replace(_name, @"(.+) \(\d+\)", "$1");

            var names = availableNames as string[] ?? availableNames.ToArray();
            if (!names.Contains(name))
                return name;

            string alternateName;
            var nameIndex = 1;
            do
            {
                nameIndex += 1;
                alternateName = name.CreateNumberedName(nameIndex);
            } while (names.Contains(alternateName));

            return alternateName;
        }

        public static string CreateNumberedName(this string name, int number)
        {
            return string.Format("{0} ({1})", name, number);
        }
    }
}