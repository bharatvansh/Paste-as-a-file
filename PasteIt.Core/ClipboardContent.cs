using System;
using System.Drawing;

namespace PasteIt.Core
{
    public sealed class ClipboardContent : IDisposable
    {
        private ClipboardContent(
            ClipboardContentType type,
            string? textContent,
            string? suggestedLanguage,
            string? suggestedExtension,
            Image? imageContent)
        {
            Type = type;
            TextContent = textContent;
            SuggestedLanguage = suggestedLanguage;
            SuggestedExtension = suggestedExtension;
            ImageContent = imageContent;
        }

        public ClipboardContentType Type { get; }

        public string? TextContent { get; }

        public string? SuggestedLanguage { get; }

        public string? SuggestedExtension { get; }

        public Image? ImageContent { get; }

        public bool HasContent =>
            Type switch
            {
                ClipboardContentType.Image => ImageContent != null,
                ClipboardContentType.None => false,
                ClipboardContentType.FileDropList => false,
                _ => !string.IsNullOrWhiteSpace(TextContent)
            };

        public static ClipboardContent None() =>
            new ClipboardContent(ClipboardContentType.None, null, null, null, null);

        public static ClipboardContent FileDropList() =>
            new ClipboardContent(ClipboardContentType.FileDropList, null, null, null, null);

        public static ClipboardContent Image(Image image) =>
            new ClipboardContent(ClipboardContentType.Image, null, null, ".png", image);

        public static ClipboardContent Url(string url) =>
            new ClipboardContent(ClipboardContentType.Url, url, "URL", ".url", null);

        public static ClipboardContent Html(string html) =>
            new ClipboardContent(ClipboardContentType.Html, html, "HTML", ".html", null);

        public static ClipboardContent Code(string code, string language, string extension) =>
            new ClipboardContent(ClipboardContentType.Code, code, language, extension, null);

        public static ClipboardContent Text(string text) =>
            new ClipboardContent(ClipboardContentType.Text, text, "Text", ".txt", null);

        public void Dispose()
        {
            ImageContent?.Dispose();
        }
    }
}

