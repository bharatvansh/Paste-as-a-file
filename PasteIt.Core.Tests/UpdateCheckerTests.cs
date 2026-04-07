using System;
using PasteIt.Core;
using Xunit;

namespace PasteIt.Core.Tests
{
    public class UpdateCheckerTests
    {
        [Fact]
        public void AppSettings_DefaultsAutoUpdatesToEnabled()
        {
            var settings = new AppSettings();

            Assert.True(settings.EnableAutoUpdates);
        }

        [Fact]
        public void CheckForUpdates_ReturnsNoUpdate_WhenVersionsMatch()
        {
            var settings = new AppSettings();
            var checker = new UpdateChecker(
                feedClient: new FakeFeedClient(new UpdateInfo
                {
                    VersionString = "1.0.0",
                    Version = new Version(1, 0, 0),
                    InstallerUrl = "https://example.test/PasteIt_Setup.exe",
                    ReleaseNotesUrl = "https://example.test/release"
                }),
                loadSettings: () => settings,
                saveSettings: _ => { },
                utcNow: () => new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
                currentVersionProvider: () => new Version(1, 0, 0));

            var result = checker.CheckForUpdates(manualCheck: true);

            Assert.False(result.IsUpdateAvailable);
            Assert.Null(result.UpdateInfo);
        }

        [Fact]
        public void CheckForUpdates_ReturnsUpdate_WhenStableReleaseIsNewer()
        {
            var settings = new AppSettings();
            var checker = new UpdateChecker(
                feedClient: new FakeFeedClient(new UpdateInfo
                {
                    VersionString = "1.2.0",
                    Version = new Version(1, 2, 0),
                    InstallerUrl = "https://example.test/PasteIt_Setup.exe",
                    ReleaseNotesUrl = "https://example.test/release"
                }),
                loadSettings: () => settings,
                saveSettings: _ => { },
                utcNow: () => new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
                currentVersionProvider: () => new Version(1, 0, 0));

            var result = checker.CheckForUpdates(manualCheck: true);

            Assert.True(result.IsUpdateAvailable);
            Assert.NotNull(result.UpdateInfo);
            Assert.Equal("1.2.0", result.UpdateInfo!.VersionString);
        }

        [Fact]
        public void CheckForUpdates_SuppressesSkippedVersion_ForBackgroundChecks()
        {
            var settings = new AppSettings
            {
                SkippedVersion = "1.2.0"
            };
            var checker = new UpdateChecker(
                feedClient: new FakeFeedClient(new UpdateInfo
                {
                    VersionString = "1.2.0",
                    Version = new Version(1, 2, 0),
                    InstallerUrl = "https://example.test/PasteIt_Setup.exe",
                    ReleaseNotesUrl = "https://example.test/release"
                }),
                loadSettings: () => settings,
                saveSettings: _ => { },
                utcNow: () => new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
                currentVersionProvider: () => new Version(1, 0, 0));

            var result = checker.CheckForUpdates(manualCheck: false);

            Assert.False(result.IsUpdateAvailable);
            Assert.True(result.IsSkippedVersion);
        }

        [Fact]
        public void CheckForUpdates_UpdatesLastCheckedTimestamp_AfterSuccessfulCheck()
        {
            var settings = new AppSettings();
            var now = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
            var checker = new UpdateChecker(
                feedClient: new FakeFeedClient(null),
                loadSettings: () => settings,
                saveSettings: _ => { },
                utcNow: () => now,
                currentVersionProvider: () => new Version(1, 0, 0));

            checker.CheckForUpdates(manualCheck: true);

            Assert.Equal(now, settings.LastUpdateCheckUtc);
        }

