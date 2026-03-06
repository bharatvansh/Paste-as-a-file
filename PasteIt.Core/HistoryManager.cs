using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace PasteIt.Core
{
    public sealed class HistoryManager
    {
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private readonly object _lock = new object();

        public void AddEntry(HistoryEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            lock (_lock)
            {
                var entries = LoadEntriesInternal();
                entries.Insert(0, entry);

                var settings = new SettingsManager().Load();
                var max = settings.MaxHistoryItems > 0 ? settings.MaxHistoryItems : 50;
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

                return _serializer.Deserialize<List<HistoryEntry>>(json) ?? new List<HistoryEntry>();
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
                var json = _serializer.Serialize(entries);
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
