using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace PasteIt
{
    internal static class ShellExtensionRegistration
    {
        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        public static int Register(string? shellExtensionPath)
        {
            return Execute(shellExtensionPath, false);
        }

        public static int Unregister(string? shellExtensionPath)
        {
            return Execute(shellExtensionPath, true);
        }

        private static int Execute(string? shellExtensionPath, bool unregister)
        {
            var resolvedPath = ResolveShellExtensionPath(shellExtensionPath);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                return 1;
            }

            try
            {
                var args = unregister ? $"/u /s \"{resolvedPath}\"" : $"/s \"{resolvedPath}\"";
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "regsvr32.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        return 1;
                    }
                }

                RefreshShellAssociations();
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        private static string ResolveShellExtensionPath(string? shellExtensionPath)
        {
            var path = !string.IsNullOrWhiteSpace(shellExtensionPath)
                ? shellExtensionPath!
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PasteItExtension.dll");

            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            var directory = Path.GetDirectoryName(path);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            var comHostFileName = $"{fileNameWithoutExtension}.comhost.dll";

            return string.IsNullOrEmpty(directory)
                ? comHostFileName
                : Path.Combine(directory, comHostFileName);
        }

        private static void RefreshShellAssociations()
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
