using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseDL.src.Util
{
    internal class FormatFileSize
    {
        public static string FormatFromBytes(long bytes)
        {
            string[] sizes = { "o", "Ko", "Mo", "Go", "To" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
