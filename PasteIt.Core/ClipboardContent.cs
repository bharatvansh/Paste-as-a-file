using System;
using System.Drawing;
using System.IO;

namespace PasteIt.Core
{
    public sealed class ClipboardContent : IDisposable
    {
        private ClipboardContent(
            ClipboardContentType type,
            string? textContent,
            string? suggestedLanguage,
            string? suggestedExtension,
            Image? imageContent,
            Stream? audioContent,
            Stream? videoContent)
        {
            Type = type;
            TextContent = textContent;
            SuggestedLanguage = suggestedLanguage;
            SuggestedExtension = suggestedExtension;
            ImageContent = imageContent;
            AudioContent = audioContent;
            VideoContent = videoContent;
        }

        public ClipboardContentType Type { get; }

        public string? TextContent { get; }

        public string? SuggestedLanguage { get; }

        public string? SuggestedExtension { get; }

        public Image? ImageContent { get; }

        public Stream? AudioContent { get; }

        public Stream? VideoContent { get; }

        public bool HasContent =>
            Type switch
            {
                ClipboardContentType.Image => ImageContent != null,
                ClipboardContentType.Audio => AudioContent != null,
                ClipboardContentType.Video => VideoContent != null,
                ClipboardContentType.None => false,
                ClipboardContentType.FileDropList => false,
                _ => !string.IsNullOrWhiteSpace(TextContent)
            };

        public static ClipboardContent None() =>
            new ClipboardContent(ClipboardContentType.None, null, null, null, null, null, null);

        public static ClipboardContent FileDropList() =>
            new ClipboardContent(ClipboardContentType.FileDropList, null, null, null, null, null, null);

        public static ClipboardContent Image(Image image) =>
            new ClipboardContent(ClipboardContentType.Image, null, null, ".png", image, null, null);

        public static ClipboardContent Audio(Stream audioStream, string extension = ".wav") =>
            new ClipboardContent(ClipboardContentType.Audio, null, null, extension, null, audioStream, null);

        public static ClipboardContent Video(Stream videoStream, string extension) =>
            new ClipboardContent(ClipboardContentType.Video, null, null, extension, null, null, videoStream);

        public static ClipboardContent Url(string url) =>
            new ClipboardContent(ClipboardContentType.Url, url, "URL", ".url", null, null, null);

        public static ClipboardContent Html(string html) =>
            new ClipboardContent(ClipboardContentType.Html, html, "HTML", ".html", null, null, null);

        public static ClipboardContent Code(string code, string language, string extension) =>
            new ClipboardContent(ClipboardContentType.Code, code, language, extension, null, null, null);

        public static ClipboardContent Text(string text) =>
            new ClipboardContent(ClipboardContentType.Text, text, "Text", ".txt", null, null, null);

        public void Dispose()
        {
            ImageContent?.Dispose();
            AudioContent?.Dispose();
            VideoContent?.Dispose();
        }
    }
}
