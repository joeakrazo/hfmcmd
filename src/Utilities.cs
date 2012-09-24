using System;
using System.IO;
using System.Text.RegularExpressions;


namespace HFMCmd
{

    public static class Utilities
    {

        /// <summary>
        /// Converts a pattern containing ? and * characters into a case-
        /// insensitive regular expression.
        /// </summary>
        public static Regex ConvertWildcardPatternToRE(string pattern)
        {
            return new Regex("^" + pattern.Replace(".", @"\.")
                    .Replace("*", ".*").Replace("?", ".") + "$",
                    RegexOptions.IgnoreCase);
        }


        /// <summary>
        /// Checks if the specified path exists, throwing an exception if it
        /// doesn't.
        /// </summary>
        public static void EnsureFileExists(string path)
        {
            if(!File.Exists(path)) {
                throw new FileNotFoundException(string.Format("No file was found at {0}", path));
            }
        }


        /// <summary>
        /// Checks if the specified directory exists, throwing an exception if
        /// it doesn't.
        /// </summary>
        public static void EnsureDirExists(string path)
        {
            if(!Directory.Exists(path)) {
                throw new DirectoryNotFoundException(string.Format("No directory was found at {0}", path));
            }
        }


        /// <summary>
        /// Checks if the specified path can be written to.
        /// </summary>
        public static void EnsureFileWriteable(string path)
        {
            using (FileStream s = File.Open(path, FileMode.OpenOrCreate,
                                            FileAccess.Write, FileShare.None))
            { }
        }


        /// <summary>
        /// Returns the part of path2 that is not in path1.
        /// </summary>
        public static string PathDifference(string path1, string path2)
        {
            string diff = "";
            if(path2.Length > path1.Length) {
                if(path2.Substring(0, path1.Length) != path1) {
                    throw new ArgumentException(string.Format("Path {0} is not a parent path of {1}", path1, path2));
                }
                else {
                    return path2.Substring(path1.Length);
                }
            }
            return diff;
        }

    }

}
