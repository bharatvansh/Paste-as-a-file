namespace PasteIt.Core
{
    public sealed class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; set; }
        public bool IsManualCheck { get; set; }
        public bool IsSkippedVersion { get; set; }
        public string? ErrorMessage { get; set; }
        public UpdateInfo? UpdateInfo { get; set; }
        public string CurrentVersion { get; set; } = "0.0.0";
    }
}
