using System;
using System.Reflection;

namespace PasteIt.Core
{
    public sealed class UpdateChecker
    {
        private static readonly TimeSpan BackgroundCheckInterval = TimeSpan.FromDays(1);

        private readonly IUpdateFeedClient _feedClient;
        private readonly Func<AppSettings> _loadSettings;
        private readonly Action<AppSettings> _saveSettings;
        private readonly Func<DateTime> _utcNow;
        private readonly Func<Version?> _currentVersionProvider;

        public UpdateChecker(
            IUpdateFeedClient? feedClient = null,
            Func<AppSettings>? loadSettings = null,
            Action<AppSettings>? saveSettings = null,
            Func<DateTime>? utcNow = null,
            Func<Version?>? currentVersionProvider = null)
        {
            _feedClient = feedClient ?? new GitHubReleaseFeedClient();
            _loadSettings = loadSettings ?? (() => new SettingsManager().Load());
            _saveSettings = saveSettings ?? (settings => new SettingsManager().Save(settings));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _currentVersionProvider = currentVersionProvider ?? (() => AppVersionInfo.GetVersion(Assembly.GetEntryAssembly()));
        }

        public UpdateCheckResult CheckForUpdates(bool manualCheck)
        {
            var settings = _loadSettings();
            var result = new UpdateCheckResult
            {
                IsManualCheck = manualCheck,
                CurrentVersion = AppVersionInfo.GetDisplayVersion(Assembly.GetEntryAssembly())
            };

            if (!manualCheck && !ShouldRunBackgroundCheck(settings))
            {
                return result;
            }

            try
            {
                var update = _feedClient.GetLatestRelease();
                settings.LastUpdateCheckUtc = _utcNow();
                _saveSettings(settings);

                if (update == null)
                {
                    return result;
                }

                var currentVersion = _currentVersionProvider() ?? new Version(0, 0, 0);
                if (update.Version <= currentVersion)
                {
                    return result;
                }

                if (!manualCheck &&
                    !string.IsNullOrWhiteSpace(settings.SkippedVersion) &&
                    string.Equals(settings.SkippedVersion, update.VersionString, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsSkippedVersion = true;
                    return result;
                }

                result.IsUpdateAvailable = true;
                result.UpdateInfo = update;
                return result;
            }
            catch (Exception ex)
            {
                if (manualCheck)
                {
                    result.ErrorMessage = ex.Message;
                }

                return result;
            }
        }

        public void SkipVersion(string? versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
            {
                return;
            }

            var settings = _loadSettings();
            settings.SkippedVersion = versionString;
            _saveSettings(settings);
        }

        public void ClearSkippedVersion()
        {
            var settings = _loadSettings();
            settings.SkippedVersion = null;
            _saveSettings(settings);
        }

        internal bool ShouldRunBackgroundCheck(AppSettings settings)
        {
            if (!settings.LastUpdateCheckUtc.HasValue)
            {
                return true;
            }

            return _utcNow() - settings.LastUpdateCheckUtc.Value >= BackgroundCheckInterval;
        }
    }
}
