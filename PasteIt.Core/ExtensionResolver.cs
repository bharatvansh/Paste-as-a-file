using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PasteIt.Core
{
    public sealed class ExtensionResolver
    {
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

                case ClipboardContentType.Image:
                    return new[] { new FileExtensionOption("Image", ".png", true) };

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

            var score = 0;

            // Headings: # Title
            if (Regex.IsMatch(text, @"^#{1,6}\s+\S", RegexOptions.Multiline))
            {
                score += 3;
            }

            // Links: [text](url)
            if (Regex.IsMatch(text, @"\[.+?\]\(.+?\)"))
            {
                score += 2;
            }

            // Code fences: ```
            if (text.Contains("```"))
            {
                score += 3;
            }

            // Bold / italic: **text** or *text* or __text__
            if (Regex.IsMatch(text, @"(\*\*|__).+?\1"))
            {
                score += 2;
            }

            // Unordered lists: lines starting with - or *
            if (Regex.IsMatch(text, @"^\s*[-*]\s+\S", RegexOptions.Multiline))
            {
                score += 1;
            }

            // Ordered lists: lines starting with 1. 2. etc.
            if (Regex.IsMatch(text, @"^\s*\d+\.\s+\S", RegexOptions.Multiline))
            {
                score += 1;
            }

            // Blockquotes: > text
            if (Regex.IsMatch(text, @"^\s*>\s+", RegexOptions.Multiline))
            {
                score += 1;
            }

            // Threshold: 4+ signals = heavy markdown
            return score >= 4;
        }
    }
}
