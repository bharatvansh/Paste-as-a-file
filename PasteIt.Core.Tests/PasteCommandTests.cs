using System;
using System.Drawing;
using System.IO;
using PasteIt;
using PasteIt.Core;
using Xunit;

namespace PasteIt.Core.Tests
{
    public class PasteCommandTests : IDisposable
    {
        private readonly string _rootDirectory;

        public PasteCommandTests()
        {
            _rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "PasteItCommandTests_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_rootDirectory);
        }

        [Fact]
        public void ResolveTargetDirectory_UsesConfiguredDefaultSaveLocation_WhenPreferredTargetMissing()
        {
            var configuredDirectory = Path.Combine(_rootDirectory, "configured");
            Directory.CreateDirectory(configuredDirectory);

            var result = PasteCommand.ResolveTargetDirectory(
                preferredTargetDirectory: null,
                loadSettings: () => new AppSettings { DefaultSaveLocation = configuredDirectory });

            Assert.Equal(Path.GetFullPath(configuredDirectory), result);
        }

        [Fact]
        public void ResolveTargetDirectory_PrefersExplicitTarget_OverConfiguredDefault()
        {
            var preferredDirectory = Path.Combine(_rootDirectory, "preferred");
            var configuredDirectory = Path.Combine(_rootDirectory, "configured");
            Directory.CreateDirectory(preferredDirectory);
            Directory.CreateDirectory(configuredDirectory);

            var result = PasteCommand.ResolveTargetDirectory(
                preferredTargetDirectory: preferredDirectory,
                loadSettings: () => new AppSettings { DefaultSaveLocation = configuredDirectory });

            Assert.Equal(Path.GetFullPath(preferredDirectory), result);
        }

        [Theory]
        [InlineData(ClipboardContentType.Text, true)]
        [InlineData(ClipboardContentType.Code, true)]
        [InlineData(ClipboardContentType.Html, true)]
        [InlineData(ClipboardContentType.Url, true)]
        [InlineData(ClipboardContentType.Image, false)]
        [InlineData(ClipboardContentType.Audio, false)]
        [InlineData(ClipboardContentType.Video, false)]
        public void GetHistoryFullText_ReturnsExpectedPayload(ClipboardContentType type, bool expectsText)
        {
            using var content = CreateClipboardContent(type, new string('a', 240));

            var result = PasteCommand.GetHistoryFullText(content);

            if (expectsText)
            {
                Assert.Equal(new string('a', 240), result);
            }
            else
            {
                Assert.Null(result);
            }
        }

        [Fact]
        public void GetHistoryFullText_PreservesLongTextForHistoryAndPreviewCanBeTruncatedSeparately()
        {
            var value = new string('x', 240);
            using var content = ClipboardContent.Text(value);

            var result = PasteCommand.GetHistoryFullText(content);

            Assert.Equal(240, result!.Length);
            Assert.StartsWith(new string('x', 200), result);
        }

        private static ClipboardContent CreateClipboardContent(ClipboardContentType type, string text)
        {
            switch (type)
            {
                case ClipboardContentType.Text:
                    return ClipboardContent.Text(text);
                case ClipboardContentType.Code:
                    return ClipboardContent.Code(text, "C#", ".cs");
                case ClipboardContentType.Html:
                    return ClipboardContent.Html(text);
                case ClipboardContentType.Url:
                    return ClipboardContent.Url(text);
                case ClipboardContentType.Image:
                    return ClipboardContent.Image(new Bitmap(1, 1));
                case ClipboardContentType.Audio:
                    return ClipboardContent.Audio(new MemoryStream(new byte[] { 1, 2, 3 }));
                case ClipboardContentType.Video:
                    return ClipboardContent.Video(new MemoryStream(new byte[] { 1, 2, 3 }), ".mp4");
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
        }
    }
}
