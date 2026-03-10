using System;
using System.Threading;
using System.Windows.Forms;
using PasteIt.Core;

namespace PasteIt
{
    internal static class UpdateWorkflow
    {
        public static void CheckForUpdatesInBackground()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var checker = new UpdateChecker();
                var result = checker.CheckForUpdates(manualCheck: false);

                if (!result.IsUpdateAvailable || result.UpdateInfo == null || result.IsSkippedVersion)
                {
                    return;
                }

                RunInSta(() =>
                {
                    var settings = new SettingsManager().Load();
                    var promptResult = ShowUpdatePrompt(result.UpdateInfo, settings.EnableAutoUpdates);

                    if (promptResult == UpdatePromptChoice.Skip)
                    {
                        checker.SkipVersion(result.UpdateInfo.VersionString);
                        return;
                    }

                    if (promptResult != UpdatePromptChoice.Install)
                    {
                        return;
                    }

                    checker.ClearSkippedVersion();
                    InstallUpdate(result.UpdateInfo, silent: settings.EnableAutoUpdates);
                });
            });
        }

        public static UpdateCheckResult CheckForUpdatesManually()
        {
            var checker = new UpdateChecker();
            return checker.CheckForUpdates(manualCheck: true);
        }

        public static void InstallUpdate(UpdateInfo updateInfo, bool silent)
        {
            var installer = new UpdateInstaller();
            var installerPath = installer.DownloadInstaller(updateInfo);
            installer.LaunchInstaller(installerPath, silent);
        }

        public static void SkipVersion(string? versionString)
        {
            new UpdateChecker().SkipVersion(versionString);
        }

        public static void ClearSkippedVersion()
        {
            new UpdateChecker().ClearSkippedVersion();
        }

        private static void RunInSta(ThreadStart action)
        {
            var thread = new Thread(action);
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        private static UpdatePromptChoice ShowUpdatePrompt(UpdateInfo updateInfo, bool enableAutoUpdates)
        {
            var message = enableAutoUpdates
                ? "PasteIt " + updateInfo.VersionString + " is available.\n\nYes = Install now\nNo = Later\nCancel = Skip this version"
                : "PasteIt " + updateInfo.VersionString + " is available.\n\nAutomatic installs are turned off.\n\nYes = Download and open installer\nNo = Later\nCancel = Skip this version";

            var result = MessageBox.Show(
                message,
                "PasteIt Update Available",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                return UpdatePromptChoice.Install;
            }

            if (result == DialogResult.Cancel)
            {
                return UpdatePromptChoice.Skip;
            }

            return UpdatePromptChoice.Later;
        }

        private enum UpdatePromptChoice
        {
            Later,
            Install,
            Skip
        }
    }
}
