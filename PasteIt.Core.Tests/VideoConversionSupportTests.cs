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
    }
}
