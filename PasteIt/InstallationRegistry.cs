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

                    SetValueIfChanged(key, "ExecutablePath", executablePath);
                    SetValueIfChanged(key, "InstallPath", installPath);
                }
            }
            catch
            {
                // Best-effort registry update; ignore failures.
            }
        }

        private static void SetValueIfChanged(RegistryKey key, string name, string value)
        {
            var existingValue = key.GetValue(name) as string;
            if (string.Equals(existingValue, value, StringComparison.Ordinal))
            {
                return;
            }

            key.SetValue(name, value, RegistryValueKind.String);
        }
    }
}
