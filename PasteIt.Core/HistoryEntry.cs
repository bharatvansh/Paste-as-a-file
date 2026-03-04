using System;

namespace PasteIt.Core
{
    public sealed class HistoryEntry
    {
        public DateTime TimestampUtc { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string DisplayType { get; set; } = string.Empty;
        public string? PreviewText { get; set; }
        public long FileSizeBytes { get; set; }
    }
}
