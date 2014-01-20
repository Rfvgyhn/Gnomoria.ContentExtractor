using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gnomoria.ContentExtractor.Extensions
{
    public static class StringExtensions
    {
        public static string Format(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static string EnsureEndsWith(this string str, string value)
        {
            if (!str.EndsWith(value))
                return str + value;

            return str;
        }

        public static string EnsureEndsWith(this string str, char value)
        {
            return EnsureEndsWith(str, value.ToString());
        }
    }
}
