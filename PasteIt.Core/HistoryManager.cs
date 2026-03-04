using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace PasteIt.Core
{
    public sealed class HistoryManager
    {
        private static readonly string DataDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasteIt");

        private static readonly string HistoryFilePath =
            Path.Combine(DataDirectory, "history.json");

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
                return LoadEntriesInternal();
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
    }
}
