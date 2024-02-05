namespace TechBlogCore.AOT.Helpers
{
    public static class Extension
    {
        public static bool IsNullOrWhiteSpace(this string? s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        public static string? Join(this IEnumerable<string> source, char separator)
        {
            if (source == null) return null;
            return string.Join(separator, source);
        }

        public static long HexToLong(this string? id)
        {
            return Convert.ToInt64(id.PadRight(16, '0'), 16);
        }

        public static string LongToHex(this long? value)
        {
            if (value == null) return null;
            return string.Format("{0:X}", value.Value).TrimEnd('0');
        }

        public static string LongToHex(this long value)
        {
            return string.Format("{0:x}", value).TrimEnd('0');
        }
    }
}
