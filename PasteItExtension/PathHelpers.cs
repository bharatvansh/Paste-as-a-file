using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace PasteItExtension
{
    internal static class PathHelpers
    {
        public static string? ResolvePasteItExecutablePath()
        {
            var candidates = new List<string>();

            var envPath = Environment.GetEnvironmentVariable("PASTEIT_EXE_PATH");
            if (!string.IsNullOrWhiteSpace(envPath))
            {
                candidates.Add(envPath);
            }

            AddRegistryCandidates(candidates, Registry.CurrentUser);
            AddRegistryCandidates(candidates, Registry.LocalMachine);

            var extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(extensionDirectory))
            {
                candidates.Add(Path.Combine(extensionDirectory, "PasteIt.exe"));
                candidates.Add(Path.GetFullPath(Path.Combine(extensionDirectory, "..", "PasteIt.exe")));
                candidates.Add(Path.GetFullPath(Path.Combine(extensionDirectory, "..", "..", "PasteIt", "bin", "Release", "PasteIt.exe")));
                candidates.Add(Path.GetFullPath(Path.Combine(extensionDirectory, "..", "..", "PasteIt", "bin", "Debug", "PasteIt.exe")));
            }

            return candidates
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim())
                .FirstOrDefault(File.Exists);
        }

        public static string? ResolvePasteItUIExecutablePath()
        {
            // The UI executable lives alongside PasteIt.exe, so derive its
            // location from the resolved PasteIt.exe path when possible.
            var pasteItExe = ResolvePasteItExecutablePath();
            if (!string.IsNullOrWhiteSpace(pasteItExe))
            {
                var dir = Path.GetDirectoryName(pasteItExe);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    var uiPath = Path.Combine(dir, "PasteIt.UI.exe");
                    if (File.Exists(uiPath))
                    {
                        return uiPath;
                    }
                }
            }

            // Fallback: look relative to the extension DLL itself.
            var extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(extensionDirectory))
            {
                var candidates = new[]
                {
                    Path.Combine(extensionDirectory, "PasteIt.UI.exe"),
                    Path.GetFullPath(Path.Combine(extensionDirectory, "..", "PasteIt.UI.exe")),
                    Path.GetFullPath(Path.Combine(extensionDirectory, "..", "..", "PasteIt.UI", "bin", "Release", "PasteIt.UI.exe")),
                    Path.GetFullPath(Path.Combine(extensionDirectory, "..", "..", "PasteIt.UI", "bin", "Debug", "PasteIt.UI.exe")),
                };

                return candidates.FirstOrDefault(File.Exists);
            }

            return null;
        }

        private static void AddRegistryCandidates(ICollection<string> candidates, RegistryKey baseKey)
        {
            var keyPaths = new[]
            {
                @"Software\PasteIt",
                @"Software\PasteIt\Install"
            };

            foreach (var keyPath in keyPaths)
            {
                using (var key = baseKey.OpenSubKey(keyPath))
                {
                    if (key == null)
                    {
                        continue;
                    }

                    if (key.GetValue("ExecutablePath") is string executablePath &&
                        !string.IsNullOrWhiteSpace(executablePath))
                    {
                        candidates.Add(executablePath);
                    }

                    if (key.GetValue("InstallPath") is string installPath &&
                        !string.IsNullOrWhiteSpace(installPath))
                    {
                        candidates.Add(Path.Combine(installPath, "PasteIt.exe"));
                    }
                }
            }
        }
    }
}
