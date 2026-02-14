using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PasteIt
{
    internal static class InstallationRegistry
    {
        private const string RegistryKeyPath = @"Software\PasteIt";

        public static void EnsureCurrentExecutableRegistered()
        {
            try
            {
                var executablePath = Application.ExecutablePath;
                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    return;
                }

                var installPath = Path.GetDirectoryName(executablePath);
                if (string.IsNullOrWhiteSpace(installPath))
                {
                    return;
                }

                using (var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    if (key == null)
                    {
                        return;
                    }

                    key.SetValue("ExecutablePath", executablePath, RegistryValueKind.String);
                    key.SetValue("InstallPath", installPath, RegistryValueKind.String);
                    key.SetValue("LastUpdatedUtc", DateTime.UtcNow.ToString("O"), RegistryValueKind.String);
                }
            }
            catch
            {
                // Best-effort registry update; ignore failures.
            }
        }
    }
}
