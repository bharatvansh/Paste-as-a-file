using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PasteIt.Core
{
    internal static class VideoConversionSupport
    {
        private static readonly string[] SupportedExtensionsInternal =
        {
            ".mp4",
            ".mov",
            ".avi",
            ".mkv",
            ".webm",
            ".wmv",
            ".m4v",
            ".mpg",
            ".mpeg"
        };

        public static IReadOnlyList<string> SupportedExtensions => SupportedExtensionsInternal;

        public static bool IsSupportedExtension(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            return SupportedExtensionsInternal.Contains(NormalizeExtension(extension!), StringComparer.OrdinalIgnoreCase);
        }

        public static bool CanConvert(Func<AppSettings>? loadSettings = null)
        {
            return !string.IsNullOrWhiteSpace(ResolveFfmpegPath(loadSettings));
        }

        public static string? ResolveFfmpegPath(Func<AppSettings>? loadSettings = null)
        {
            var settingsLoader = loadSettings ?? (() => new SettingsManager().Load());

            try
            {
                var configuredPath = settingsLoader().FfmpegPath;
                if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
                {
                    return Path.GetFullPath(configuredPath);
                }
            }
            catch
            {
            }

            var bundledPath = ResolveBundledFfmpegPath();
            if (!string.IsNullOrWhiteSpace(bundledPath))
            {
                return bundledPath;
            }

            var pathValue = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathValue))
            {
                return null;
            }

            foreach (var rawDirectory in pathValue.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(rawDirectory))
                {
                    continue;
                }

                try
                {
                    var directory = rawDirectory.Trim();
                    var exePath = Path.Combine(directory, "ffmpeg.exe");
                    if (File.Exists(exePath))
                    {
                        return exePath;
                    }

                    var barePath = Path.Combine(directory, "ffmpeg");
                    if (File.Exists(barePath))
                    {
                        return barePath;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        internal static string? ResolveBundledFfmpegPath(string? baseDirectory = null)
        {
            foreach (var directory in GetCandidateDirectories(baseDirectory))
            {
                try
                {
                    var candidate = Path.Combine(directory, "ffmpeg.exe");
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }

                    candidate = Path.Combine(directory, "ffmpeg");
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static IEnumerable<string> GetCandidateDirectories(string? baseDirectory)
        {
            if (!string.IsNullOrWhiteSpace(baseDirectory))
            {
                yield return baseDirectory!;
                yield break;
            }

            var appBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(appBaseDirectory))
            {
                yield return appBaseDirectory;
            }

            var entryAssemblyDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            if (!string.IsNullOrWhiteSpace(entryAssemblyDirectory) &&
                !string.Equals(entryAssemblyDirectory, appBaseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                yield return entryAssemblyDirectory;
            }

            var currentAssemblyDirectory = Path.GetDirectoryName(typeof(VideoConversionSupport).GetTypeInfo().Assembly.Location);
            if (!string.IsNullOrWhiteSpace(currentAssemblyDirectory) &&
                !string.Equals(currentAssemblyDirectory, appBaseDirectory, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(currentAssemblyDirectory, entryAssemblyDirectory, StringComparison.OrdinalIgnoreCase))
            {
                yield return currentAssemblyDirectory;
            }
        }

        public static IReadOnlyList<FileExtensionOption> BuildVideoOptions(string sourceExtension, bool canConvert)
        {
            var normalizedSourceExtension = NormalizeExtension(sourceExtension);
            var options = new List<FileExtensionOption>
            {
                new FileExtensionOption(BuildVideoLabel(normalizedSourceExtension), normalizedSourceExtension, true)
            };

            if (!canConvert)
            {
                return options;
            }

            foreach (var extension in SupportedExtensionsInternal)
            {
                if (string.Equals(extension, normalizedSourceExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                options.Add(new FileExtensionOption(BuildVideoLabel(extension), extension, false));
            }

            return options;
        }

        public static string BuildVideoLabel(string extension)
        {
            switch (NormalizeExtension(extension).ToLowerInvariant())
            {
                case ".mp4":
                    return "MP4 Video";
                case ".mov":
                    return "MOV Video";
                case ".avi":
                    return "AVI Video";
                case ".mkv":
                    return "MKV Video";
                case ".webm":
                    return "WebM Video";
                case ".wmv":
                    return "WMV Video";
                case ".m4v":
                    return "M4V Video";
                case ".mpg":
                case ".mpeg":
                    return "MPEG Video";
                default:
                    return "Video";
            }
        }

        public static string NormalizeExtension(string extension)
        {
            return string.IsNullOrWhiteSpace(extension)
                ? string.Empty
                : (extension.StartsWith(".") ? extension : "." + extension);
        }
    }
}