        [Fact]
        public void CheckForUpdates_SkipsBackgroundCheck_WhenLastCheckIsRecent()
        {
            var settings = new AppSettings
            {
                LastUpdateCheckUtc = new DateTime(2026, 3, 10, 6, 0, 0, DateTimeKind.Utc)
            };
            var feedClient = new FakeFeedClient(new UpdateInfo
            {
                VersionString = "1.2.0",
                Version = new Version(1, 2, 0),
                InstallerUrl = "https://example.test/PasteIt_Setup.exe",
                ReleaseNotesUrl = "https://example.test/release"
            });
            var checker = new UpdateChecker(
                feedClient: feedClient,
                loadSettings: () => settings,
                saveSettings: _ => { },
                utcNow: () => new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
                currentVersionProvider: () => new Version(1, 0, 0));

            var result = checker.CheckForUpdates(manualCheck: false);

            Assert.False(result.IsUpdateAvailable);
            Assert.Equal(0, feedClient.CallCount);
        }

        [Fact]
        public void CheckForUpdates_ManualFailuresDoNotCorruptSettings()
        {
            var originalCheckTime = new DateTime(2026, 3, 8, 12, 0, 0, DateTimeKind.Utc);
            var settings = new AppSettings
            {
                LastUpdateCheckUtc = originalCheckTime,
                SkippedVersion = "1.2.0"
            };
            var checker = new UpdateChecker(
                feedClient: new ThrowingFeedClient(),
                loadSettings: () => settings,
                saveSettings: _ => { },
                utcNow: () => new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
                currentVersionProvider: () => new Version(1, 0, 0));

            var result = checker.CheckForUpdates(manualCheck: true);

            Assert.False(result.IsUpdateAvailable);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(originalCheckTime, settings.LastUpdateCheckUtc);
            Assert.Equal("1.2.0", settings.SkippedVersion);
        }

        [Fact]
        public void GitHubReleaseMapping_ParsesExpectedInstallerAsset()
        {
            var dto = new GitHubReleaseFeedClient.GitHubReleaseDto
            {
                tag_name = "v1.2.3",
                html_url = "https://github.com/bharatvansh/Paste-as-a-file/releases/tag/v1.2.3",
                prerelease = false,
                published_at = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
                assets = new[]
                {
                    new GitHubReleaseFeedClient.GitHubAssetDto
                    {
                        name = "PasteIt_Setup.exe",
                        browser_download_url = "https://example.test/PasteIt_Setup.exe"
                    }
                }
            };

            var update = GitHubReleaseFeedClient.TryMap(dto, GitHubReleaseFeedClient.DefaultAssetName);

            Assert.NotNull(update);
            Assert.Equal("1.2.3", update!.VersionString);
            Assert.Equal(new Version(1, 2, 3), update.Version);
            Assert.Equal("https://example.test/PasteIt_Setup.exe", update.InstallerUrl);
        }

        [Fact]
        public void GitHubReleaseMapping_ReturnsNull_WhenInstallerAssetIsMissing()
        {
            var dto = new GitHubReleaseFeedClient.GitHubReleaseDto
            {
                tag_name = "v1.2.3",
                prerelease = false,
                assets = new[]
                {
                    new GitHubReleaseFeedClient.GitHubAssetDto
                    {
                        name = "notes.txt",
                        browser_download_url = "https://example.test/notes.txt"
                    }
                }
            };

            var update = GitHubReleaseFeedClient.TryMap(dto, GitHubReleaseFeedClient.DefaultAssetName);

            Assert.Null(update);
        }

        private sealed class FakeFeedClient : IUpdateFeedClient
        {
            private readonly UpdateInfo? _updateInfo;

            public FakeFeedClient(UpdateInfo? updateInfo)
            {
                _updateInfo = updateInfo;
            }

            public int CallCount { get; private set; }

            public UpdateInfo? GetLatestRelease()
            {
                CallCount++;
                return _updateInfo;
            }
        }

        private sealed class ThrowingFeedClient : IUpdateFeedClient
        {
            public UpdateInfo? GetLatestRelease()
            {
                throw new InvalidOperationException("Network unavailable");
            }
        }
    }
}
