using System;
using System.IO;
using System.Runtime.InteropServices;
using SharpShell.Helpers;

namespace PasteIt
{
    internal static class ShellExtensionRegistration
    {
        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        public static int Register(string? shellExtensionPath)
        {
            return Execute(shellExtensionPath, (regAsm, path) => regAsm.Register64(path, true));
        }

        public static int Unregister(string? shellExtensionPath)
        {
            return Execute(shellExtensionPath, (regAsm, path) => regAsm.Unregister64(path));
        }

        private static int Execute(string? shellExtensionPath, Func<RegAsm, string, bool> action)
        {
            var resolvedPath = ResolveShellExtensionPath(shellExtensionPath);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                return 1;
            }

            try
            {
                var regAsm = new RegAsm();
                if (!action(regAsm, resolvedPath))
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
            if (!string.IsNullOrWhiteSpace(shellExtensionPath))
            {
                return shellExtensionPath!;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PasteItExtension.dll");
        }

        private static void RefreshShellAssociations()
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
