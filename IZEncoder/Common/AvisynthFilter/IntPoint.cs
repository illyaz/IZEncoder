namespace IZEncoder.Common.AvisynthFilter
{
    using System;
    using Caliburn.Micro;

    public class IntPoint : PropertyChangedBase, IEquatable<IntPoint>
    {
        public IntPoint() { }

        public IntPoint(int? x, int? y)
        {
            X = x;
            Y = y;
        }

        public int? X { get; set; }
        public int? Y { get; set; }


        public bool Equals(IntPoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return X == other.X && Y == other.Y;
        }

        public static IntPoint Parse(string s)
        {
            if (TryParse(s, out var v))
                return v;

            throw new FormatException("Input string was not in a correct format.");
        }

        public static bool TryParse(string s, out IntPoint result)
        {
            var sp = s.Split(',');

            if (sp.Length == 2)
            {
                sp[0] = sp[0].Trim();
                sp[1] = sp[1].Trim();

                result = null;
                var _result = new IntPoint();

                int x, y;

                var xResult = int.TryParse(sp[0], out x);
                var yResult = int.TryParse(sp[1], out y);

                if (sp[0] != "?" && !xResult)
                    return false;

                if (sp[1] != "?" && !yResult)
                    return false;

                _result.X = x;
                _result.Y = y;

                result = _result;
                return true;
            }

            result = null;
            return false;
        }

        public override string ToString()
        {
            return (X == null ? "?" : X.ToString()) + "," + (Y == null ? "?" : Y.ToString());
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((IntPoint) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(IntPoint left, IntPoint right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(IntPoint left, IntPoint right)
        {
            return !Equals(left, right);
        }
    }
}