using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace PasteIt.Core
{
    public sealed class GitHubReleaseFeedClient : IUpdateFeedClient
    {
        internal const string DefaultLatestReleaseUrl = "https://api.github.com/repos/bharatvansh/Paste-as-a-file/releases/latest";
        internal const string DefaultAssetName = "PasteIt_Setup.exe";
        // Intentionally shared for the lifetime of the process to enable connection reuse.
        // This client is not disposed per GitHubReleaseFeedClient instance.
        private static readonly HttpClient HttpClient = CreateHttpClient();

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
            using (var request = new HttpRequestMessage(HttpMethod.Get, _latestReleaseUrl))
            {
                request.Headers.Accept.ParseAdd("application/vnd.github+json");

#if NET48
                using (var response = HttpClient.SendAsync(request).GetAwaiter().GetResult())
#else
                using (var response = HttpClient.Send(request))
#endif
                {
                    response.EnsureSuccessStatusCode();

#if NET48
                    using (var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
#else
                    using (var stream = response.Content.ReadAsStream())
#endif
                    using (var reader = new StreamReader(stream ?? Stream.Null))
                    {
                        var json = reader.ReadToEnd();
                        if (string.IsNullOrWhiteSpace(json))
                        {
                            return null;
                        }

                        var dto = JsonSerializer.Deserialize<GitHubReleaseDto>(json);
                        return TryMap(dto, _expectedAssetName);
                    }
                }
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PasteIt-Updater");
            return client;
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
