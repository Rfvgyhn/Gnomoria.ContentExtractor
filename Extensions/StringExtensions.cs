using System;
using System.IO;

namespace Gnomoria.ContentExtractor.Extensions
{
    public static class StringExtensions
    {
        public static string FormatWith(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
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

        // http://stackoverflow.com/a/340454/182821
        public static string GetRelativePathFrom(this string toPath, string fromPath)
        {
            if (fromPath.IsNullOrEmpty()) throw new ArgumentNullException("fromPath");
            if (toPath.IsNullOrEmpty()) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.ToUpperInvariant() == "FILE")
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
    }
}
