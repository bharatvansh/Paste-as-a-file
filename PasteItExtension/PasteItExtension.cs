using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using PasteIt.Core;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

namespace PasteItExtension
{
    [ComVisible(true)]
    [Guid("E6FB1156-2AA7-4E08-B889-9F411F944DA2")]
    [COMServerAssociation(AssociationType.Directory)]
    [COMServerAssociation(AssociationType.DirectoryBackground)]
    public class PasteItExtension : SharpContextMenu
    {
        protected override bool CanShowMenu()
        {
            try
            {
                var detector = new ClipboardDetector();
                using (var content = detector.Detect())
                {
                    return content.Type != ClipboardContentType.None &&
                           content.Type != ClipboardContentType.FileDropList;
                }
            }
            catch
            {
                return false;
            }
        }

        protected override ContextMenuStrip CreateMenu()
        {
            var detector = new ClipboardDetector();
            string detectedLabel;

            using (var content = detector.Detect())
            {
                detectedLabel = BuildDetectedLabel(content);
            }

            var rootMenuItem = new ToolStripMenuItem("Paste as File");
            var detectedTypeMenuItem = new ToolStripMenuItem("Paste as " + detectedLabel);
            detectedTypeMenuItem.Click += (sender, args) => ExecutePaste();
            rootMenuItem.DropDownItems.Add(detectedTypeMenuItem);

            var menu = new ContextMenuStrip();
            menu.Items.Add(rootMenuItem);
            return menu;
        }

        private void ExecutePaste()
        {
            var targetDirectory = ResolveTargetDirectory();
            var executablePath = PathHelpers.ResolvePasteItExecutablePath();
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return;
            }

            var arguments = $"--paste --target {QuoteForWindowsCommandLine(targetDirectory)}";
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfo);
        }

        private string ResolveTargetDirectory()
        {
            // When right-clicking on a specific folder, SelectedItemPaths has the folder path.
            if (SelectedItemPaths != null && SelectedItemPaths.Any())
            {
                foreach (var selectedPath in SelectedItemPaths)
                {
                    if (Directory.Exists(selectedPath))
                    {
                        return selectedPath;
                    }

                    var parent = Path.GetDirectoryName(selectedPath);
                    if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
                    {
                        return parent;
                    }
                }
            }

            // When right-clicking on the background of a folder, FolderPath
            // gives us the directory the user is currently looking at.
            try
            {
                var folderPath = FolderPath;
                if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
                {
                    return folderPath;
                }
            }
            catch
            {
                // FolderPath may not be available in all contexts.
            }

            return ExplorerHelper.ResolveTargetDirectory(null);
        }

        private static string BuildDetectedLabel(ClipboardContent content)
        {
            switch (content.Type)
            {
                case ClipboardContentType.Image:
                    return "Image (.png)";
                case ClipboardContentType.Url:
                    return "URL (.url)";
                case ClipboardContentType.Html:
                    return "HTML (.html)";
                case ClipboardContentType.Code:
                    return $"{content.SuggestedLanguage ?? "Code"} ({content.SuggestedExtension ?? ".txt"})";
                case ClipboardContentType.Text:
                    return "Text (.txt)";
                default:
                    return "File";
            }
        }

        private static string QuoteForWindowsCommandLine(string value)
        {
            if (value == null)
            {
                return "\"\"";
            }

            var builder = new StringBuilder();
            builder.Append('\"');

            var backslashCount = 0;
            foreach (var character in value)
            {
                if (character == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (character == '\"')
                {
                    builder.Append('\\', backslashCount * 2 + 1);
                    builder.Append('\"');
                    backslashCount = 0;
                    continue;
                }

                if (backslashCount > 0)
                {
                    builder.Append('\\', backslashCount);
                    backslashCount = 0;
                }

                builder.Append(character);
            }

            if (backslashCount > 0)
            {
                builder.Append('\\', backslashCount * 2);
            }

            builder.Append('\"');
            return builder.ToString();
        }
    }
}
