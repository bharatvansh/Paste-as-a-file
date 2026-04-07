using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace PasteIt.Core
{
    public sealed class UpdateInstaller
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();

        public string DownloadInstaller(UpdateInfo updateInfo)
        {
            if (updateInfo == null)
            {
                throw new ArgumentNullException(nameof(updateInfo));
            }

            if (string.IsNullOrWhiteSpace(updateInfo.InstallerUrl))
            {
                throw new InvalidOperationException("The release does not provide a downloadable installer.");
            }

            var fileName = "PasteIt_Setup_" + updateInfo.VersionString + ".exe";
            var targetPath = Path.Combine(Path.GetTempPath(), fileName);

            using (var request = new HttpRequestMessage(HttpMethod.Get, updateInfo.InstallerUrl))
#if NET48
            using (var response = HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
            using (var installerStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
#else
            using (var response = HttpClient.Send(request, HttpCompletionOption.ResponseHeadersRead))
            using (var installerStream = response.Content.ReadAsStream())
#endif
            using (var output = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
            {
                response.EnsureSuccessStatusCode();
                installerStream.CopyTo(output);
            }

            return targetPath;
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PasteIt-Updater");
            return client;
        }

        public void LaunchInstaller(string installerPath, bool silent)
        {
            if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
            {
                throw new FileNotFoundException("The downloaded installer could not be found.", installerPath);
            }

            var arguments = silent
                ? "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART"
                : string.Empty;

            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = arguments,
                UseShellExecute = true
            });
        }
    }
}
