using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PasteIt.Core
{
    public static class ExplorerHelper
    {
        private const string ExplorerClassName = "CabinetWClass";
        private const string ExplorerLegacyClassName = "ExploreWClass";

        public static string ResolveTargetDirectory(string? preferredDirectory)
        {
            if (IsValidDirectory(preferredDirectory))
            {
                return Path.GetFullPath(preferredDirectory!);
            }

            var activeExplorerPath = GetActiveExplorerFolderPath();
            if (IsValidDirectory(activeExplorerPath))
            {
                return activeExplorerPath!;
            }

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            Directory.CreateDirectory(desktop);
            return desktop;
        }

        public static bool IsExplorerInForeground()
        {
            var handle = GetForegroundWindow();
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            var className = GetWindowClassName(handle);
            return className.Equals(ExplorerClassName, StringComparison.OrdinalIgnoreCase) ||
                   className.Equals(ExplorerLegacyClassName, StringComparison.OrdinalIgnoreCase);
        }

        public static string? GetActiveExplorerFolderPath()
        {
            object? shell = null;
            object? windows = null;

            try
            {
                var shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null)
                {
                    return null;
                }

                shell = Activator.CreateInstance(shellType);
                if (shell == null)
                {
                    return null;
                }

                windows = shellType.InvokeMember("Windows", System.Reflection.BindingFlags.InvokeMethod, null, shell, null);
                if (windows == null)
                {
                    return null;
                }

                var foreground = GetForegroundWindow();

                foreach (var window in (IEnumerable)windows)
                {
                    try
                    {
                        var hwndObject = window.GetType().InvokeMember("HWND", System.Reflection.BindingFlags.GetProperty, null, window, null);
                        if (hwndObject == null)
                        {
                            continue;
                        }

                        var windowHandle = new IntPtr(Convert.ToInt32(hwndObject));
                        if (foreground != IntPtr.Zero && windowHandle != foreground)
                        {
                            continue;
                        }

                        var path = TryGetFolderPath(window);
                        if (IsValidDirectory(path))
                        {
                            return path;
                        }
                    }
                    finally
                    {
                        ReleaseComObject(window);
                    }
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                ReleaseComObject(windows);
                ReleaseComObject(shell);
            }

            return null;
        }

        private static string? TryGetFolderPath(object window)
        {
            try
            {
                var document = window.GetType().InvokeMember("Document", System.Reflection.BindingFlags.GetProperty, null, window, null);
                if (document != null)
                {
                    try
                    {
                        var folder = document.GetType().InvokeMember("Folder", System.Reflection.BindingFlags.GetProperty, null, document, null);
                        if (folder != null)
                        {
                            try
                            {
                                var self = folder.GetType().InvokeMember("Self", System.Reflection.BindingFlags.GetProperty, null, folder, null);
                                if (self != null)
                                {
                                    var path = self.GetType().InvokeMember("Path", System.Reflection.BindingFlags.GetProperty, null, self, null) as string;
                                    if (IsValidDirectory(path))
                                    {
                                        return path;
                                    }
                                }
                            }
                            finally
                            {
                                ReleaseComObject(folder);
                            }
                        }
                    }
                    finally
                    {
                        ReleaseComObject(document);
                    }
                }
            }
            catch
            {
            }

            try
            {
                var locationUrl = window.GetType().InvokeMember("LocationURL", System.Reflection.BindingFlags.GetProperty, null, window, null) as string;
                if (string.IsNullOrWhiteSpace(locationUrl))
                {
                    return null;
                }

                if (Uri.TryCreate(locationUrl, UriKind.Absolute, out var uri) &&
                    uri.IsFile)
                {
                    return uri.LocalPath;
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool IsValidDirectory(string? path)
        {
            return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
        }

        private static string GetWindowClassName(IntPtr hwnd)
        {
            var sb = new StringBuilder(256);
            return GetClassName(hwnd, sb, sb.Capacity) > 0 ? sb.ToString() : string.Empty;
        }

        private static void ReleaseComObject(object? obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Marshal.IsComObject(obj))
            {
                Marshal.FinalReleaseComObject(obj);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    }
}
