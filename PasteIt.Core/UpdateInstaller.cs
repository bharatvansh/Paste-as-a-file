using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace PasteIt.Core
{
    public sealed class UpdateInstaller
    {
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

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "PasteIt-Updater";
                client.DownloadFile(updateInfo.InstallerUrl, targetPath);
            }

            return targetPath;
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
