using System;
using System.IO;
using PasteIt.Core;
using Xunit;

namespace PasteIt.Core.Tests
{
    public class VideoConversionSupportTests
    {
        [Fact]
        public void ResolveFfmpegPath_UsesConfiguredPath_WhenPresent()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "ffmpeg-" + Guid.NewGuid().ToString("N") + ".exe");
            File.WriteAllBytes(tempPath, new byte[] { 1 });

            try
            {
                var resolved = VideoConversionSupport.ResolveFfmpegPath(() => new AppSettings { FfmpegPath = tempPath });
                Assert.Equal(Path.GetFullPath(tempPath), resolved);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        [Fact]
        public void ResolveBundledFfmpegPath_UsesBundledBinary_WhenPresent()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "pasteit-ffmpeg-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            var tempPath = Path.Combine(tempDirectory, "ffmpeg.exe");
            File.WriteAllBytes(tempPath, new byte[] { 1 });

            try
            {
                var resolved = VideoConversionSupport.ResolveBundledFfmpegPath(tempDirectory);
                Assert.Equal(Path.GetFullPath(tempPath), resolved);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory);
                }
            }
        }

        [Fact]
        public void ResolveBundledFfmpegPath_ReturnsNull_WhenBaseDirectoryDoesNotContainFfmpeg()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "pasteit-ffmpeg-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var resolved = VideoConversionSupport.ResolveBundledFfmpegPath(tempDirectory);
                Assert.Null(resolved);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory);
                }
            }
        }
    }
}
