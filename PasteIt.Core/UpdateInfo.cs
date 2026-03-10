using System;

namespace PasteIt.Core
{
    public sealed class UpdateInfo
    {
        public string VersionString { get; set; } = string.Empty;
        public Version Version { get; set; } = new Version(0, 0, 0);
        public string InstallerUrl { get; set; } = string.Empty;
        public string ReleaseNotesUrl { get; set; } = string.Empty;
        public DateTime PublishedAtUtc { get; set; }
        public bool IsPrerelease { get; set; }
    }
}
