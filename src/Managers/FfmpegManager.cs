using PulseDL.src.Types;
using PulseDL.src.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PulseDL.src.Managers
{
    internal class FfmpegManager
    {
        public static string ffmpegPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PulseDL",
            "ffmpeg.exe"
        );

        public static async Task<bool> IsFfmpegInstalled()
        {
            return File.Exists(ffmpegPath);
        }
        public static async Task<string> DownloadFfmpeg()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PulseDL"
            );
            Directory.CreateDirectory(folder);
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "PulseDL");
            string json = await client.GetStringAsync("https://cdn.pulsedl.fouinard.fr/latest.json");
            using JsonDocument doc = JsonDocument.Parse(json);
            var assets = doc.RootElement.GetProperty("ffmpeg");
            string downloadUrl = assets.GetProperty("file").ToString();
            if (string.IsNullOrEmpty(downloadUrl)) throw new System.Exception("Could not find ffmpeg.exe in the latest release assets.");
            byte[] data = await client.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(ffmpegPath, data);
            return ffmpegPath;
        }
    }
}
