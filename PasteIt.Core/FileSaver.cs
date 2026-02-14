using System;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace PasteIt.Core
{
    public sealed class FileSaver
    {
        private readonly Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public FileSaveResult Save(ClipboardContent content, string? targetDirectory, DateTime? now = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (!content.HasContent)
            {
                throw new InvalidOperationException("Clipboard content is empty.");
            }

            var directory = ResolveTargetDirectory(targetDirectory);
            var extension = ResolveExtension(content);
            var path = GenerateUniquePath(directory, extension, now ?? DateTime.Now);

            switch (content.Type)
            {
                case ClipboardContentType.Image:
                    SaveImage(content, path);
                    break;
                case ClipboardContentType.Url:
                    SaveUrlShortcut(content, path);
                    break;
                case ClipboardContentType.Html:
                case ClipboardContentType.Code:
                case ClipboardContentType.Text:
                    SaveText(content, path);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported clipboard type: {content.Type}.");
            }

            return new FileSaveResult(path, BuildDisplayType(content, extension));
        }

        public string GenerateUniquePath(string targetDirectory, string extension, DateTime now)
        {
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            for (var counter = 1; counter <= 999; counter++)
            {
                var filename = $"clipboard_{now:yyyy-MM-dd}_{counter:D3}{extension}";
                var fullPath = Path.Combine(targetDirectory, filename);
                if (!File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return Path.Combine(
                targetDirectory,
                $"clipboard_{now:yyyy-MM-dd}_{Guid.NewGuid():N}{extension}");
        }

        private static string ResolveTargetDirectory(string? preferredPath)
        {
            if (!string.IsNullOrWhiteSpace(preferredPath))
            {
                try
                {
                    var fullPath = Path.GetFullPath(preferredPath);
                    Directory.CreateDirectory(fullPath);
                    return fullPath;
                }
                catch
                {
                }
            }

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            Directory.CreateDirectory(desktop);
            return desktop;
        }

        private static string ResolveExtension(ClipboardContent content)
        {
            switch (content.Type)
            {
                case ClipboardContentType.Image:
                    return ".png";
                case ClipboardContentType.Url:
                    return ".url";
                case ClipboardContentType.Html:
                    return ".html";
                case ClipboardContentType.Code:
                    return string.IsNullOrWhiteSpace(content.SuggestedExtension) ? ".txt" : content.SuggestedExtension!;
                case ClipboardContentType.Text:
                    return ".txt";
                default:
                    return ".txt";
            }
        }

        private static string BuildDisplayType(ClipboardContent content, string extension)
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
                    return $"{content.SuggestedLanguage ?? "Code"} ({extension})";
                case ClipboardContentType.Text:
                    return "Text (.txt)";
                default:
                    return $"File ({extension})";
            }
        }

        private void SaveText(ClipboardContent content, string path)
        {
            File.WriteAllText(path, content.TextContent ?? string.Empty, _encoding);
        }

        private static void SaveImage(ClipboardContent content, string path)
        {
            if (content.ImageContent == null)
            {
                throw new InvalidOperationException("Image content is missing.");
            }

            content.ImageContent.Save(path, ImageFormat.Png);
        }

        private static void SaveUrlShortcut(ClipboardContent content, string path)
        {
            var body = "[InternetShortcut]" + Environment.NewLine +
                       "URL=" + (content.TextContent ?? string.Empty);
            File.WriteAllText(path, body, Encoding.ASCII);
        }
    }
}

