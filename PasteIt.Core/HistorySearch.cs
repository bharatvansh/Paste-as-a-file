using System;
using System.IO;

namespace PasteIt.Core
{
    public static class HistorySearch
    {
        public static bool Matches(HistoryEntry? entry, string? query)
        {
            if (entry == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            var trimmedQuery = query?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedQuery))
            {
                return true;
            }

            var term = trimmedQuery!;

            return Contains(Path.GetFileName(entry.FilePath), term) ||
                   Contains(entry.DisplayType, term) ||
                   Contains(entry.ContentType, term) ||
                   Contains(entry.FullText, term) ||
                   Contains(entry.PreviewText, term);
        }

        private static bool Contains(string? value, string term)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var candidate = value!;
            return candidate.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
