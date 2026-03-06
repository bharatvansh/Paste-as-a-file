using System;
using System.Collections.Specialized;
using System.IO;
using PasteIt.Core;
using Xunit;

namespace PasteIt.Core.Tests
{
    public class ClipboardDetectorTests
    {
        [Fact]
        public void NormalizeHtmlClipboardContent_StripsCfHtmlHeaderAndFragmentMarkers()
        {
            const string htmlBody = "<html><body><!--StartFragment--><p>Hello</p><!--EndFragment--></body></html>";
            const string headerTemplate =
                "Version:0.9\r\n" +
                "StartHTML:{0:D10}\r\n" +
                "EndHTML:{1:D10}\r\n" +
                "StartFragment:{2:D10}\r\n" +
                "EndFragment:{3:D10}\r\n";

            var placeholderHeader = string.Format(headerTemplate, 0, 0, 0, 0);
            var startHtml = placeholderHeader.Length;
            var endHtml = startHtml + htmlBody.Length;
            var startFragment = startHtml + htmlBody.IndexOf("<!--StartFragment-->", StringComparison.Ordinal) + "<!--StartFragment-->".Length;
            var endFragment = startHtml + htmlBody.IndexOf("<!--EndFragment-->", StringComparison.Ordinal);
            var clipboardHtml = string.Format(headerTemplate, startHtml, endHtml, startFragment, endFragment) + htmlBody;

            var normalized = ClipboardDetector.NormalizeHtmlClipboardContent(clipboardHtml);

            Assert.Equal("<html><body><p>Hello</p></body></html>", normalized);
        }

        [Fact]
        public void IsSupportedAudioExtension_ReturnsTrue_ForKnownAudioFormats()
        {
            Assert.True(ClipboardDetector.IsSupportedAudioExtension(".mp3"));
            Assert.True(ClipboardDetector.IsSupportedAudioExtension(".wav"));
            Assert.False(ClipboardDetector.IsSupportedAudioExtension(".txt"));
        }

        [Fact]
        public void TryCreateAudioContentFromFileDropList_ReturnsAudio_ForSingleSupportedFile()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp3");
            File.WriteAllBytes(tempPath, new byte[] { 1, 2, 3, 4 });

            try
            {
                var fileDropList = new StringCollection();
                fileDropList.Add(tempPath);

                using (var content = ClipboardDetector.TryCreateAudioContentFromFileDropList(fileDropList))
                {
                    Assert.NotNull(content);
                    Assert.Equal(ClipboardContentType.Audio, content!.Type);
                    Assert.Equal(".mp3", content.SuggestedExtension);
                    Assert.NotNull(content.AudioContent);
                }
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
        public void IsSupportedVideoExtension_ReturnsTrue_ForKnownVideoFormats()
        {
            Assert.True(ClipboardDetector.IsSupportedVideoExtension(".mp4"));
            Assert.True(ClipboardDetector.IsSupportedVideoExtension(".webm"));
            Assert.False(ClipboardDetector.IsSupportedVideoExtension(".txt"));
        }

        [Fact]
        public void TryCreateVideoContentFromFileDropList_ReturnsVideo_ForSingleSupportedFile()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
            File.WriteAllBytes(tempPath, new byte[] { 10, 20, 30, 40 });

            try
            {
                var fileDropList = new StringCollection();
                fileDropList.Add(tempPath);

                using (var content = ClipboardDetector.TryCreateVideoContentFromFileDropList(fileDropList))
                {
                    Assert.NotNull(content);
                    Assert.Equal(ClipboardContentType.Video, content!.Type);
                    Assert.Equal(".mp4", content.SuggestedExtension);
                    Assert.NotNull(content.VideoContent);
                }
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
