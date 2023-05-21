using System.Linq;

namespace LoneStoneStudio.Tools {
    public static class StringExtensions {
        public static bool ContainsAny(this string s, string[] tests) {
            foreach (var t in tests) {
                if (s.Contains(t)) return true;
            }

            return false;
        }

        public static int CountChar(this string s, char character) {
            int count = 0;
            foreach (char c in s) {
                if (c == character) {
                    count++;
                }
            }

            return count;
        }

        public static string FirstUpper(this string s) {
            if (string.IsNullOrEmpty(s)) {
                return s;
            }

            return s.First().ToString().ToUpper() + s.Substring(1);
        }

        public static string AsPercent(this double value) {
            return $"{(value * 100):F1} %";
        }

        public static string Truncate(this string value, int maxLength) => value.Length > maxLength ? value.Substring(0, maxLength) : value;
    }
}