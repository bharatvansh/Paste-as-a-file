using System;
using System.Linq;
using System.Windows.Forms;

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
                Application.Run(new PasteServiceContext());
                return 0;
            }

            using (var toast = new ToastNotification())
            {
                toast.ShowError("Unsupported arguments. Use --service or --paste.");
            }

            return 1;
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
