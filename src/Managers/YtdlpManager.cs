using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PulseDL.src.Util;
using PulseDL.src.Types;

namespace PulseDL.src.Managers
{
    internal class YtdlpManager
    {
        public static string ytdlpPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PulseDL",
            "yt-dlp.exe"
        );

        public static async Task<YoutubeVideoData> GetVideoData(string url, Action<string> onError)
        {
            var settings = SettingsManager.Load();
            string browser = settings.DefaultBrowser.ToLower();

            if (string.IsNullOrEmpty(browser) || browser == "sans navigateur")
            {
                browser = "";
            }
            else
            {
                browser = "--cookies-from-browser " + settings.DefaultBrowser.ToLower();
            }

            var psi = new ProcessStartInfo
            {
                FileName = ytdlpPath,
                Arguments = $"--js-runtimes node --update --no-playlist {browser} -J --ffmpeg-location {FfmpegManager.ffmpegPath} --skip-download \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(psi)!;
            string output = (await process.StandardOutput.ReadToEndAsync()).Trim();
            string error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            if(!string.IsNullOrEmpty(error))
            {
                onError(error);
                return new YoutubeVideoData();
            }
            Debug.WriteLine(error);
            YoutubeVideoData video = JsonSerializer.Deserialize<YoutubeVideoData>(output)!;
            List<YoutubeFormat> filteredFormats = [];
            foreach(var format in video.formats)
            {
                if(!format.format_id.Contains("drc")) {
                    filteredFormats.Add(format);
                }
            }
            video.formats = filteredFormats;
            return video;
        }

        public static async Task DownloadYoutubeVideo(
            YoutubeVideoData videoData,
            string format,
            string downloadFolder,
            Action<float> progressCallback,
            Action<string, string> milestoneCallback
        )
        {
            string path = Path.Combine(downloadFolder, $"{Sanitizer.SanitizeFileName(videoData.title)} ({videoData.id}).%(ext)s");
            var psi = new ProcessStartInfo
            {
                FileName = ytdlpPath,
                Arguments = $"--js-runtimes node --update -f \"{format}\" --newline -o \"{path}\" \"https://youtu.be/{videoData.id}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = new Process { StartInfo = psi };
            process.OutputDataReceived += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                if(e.Data.Contains("ETA"))
                {
                    string data = Regex.Replace(e.Data.Trim(), @"\s+", " ").Trim();
                    float.TryParse(
                        data.Split(" ")[1].Replace("%", "").Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float percent
                    );
                    progressCallback(percent);
                }
                if(e.Data.Contains("Destination:"))
                {
                    if(!format.Contains("+"))
                    {
                        string finalFilepath = string.Join("Destination:", e.Data.Trim().Split("Destination:").Skip(1)).Trim();
                        milestoneCallback("dl_final", finalFilepath);
                    } else
                    {
                        string partFilepath = string.Join("Destination:", e.Data.Trim().Split("Destination:").Skip(1)).Trim();
                        milestoneCallback("dl_part", partFilepath);
                    }
                }
                if(format.Contains("+") && e.Data.Contains("[Merger]"))
                {
                    string finalFilepath = string.Join("into", e.Data.Trim().Split("into").Skip(1)).Trim();
                    finalFilepath = finalFilepath.Substring(1, finalFilepath.Length - 2);
                    milestoneCallback("merging", finalFilepath);
                }
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                Debug.WriteLine(e.Data);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            return;
        }
        public static async Task<bool> IsYtdlpInstalled()
        {
            return File.Exists(ytdlpPath);
        }
        public static async Task<string> DownloadYtdlp()
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
            var assets = doc.RootElement.GetProperty("ytdlp");
            string downloadUrl = assets.GetProperty("file").ToString();
            if(string.IsNullOrEmpty(downloadUrl)) throw new System.Exception("Could not find yt-dlp.exe in the latest release assets.");
            byte[] data = await client.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(ytdlpPath, data);
            return ytdlpPath;
        }
    }
}