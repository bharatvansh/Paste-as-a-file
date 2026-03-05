using System;
using System.Linq;
using System.Windows;

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
