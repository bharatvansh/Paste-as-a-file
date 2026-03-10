namespace PasteIt.Core
{
    public sealed class AppSettings
    {
        public int MaxHistoryItems { get; set; } = 50;
        public string FilenamePrefix { get; set; } = "clipboard";
        public string? DefaultSaveLocation { get; set; }
        public string? FfmpegPath { get; set; }
        public bool EnableHistory { get; set; } = true;
        public bool EnableAutoUpdates { get; set; } = true;
        public System.DateTime? LastUpdateCheckUtc { get; set; }
        public string? SkippedVersion { get; set; }
    }
}
