using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Linq;
using System.Text.Json;

namespace PasteIt.Core
{
    public sealed class HistoryManager
    {
        // History JSON is only stored locally under the app data directory.
        // We preserve readable Unicode text here so previews/full text round-trip cleanly.
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly object _lock = new object();

        public void AddEntry(HistoryEntry entry)
        {
            AddEntry(entry, null);
        }

        public void AddEntry(HistoryEntry entry, int? maxHistoryItems)
        {
            if (entry == null)
            {
                return;
            }

            lock (_lock)
            {
                var entries = LoadEntriesInternal();
                entries.Insert(0, entry);

                var max = maxHistoryItems.GetValueOrDefault();
                if (max <= 0)
                {
                    var settings = new SettingsManager().Load();
                    max = settings.MaxHistoryItems > 0 ? settings.MaxHistoryItems : 50;
                }

                if (entries.Count > max)
                {
                    entries = entries.Take(max).ToList();
                }

                SaveEntries(entries);
            }
        }

        public List<HistoryEntry> GetEntries()
        {
            lock (_lock)
            {
                var entries = LoadEntriesInternal();
                var existingEntries = entries.Where(EntryFileExists).ToList();

                if (existingEntries.Count != entries.Count)
                {
                    SaveEntries(existingEntries);
                }

                return existingEntries;
            }
        }

        public void DeleteEntry(HistoryEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            lock (_lock)
            {
                var entries = LoadEntriesInternal();
                var index = entries.FindIndex(candidate => AreSameEntry(candidate, entry));
                if (index < 0)
                {
                    return;
                }

                entries.RemoveAt(index);
                SaveEntries(entries);
            }
        }

        public void ClearHistory()
        {
            lock (_lock)
            {
                SaveEntries(new List<HistoryEntry>());
            }
        }

        private List<HistoryEntry> LoadEntriesInternal()
        {
            try
            {
                if (!File.Exists(HistoryFilePath))
                {
                    return new List<HistoryEntry>();
                }

                var json = File.ReadAllText(HistoryFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<HistoryEntry>();
                }

                return JsonSerializer.Deserialize<List<HistoryEntry>>(json, JsonOptions) ?? new List<HistoryEntry>();
            }
            catch
            {
                return new List<HistoryEntry>();
            }
        }

        private void SaveEntries(List<HistoryEntry> entries)
        {
            try
            {
                Directory.CreateDirectory(DataDirectory);
                var json = JsonSerializer.Serialize(entries, JsonOptions);
                File.WriteAllText(HistoryFilePath, json);
            }
            catch
            {
                // Best-effort write; ignore failures.
            }
        }

        private static bool AreSameEntry(HistoryEntry left, HistoryEntry right)
        {
            return left.TimestampUtc == right.TimestampUtc &&
                   string.Equals(left.FilePath, right.FilePath, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(left.ContentType, right.ContentType, StringComparison.Ordinal) &&
                   string.Equals(left.DisplayType, right.DisplayType, StringComparison.Ordinal) &&
                   string.Equals(left.FullText, right.FullText, StringComparison.Ordinal) &&
                   string.Equals(left.PreviewText, right.PreviewText, StringComparison.Ordinal) &&
                   left.FileSizeBytes == right.FileSizeBytes;
        }

        private static bool EntryFileExists(HistoryEntry entry)
        {
            return entry != null &&
                   !string.IsNullOrWhiteSpace(entry.FilePath) &&
                   File.Exists(entry.FilePath);
        }

        private static string DataDirectory =>
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PASTEIT_DATA_DIRECTORY"))
                ? Environment.GetEnvironmentVariable("PASTEIT_DATA_DIRECTORY")!
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasteIt");

        private static string HistoryFilePath =>
            Path.Combine(DataDirectory, "history.json");
    }
}
