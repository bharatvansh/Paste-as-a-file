using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace PasteIt.Core
{
    public sealed class GitHubReleaseFeedClient : IUpdateFeedClient
    {
        internal const string DefaultLatestReleaseUrl = "https://api.github.com/repos/bharatvansh/Paste-as-a-file/releases/latest";
        internal const string DefaultAssetName = "PasteIt_Setup.exe";

        private readonly string _latestReleaseUrl;
        private readonly string _expectedAssetName;

        public GitHubReleaseFeedClient(
            string? latestReleaseUrl = null,
            string? expectedAssetName = null)
        {
            _latestReleaseUrl = string.IsNullOrWhiteSpace(latestReleaseUrl)
                ? DefaultLatestReleaseUrl
                : latestReleaseUrl!;
            _expectedAssetName = string.IsNullOrWhiteSpace(expectedAssetName)
                ? DefaultAssetName
                : expectedAssetName!;
        }

        public UpdateInfo? GetLatestRelease()
        {
            var request = WebRequest.CreateHttp(_latestReleaseUrl);
            request.Method = "GET";
            request.Accept = "application/vnd.github+json";
            request.UserAgent = "PasteIt-Updater";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = 15000;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream ?? Stream.Null))
            {
                var json = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                var serializer = new JavaScriptSerializer();
                var dto = serializer.Deserialize<GitHubReleaseDto>(json);
                return TryMap(dto, _expectedAssetName);
            }
        }

        internal static UpdateInfo? TryMap(GitHubReleaseDto? dto, string expectedAssetName)
        {
            if (dto == null || dto.prerelease)
            {
                return null;
            }

            var normalizedVersion = NormalizeVersion(dto.tag_name);
            if (!Version.TryParse(normalizedVersion, out var version))
            {
                return null;
            }

            var installer = dto.assets?.FirstOrDefault(asset =>
                asset != null &&
                string.Equals(asset.name, expectedAssetName, StringComparison.OrdinalIgnoreCase));
            if (installer == null || string.IsNullOrWhiteSpace(installer.browser_download_url))
            {
                return null;
            }

            return new UpdateInfo
            {
                VersionString = normalizedVersion,
                Version = version,
                InstallerUrl = installer.browser_download_url!,
                ReleaseNotesUrl = string.IsNullOrWhiteSpace(dto.html_url) ? installer.browser_download_url! : dto.html_url!,
                PublishedAtUtc = dto.published_at ?? DateTime.MinValue,
                IsPrerelease = dto.prerelease
            };
        }

        internal static string NormalizeVersion(string? tagName)
        {
            var value = (tagName ?? string.Empty).Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(1);
            }

            return value;
        }

        internal sealed class GitHubReleaseDto
        {
            public string? tag_name { get; set; }
            public string? html_url { get; set; }
            public bool prerelease { get; set; }
            public DateTime? published_at { get; set; }
            public GitHubAssetDto[]? assets { get; set; }
        }

        internal sealed class GitHubAssetDto
        {
            public string? name { get; set; }
            public string? browser_download_url { get; set; }
        }
    }
}
