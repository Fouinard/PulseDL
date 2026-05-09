using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PulseDL.src
{
    public class ComponentInfo
    {
        public required string Version { get; set; }
        public required string Checksum { get; set; }
        public required string File { get; set; }
    }
    public class LatestVersionInfo
    {
        public required ComponentInfo Core { get; set; }
        public required ComponentInfo Ytdlp { get; set; }
        public required ComponentInfo Ffmpeg { get; set; }
    }
    internal class UpdateManager
    {
        public static async Task<LatestVersionInfo> GetLatestVersionInfo()
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "PulseDL");
            string json = await client.GetStringAsync("https://cdn.pulsedl.fouinard.fr/latest.json");
            return JsonSerializer.Deserialize<LatestVersionInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public static async void InstallLatestVersion(LatestVersionInfo versionInfo)
        {
            using HttpClient client = new();
            byte[] fileData = await client.GetByteArrayAsync(versionInfo.Core.File);
            string updateExecutablePath = Path.Combine(
                Path.GetTempPath(),
                "PulseDL_Update_temp.exe"
            );
            await File.WriteAllBytesAsync(
                updateExecutablePath,
                fileData
            );
            string checksum = "";
            using (FileStream stream = File.OpenRead(updateExecutablePath))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                StringBuilder sb = new();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                checksum = sb.ToString();
            }
            Debug.WriteLine(checksum);
            Debug.WriteLine(versionInfo.Core.Checksum);
            if (checksum.ToLower() != versionInfo.Core.Checksum.ToLower())
            {
                File.Delete(updateExecutablePath);
                throw new Exception("Downloaded file checksum does not match expected value.");
            }
            ProcessStartInfo psi = new()
            {
                FileName = updateExecutablePath,
                Arguments = $"/VERYSILENT /SP- /FORCECLOSEAPPLICATIONS",
                UseShellExecute = true
            };
            Process process = Process.Start(psi)!;
        }
    }
}
