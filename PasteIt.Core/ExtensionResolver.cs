using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PasteIt.Core
{
    public sealed class ExtensionResolver
    {
        private readonly Func<bool> _canConvertVideo;

        public ExtensionResolver(Func<bool>? canConvertVideo = null)
        {
            _canConvertVideo = canConvertVideo ?? (() => VideoConversionSupport.CanConvert());
        }

        public IReadOnlyList<FileExtensionOption> Resolve(ClipboardContent content)
        {
            switch (content.Type)
            {
                case ClipboardContentType.Text:
                    return ResolveForText(content.TextContent);

                case ClipboardContentType.Code:
                    return ResolveForCode(content);

                case ClipboardContentType.Html:
                    return ResolveForHtml();

                case ClipboardContentType.Url:
                    return new[] { new FileExtensionOption("URL", ".url", true) };

                case ClipboardContentType.Audio:
                    return ResolveForAudio(content);

                case ClipboardContentType.Video:
                    return ResolveForVideo(content);

                case ClipboardContentType.Image:
                    return ResolveForImage();

                default:
                    return new[] { new FileExtensionOption("File", ".txt", true) };
            }
        }

        private static IReadOnlyList<FileExtensionOption> ResolveForText(string? text)
        {
            if (IsHeavyMarkdown(text))
            {
                return new[]
                {
                    new FileExtensionOption("Markdown", ".md", true),
                    new FileExtensionOption("Text", ".txt", false)
                };
            }

            var options = new List<FileExtensionOption>
            {
                new FileExtensionOption("Text", ".txt", true),
                new FileExtensionOption("Markdown", ".md", false)
            };

            return options;
        }

        private static IReadOnlyList<FileExtensionOption> ResolveForCode(ClipboardContent content)
        {
            var lang = content.SuggestedLanguage ?? "Code";
            var ext = content.SuggestedExtension ?? ".txt";

            var options = new List<FileExtensionOption>
            {
                new FileExtensionOption(lang, ext, true)
            };

            // If the detected extension is not already .txt, offer it
            if (ext != ".txt")
            {
                options.Add(new FileExtensionOption("Text", ".txt", false));
            }

            return options;
        }

        private static IReadOnlyList<FileExtensionOption> ResolveForImage()
        {
            return new[]
            {
                new FileExtensionOption("PNG", ".png", true),
                new FileExtensionOption("JPG", ".jpg", false),
                new FileExtensionOption("WebP", ".webp", false),
                new FileExtensionOption("Bitmap", ".bmp", false),
                new FileExtensionOption("GIF", ".gif", false),
                new FileExtensionOption("TIFF", ".tiff", false),
                new FileExtensionOption("Icon", ".ico", false),
            };
        }

        private static IReadOnlyList<FileExtensionOption> ResolveForAudio(ClipboardContent content)
        {
            var extension = string.IsNullOrWhiteSpace(content.SuggestedExtension)
                ? ".wav"
                : content.SuggestedExtension!;

            if (!string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    new FileExtensionOption(BuildAudioLabel(extension), extension, true)
                };
            }

            return new[]
            {
                new FileExtensionOption("WAV", ".wav", true),
                new FileExtensionOption("MP3", ".mp3", false),
                new FileExtensionOption("FLAC", ".flac", false),
                new FileExtensionOption("OGG", ".ogg", false),
                new FileExtensionOption("AAC", ".aac", false)
            };
        }

        private IReadOnlyList<FileExtensionOption> ResolveForVideo(ClipboardContent content)
        {
            var extension = string.IsNullOrWhiteSpace(content.SuggestedExtension)
                ? ".mp4"
                : content.SuggestedExtension!;

            return VideoConversionSupport.BuildVideoOptions(extension, _canConvertVideo());
        }

        private static IReadOnlyList<FileExtensionOption> ResolveForHtml()
        {
            return new[]
            {
                new FileExtensionOption("HTML", ".html", true),
                new FileExtensionOption("HTML (short)", ".htm", false),
                new FileExtensionOption("Text", ".txt", false)
            };
        }

        internal static bool IsHeavyMarkdown(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var markdownText = text!;
            var score = 0;

            // Headings: # Title
            if (Regex.IsMatch(markdownText, @"^#{1,6}\s+\S", RegexOptions.Multiline))
            {
                score += 3;
            }

            // Links: [text](url)
            if (Regex.IsMatch(markdownText, @"\[.+?\]\(.+?\)"))
            {
                score += 2;
            }

            // Code fences: ```
            if (markdownText.Contains("```"))
            {
                score += 3;
            }

            // Bold / italic: **text** or *text* or __text__
            if (Regex.IsMatch(markdownText, @"(\*\*|__).+?\1"))
            {
                score += 2;
            }

            // Unordered lists: lines starting with - or *
            if (Regex.IsMatch(markdownText, @"^\s*[-*]\s+\S", RegexOptions.Multiline))
            {
                score += 1;
            }

            // Ordered lists: lines starting with 1. 2. etc.
            if (Regex.IsMatch(markdownText, @"^\s*\d+\.\s+\S", RegexOptions.Multiline))
            {
                score += 1;
            }

            // Blockquotes: > text
            if (Regex.IsMatch(markdownText, @"^\s*>\s+", RegexOptions.Multiline))
            {
                score += 1;
            }

            // Threshold: 4+ signals = heavy markdown
            return score >= 4;
        }

        private static string BuildAudioLabel(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".wav":
                    return "WAV";
                case ".mp3":
                    return "MP3";
                case ".flac":
                    return "FLAC";
                case ".ogg":
                    return "OGG";
                case ".aac":
                    return "AAC";
                case ".m4a":
                    return "M4A";
                case ".wma":
                    return "WMA";
                default:
                    return "Audio";
            }
        }
    }
}
