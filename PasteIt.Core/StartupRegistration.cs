using System;
using System.IO;
using Microsoft.Win32;

namespace PasteIt.Core
{
    public static class StartupRegistration
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "PasteItService";

        public static void EnsureEnabled(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return;
            }

            var fullPath = Path.GetFullPath(executablePath);
            var valueData = $"\"{fullPath}\" --service";

            try
            {
                using (var existingKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false))
                {
                    var existingValue = existingKey?.GetValue(RunValueName) as string;
                    if (string.Equals(existingValue, valueData, StringComparison.Ordinal))
                    {
                        return;
                    }
                }

                using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    key?.SetValue(RunValueName, valueData, RegistryValueKind.String);
                }
            }
            catch
            {
                // Best-effort registry update; ignore failures.
            }
        }
    }
}
