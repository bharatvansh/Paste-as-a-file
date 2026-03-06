namespace PasteIt.Core
{
    public sealed class AppSettings
    {
        public int MaxHistoryItems { get; set; } = 50;
        public string FilenamePrefix { get; set; } = "clipboard";
        public string? DefaultSaveLocation { get; set; }
        public string? FfmpegPath { get; set; }
    }
}
