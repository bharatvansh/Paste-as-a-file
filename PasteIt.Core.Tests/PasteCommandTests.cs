using System;
using System.Drawing;
using System.IO;
using PasteIt;
using PasteIt.Core;
using Xunit;

namespace PasteIt.Core.Tests
{
    [Collection("Environment Variables")]
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

        [Fact]
        public void RecordHistoryIfEnabled_SkipsWritingHistory_WhenDisabled()
        {
            var dataDirectory = Path.Combine(_rootDirectory, "data");
            var originalDataDirectory = Environment.GetEnvironmentVariable("PASTEIT_DATA_DIRECTORY");
            Environment.SetEnvironmentVariable("PASTEIT_DATA_DIRECTORY", dataDirectory);

            try
            {
                var outputFile = Path.Combine(_rootDirectory, "saved.txt");
                File.WriteAllText(outputFile, "saved");

                using var content = ClipboardContent.Text("top secret");
                var result = new FileSaveResult(outputFile, "Text (.txt)");

                PasteCommand.RecordHistoryIfEnabled(content, result, new AppSettings { EnableHistory = false });

                Assert.False(File.Exists(Path.Combine(dataDirectory, "history.json")));
            }
            finally
            {
                Environment.SetEnvironmentVariable("PASTEIT_DATA_DIRECTORY", originalDataDirectory);
            }
        }

        [Fact]
        public void RecordHistoryIfEnabled_WritesHistory_WhenEnabled()
        {
            var dataDirectory = Path.Combine(_rootDirectory, "data");
            var originalDataDirectory = Environment.GetEnvironmentVariable("PASTEIT_DATA_DIRECTORY");
            Environment.SetEnvironmentVariable("PASTEIT_DATA_DIRECTORY", dataDirectory);

            try
            {
                var outputFile = Path.Combine(_rootDirectory, "saved.txt");
                File.WriteAllText(outputFile, "saved");

                using var content = ClipboardContent.Text("kept");
                var result = new FileSaveResult(outputFile, "Text (.txt)");

                PasteCommand.RecordHistoryIfEnabled(content, result, new AppSettings { EnableHistory = true });

                var historyPath = Path.Combine(dataDirectory, "history.json");
                Assert.True(File.Exists(historyPath));
                Assert.Contains("kept", File.ReadAllText(historyPath));
            }
            finally
            {
                Environment.SetEnvironmentVariable("PASTEIT_DATA_DIRECTORY", originalDataDirectory);
            }
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
