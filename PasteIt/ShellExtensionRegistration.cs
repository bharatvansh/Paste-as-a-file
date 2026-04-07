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
                var regAsmPath = ResolveRegAsmPath();
                if (string.IsNullOrWhiteSpace(regAsmPath) || !File.Exists(regAsmPath))
                {
                    return 1;
                }

                var args = unregister
                    ? $"/u \"{resolvedPath}\""
                    : $"/codebase \"{resolvedPath}\"";

                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = regAsmPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process == null)
                {
                    return 1;
                }

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    return 1;
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
            return !string.IsNullOrWhiteSpace(shellExtensionPath)
                ? shellExtensionPath!
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PasteItExtension.dll");
        }

        private static string? ResolveRegAsmPath()
        {
            var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var framework64 = Path.Combine(windowsDirectory, "Microsoft.NET", "Framework64", "v4.0.30319", "regasm.exe");
            if (File.Exists(framework64))
            {
                return framework64;
            }

            var framework = Path.Combine(windowsDirectory, "Microsoft.NET", "Framework", "v4.0.30319", "regasm.exe");
            return File.Exists(framework) ? framework : null;
        }

        private static void RefreshShellAssociations()
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
