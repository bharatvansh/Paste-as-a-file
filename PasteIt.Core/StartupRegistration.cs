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
            ApplyPreference(executablePath, enabled: true);
        }

        public static void Disable()
        {
            ApplyPreference(null, enabled: false);
        }

        public static void ApplyPreference(
            string? executablePath,
            bool enabled,
            RegistryKey? baseKey = null,
            string? runKeyPath = null,
            string? runValueName = null)
        {
            var keyRoot = baseKey ?? Registry.CurrentUser;
            var subKeyPath = runKeyPath ?? RunKeyPath;
            var valueName = runValueName ?? RunValueName;

            try
            {
                if (!enabled)
                {
                    using (var key = keyRoot.OpenSubKey(subKeyPath, writable: true))
                    {
                        key?.DeleteValue(valueName, throwOnMissingValue: false);
                    }

                    return;
                }

                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    return;
                }

                var fullPath = Path.GetFullPath(executablePath);
                var valueData = $"\"{fullPath}\" --service";

                using (var existingKey = keyRoot.OpenSubKey(subKeyPath, writable: false))
                {
                    var existingValue = existingKey?.GetValue(valueName) as string;
                    if (string.Equals(existingValue, valueData, StringComparison.Ordinal))
                    {
                        return;
                    }
                }

                using (var key = keyRoot.CreateSubKey(subKeyPath))
                {
                    key?.SetValue(valueName, valueData, RegistryValueKind.String);
                }
            }
            catch
            {
                // Best-effort registry update; ignore failures.
            }
        }
    }
}
