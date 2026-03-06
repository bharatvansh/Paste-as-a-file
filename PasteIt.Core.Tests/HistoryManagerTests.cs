using System;
using System.IO;
using PasteIt.Core;
using Xunit;

namespace PasteIt.Core.Tests
{
    public class HistoryManagerTests : IDisposable
    {
        private readonly string _appDataRoot;
        private readonly string? _originalDataDirectory;
        private readonly string _historyFilePath;

        public HistoryManagerTests()
        {
            _appDataRoot = Path.Combine(Path.GetTempPath(), "PasteItHistoryTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_appDataRoot);
            _originalDataDirectory = Environment.GetEnvironmentVariable("PASTEIT_DATA_DIRECTORY");
            Environment.SetEnvironmentVariable("PASTEIT_DATA_DIRECTORY", _appDataRoot);
            _historyFilePath = Path.Combine(_appDataRoot, "history.json");
        }

        [Fact]
        public void AddEntry_RoundTripsFullText()
        {
            var manager = new HistoryManager();
            var entry = new HistoryEntry
            {
                TimestampUtc = new DateTime(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc),
                FilePath = @"C:\Temp\snippet.cs",
                ContentType = "Code",
                DisplayType = "C# (.cs)",
                FullText = new string('z', 220),
                PreviewText = new string('z', 200) + "…",
                FileSizeBytes = 220
            };

            manager.AddEntry(entry);

            var saved = Assert.Single(manager.GetEntries());
            Assert.Equal(entry.FullText, saved.FullText);
            Assert.Equal(entry.PreviewText, saved.PreviewText);
        }

        [Fact]
        public void GetEntries_LoadsLegacyRowsWithoutFullText()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_historyFilePath)!);
            File.WriteAllText(
                _historyFilePath,
                "[{\"TimestampUtc\":\"2026-03-06T12:00:00Z\",\"FilePath\":\"C:\\\\Temp\\\\note.txt\",\"ContentType\":\"Text\",\"DisplayType\":\"Text (.txt)\",\"PreviewText\":\"legacy preview\",\"FileSizeBytes\":12}]");

            var manager = new HistoryManager();

            var saved = Assert.Single(manager.GetEntries());
            Assert.Null(saved.FullText);
            Assert.Equal("legacy preview", saved.PreviewText);
        }

        [Fact]
        public void DeleteEntry_RemovesOnlyMatchingHistoryRow()
        {
            var manager = new HistoryManager();
            var first = new HistoryEntry
            {
                TimestampUtc = new DateTime(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc),
                FilePath = @"C:\Temp\first.txt",
                ContentType = "Text",
                DisplayType = "Text (.txt)",
                FullText = "first payload",
                PreviewText = "first payload",
                FileSizeBytes = 13
            };
            var second = new HistoryEntry
            {
                TimestampUtc = new DateTime(2026, 3, 6, 12, 1, 0, DateTimeKind.Utc),
                FilePath = @"C:\Temp\second.txt",
                ContentType = "Text",
                DisplayType = "Text (.txt)",
                FullText = "second payload",
                PreviewText = "second payload",
                FileSizeBytes = 14
            };

            manager.AddEntry(first);
            manager.AddEntry(second);

            manager.DeleteEntry(second);

            var entries = manager.GetEntries();
            var remaining = Assert.Single(entries);
            Assert.Equal(first.FilePath, remaining.FilePath);
        }

        [Fact]
        public void HistorySearch_MatchesFilenameMetadataAndStoredPayload()
        {
            var entry = new HistoryEntry
            {
                TimestampUtc = DateTime.UtcNow,
                FilePath = @"C:\Temp\clipboard_2026-03-06_001.html",
                ContentType = "Html",
                DisplayType = "HTML (.html)",
                FullText = "<div>Very long payload that should be searchable</div>",
                PreviewText = "<div>Very long payload"
            };

            Assert.True(HistorySearch.Matches(entry, "clipboard_2026-03-06"));
            Assert.True(HistorySearch.Matches(entry, "html"));
            Assert.True(HistorySearch.Matches(entry, "searchable"));
            Assert.False(HistorySearch.Matches(entry, "missing"));
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("PASTEIT_DATA_DIRECTORY", _originalDataDirectory);

            if (Directory.Exists(_appDataRoot))
            {
                Directory.Delete(_appDataRoot, recursive: true);
            }
        }
    }
}
