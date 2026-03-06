using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PasteIt.Core
{
    public sealed class ClipboardDetector
    {
        private static readonly string[] SupportedAudioExtensions =
        {
            ".wav",
            ".mp3",
            ".flac",
            ".ogg",
            ".aac",
            ".m4a",
            ".wma"
        };

        private readonly CodeLanguageDetector _languageDetector;

        public ClipboardDetector(CodeLanguageDetector? languageDetector = null)
        {
            _languageDetector = languageDetector ?? new CodeLanguageDetector();
        }

        public ClipboardContent Detect()
        {
            return ClipboardAccessor.Execute(DetectInternal);
        }

        private ClipboardContent DetectInternal()
        {
            if (Clipboard.ContainsFileDropList())
            {
                var audioContent = TryCreateAudioContentFromFileDropList(Clipboard.GetFileDropList());
                if (audioContent != null)
                {
                    return audioContent;
                }

                var videoContent = TryCreateVideoContentFromFileDropList(Clipboard.GetFileDropList());
                if (videoContent != null)
                {
                    return videoContent;
                }

                return ClipboardContent.FileDropList();
            }

            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    return ClipboardContent.Image((Image)image.Clone());
                }
            }

            if (Clipboard.ContainsAudio())
            {
                var audioStream = Clipboard.GetAudioStream();
                if (audioStream != null)
                {
                    return ClipboardContent.Audio(audioStream);
                }
            }

            // Text-based detection: prioritize plain text over HTML.
            // When copying from a browser, the clipboard contains BOTH CF_HTML
            // and CF_TEXT. The user almost always wants the plain text, not the
            // raw HTML wrapper. Only use HTML if there is no plain text available.
            if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                var text = Clipboard.GetText(TextDataFormat.UnicodeText);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return DetectFromText(text);
                }
            }

            if (Clipboard.ContainsData(DataFormats.Html))
            {
                var html = Clipboard.GetText(TextDataFormat.Html);
                if (!string.IsNullOrWhiteSpace(html))
                {
                    return ClipboardContent.Html(NormalizeHtmlClipboardContent(html));
                }
            }

            return ClipboardContent.None();
        }

        private ClipboardContent DetectFromText(string? rawText)
        {
            return ClassifyTextContent(rawText, _languageDetector);
        }

        internal static ClipboardContent ClassifyTextContent(string? rawText, CodeLanguageDetector? languageDetector = null)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return ClipboardContent.None();
            }

            var text = rawText!;
            var trimmedText = text.Trim();

            if (LooksLikeUrl(trimmedText))
            {
                return ClipboardContent.Url(trimmedText);
            }

            var detector = languageDetector ?? new CodeLanguageDetector();
            var code = detector.Detect(text);
            if (code.IsCode)
            {
                return ClipboardContent.Code(text, code.Language, code.Extension);
            }

            return ClipboardContent.Text(text);
        }

        internal static ClipboardContent? TryCreateAudioContentFromFileDropList(StringCollection? fileDropList)
        {
            if (fileDropList == null || fileDropList.Count != 1)
            {
                return null;
            }

            var filePath = fileDropList[0];
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            var extension = Path.GetExtension(filePath);
            if (!IsSupportedAudioExtension(extension))
            {
                return null;
            }

            try
            {
                var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                return ClipboardContent.Audio(stream, extension);
            }
            catch
            {
                return null;
            }
        }

        internal static ClipboardContent? TryCreateVideoContentFromFileDropList(StringCollection? fileDropList)
        {
            if (fileDropList == null || fileDropList.Count != 1)
            {
                return null;
            }

            var filePath = fileDropList[0];
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            var extension = Path.GetExtension(filePath);
            if (!IsSupportedVideoExtension(extension))
            {
                return null;
            }

            try
            {
                var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                return ClipboardContent.Video(stream, extension);
            }
            catch
            {
                return null;
            }
        }

        internal static bool IsSupportedVideoExtension(string? extension)
        {
            return VideoConversionSupport.IsSupportedExtension(extension);
        }

        internal static bool IsSupportedAudioExtension(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            foreach (var supportedExtension in SupportedAudioExtensions)
            {
                if (string.Equals(supportedExtension, extension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string NormalizeHtmlClipboardContent(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            var normalized = TryExtractHtmlRange(html) ?? html;
            return normalized
                .Replace("<!--StartFragment-->", string.Empty)
                .Replace("<!--EndFragment-->", string.Empty)
                .Trim();
        }

        private static string? TryExtractHtmlRange(string html)
        {
            if (!TryReadOffset(html, "StartHTML:", out var start) ||
                !TryReadOffset(html, "EndHTML:", out var end))
            {
                return null;
            }

            if (start < 0 || end <= start || end > html.Length)
            {
                return null;
            }

            return html.Substring(start, end - start);
        }

        private static bool TryReadOffset(string html, string marker, out int offset)
        {
            offset = 0;

            var start = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                return false;
            }

            start += marker.Length;
            var end = start;

            while (end < html.Length && char.IsDigit(html[end]))
            {
                end++;
            }

            return end > start && int.TryParse(html.Substring(start, end - start), out offset);
        }

        private static bool LooksLikeUrl(string value)
        {
            if (value.Contains(" ") || value.Contains("\n"))
            {
                return false;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return false;
            }

            return uri.Scheme == Uri.UriSchemeHttp ||
                   uri.Scheme == Uri.UriSchemeHttps ||
                   uri.Scheme == Uri.UriSchemeFtp ||
                   uri.Scheme == Uri.UriSchemeFile ||
                   uri.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase);
        }
    }
}
