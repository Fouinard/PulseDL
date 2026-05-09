using PulseDL.src.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseDL.src.Types
{
    internal class AudioFormatItem
    {
        public bool IsEmpty { get; set; }
        public required YoutubeFormat format { get; set; }
        public string Display =>
            IsEmpty
                ? "Ne pas inclure d'audio"
                : $"Fréquence audio : {Math.Round(format.asr ?? 0)}Hz - Débit : {Math.Round(format.abr ?? 0)}kb/s - Poids : {FormatFileSize.FormatFromBytes(format.filesize!.Value)} ({format.ext} - {format.acodec})";
    }

    internal class VideoFormatItem
    {
        public bool IsEmpty { get; set; }
        public required YoutubeFormat format { get; set; }
        public string Display => 
            IsEmpty 
                ? "Ne pas inclure de vidéo"
                : $"Résolution : {format.resolution} - Débit : {Math.Round(format.vbr ?? 0)}kb/s - Poids : {FormatFileSize.FormatFromBytes(format.filesize!.Value)} ({format.ext} - {format.vcodec})";
    }

    internal class YoutubeFormat
    {
        public required string format_id { get; set; }
        public string? ext { get; set; }
        public string? vcodec { get; set; }
        public string? acodec { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }
        public string? format { get; set; }
        public long? filesize { get; set; }
        public string? resolution { get; set; }
        public string? protocol { get; set; }
        public float? abr { get; set; }
        public float? asr { get; set; }
        public float? vbr { get; set; }

        public string Display => $"";
    }

    internal class YoutubeVideoData
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? thumbnail { get; set; }
        public List<YoutubeFormat>? formats { get; set; }
    }
}
