using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PulseDL.src.Util
{
    internal class Sanitizer
    {
        public static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            name = name.Replace("\"", "").Replace("'", "");
            return name;
        }

        public static string RemoveEmojis(string input)
        {
            return Regex.Replace(input, @"\p{Cs}", "");
        }
    }
}
