using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using PasteIt.Core;

namespace PasteIt
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            InstallationRegistry.EnsureCurrentExecutableRegistered();
            StartupRegistration.EnsureEnabled(Application.ExecutablePath);

            if (HasArg(args, "--paste"))
            {
                var targetDirectory = ReadArgValue(args, "--target");
                var extensionOverride = ReadArgValue(args, "--ext");
                using (var toast = new ToastNotification())
                {
                    return PasteCommand.Execute(targetDirectory, extensionOverride, toast);
                }
            }

            if (args.Length == 0 || HasArg(args, "--service"))
            {
                return RunService();
            }

            using (var toast = new ToastNotification())
            {
                toast.ShowError("Unsupported arguments. Use --service or --paste.");
            }

            return 1;
        }

        internal static int RunService(
            Func<ApplicationContext>? contextFactory = null,
            Action<string>? reportError = null,
            Action<ApplicationContext>? runApplication = null)
        {
            var createContext = contextFactory ?? (() => new PasteServiceContext());
            var applicationRunner = runApplication ?? (context => Application.Run(context));

            try
            {
                applicationRunner(createContext());
                return 0;
            }
            catch (Exception ex)
            {
                var message = BuildStartupErrorMessage(ex);
                if (reportError != null)
                {
                    reportError(message);
                }
                else
                {
                    using (var toast = new ToastNotification())
                    {
                        toast.ShowError(message);
                    }
                }

                return 1;
            }
        }

        internal static string BuildStartupErrorMessage(Exception ex)
        {
            if (ex is Win32Exception &&
                ex.Message.IndexOf("global hotkey", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "PasteIt couldn't start because Ctrl+Shift+V is already in use by another app or PasteIt instance.";
            }

            return "PasteIt couldn't start: " + ex.Message;
        }

        private static bool HasArg(string[] args, string argName) =>
            args.Any(arg => string.Equals(arg, argName, StringComparison.OrdinalIgnoreCase));

        private static string? ReadArgValue(string[] args, string argName)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], argName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return i + 1 < args.Length ? args[i + 1] : null;
            }

            return null;
        }
    }
}
