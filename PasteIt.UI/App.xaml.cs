using System;
using System.IO;
using System.Linq;
using System.Windows;
using PasteIt.Core;

namespace PasteIt.UI
{
    public partial class App : Application
    {
        /// <summary>
        /// The view requested via command-line (e.g. "settings" or "history").
        /// Null means default (history).
        /// </summary>
        public static string? RequestedView { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var uiExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var installDirectory = Path.GetDirectoryName(uiExecutablePath);
            if (!string.IsNullOrWhiteSpace(installDirectory))
            {
                StartupRegistration.EnsureEnabled(Path.Combine(installDirectory, "PasteIt.exe"));
            }

            var args = e.Args;
            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--view", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    RequestedView = args[i + 1];
                    break;
                }
            }
        }
    }
}
