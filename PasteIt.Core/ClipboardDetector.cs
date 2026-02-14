using System;
using System.Drawing;
using System.Windows.Forms;

namespace PasteIt.Core
{
    public sealed class ClipboardDetector
    {
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

            if (Clipboard.ContainsData(DataFormats.Html))
            {
                var html = Clipboard.GetText(TextDataFormat.Html);
                if (!string.IsNullOrWhiteSpace(html))
                {
                    return ClipboardContent.Html(html);
                }
            }

            if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                var text = Clipboard.GetText(TextDataFormat.UnicodeText);
                return DetectFromText(text);
            }

            return ClipboardContent.None();
        }

        private ClipboardContent DetectFromText(string? rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return ClipboardContent.None();
            }

            var text = rawText.Trim();

            if (LooksLikeUrl(text))
            {
                return ClipboardContent.Url(text);
            }

            var code = _languageDetector.Detect(text);
            if (code.IsCode)
            {
                return ClipboardContent.Code(text, code.Language, code.Extension);
            }

            return ClipboardContent.Text(text);
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

