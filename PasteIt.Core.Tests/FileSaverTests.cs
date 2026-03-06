using System;
using System.IO;
using PasteIt.Core;
using Xunit;

namespace PasteIt.Core.Tests
{
    public class FileSaverTests : IDisposable
    {
        private readonly string _tempDirectory;
        private static readonly Func<AppSettings> DefaultSettings = () => new AppSettings();

        public FileSaverTests()
        {
            _tempDirectory = Path.Combine(
                Path.GetTempPath(),
                "PasteItTests_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_tempDirectory);
        }

        [Fact]
        public void Save_WritesTextFile_WithExpectedNamingPattern()
        {
            var saver = new FileSaver(DefaultSettings);
            var content = ClipboardContent.Text("hello from clipboard");
            var now = new DateTime(2026, 2, 14, 10, 30, 0);

            var result = saver.Save(content, _tempDirectory, now);

            Assert.True(File.Exists(result.FilePath));
            Assert.Equal("clipboard_2026-02-14_001.txt", Path.GetFileName(result.FilePath));
            Assert.Equal("hello from clipboard", File.ReadAllText(result.FilePath));
        }

        [Fact]
        public void Save_WritesUrlShortcut_ForUrlContent()
        {
            var saver = new FileSaver(DefaultSettings);
            var content = ClipboardContent.Url("https://example.com");
            var now = new DateTime(2026, 2, 14, 10, 30, 0);

            var result = saver.Save(content, _tempDirectory, now);
            var body = File.ReadAllText(result.FilePath);

            Assert.Equal(".url", Path.GetExtension(result.FilePath));
            Assert.Contains("[InternetShortcut]", body);
            Assert.Contains("URL=https://example.com", body);
        }

        [Fact]
        public void Save_IncrementsCounter_WhenFileAlreadyExists()
        {
            File.WriteAllText(
                Path.Combine(_tempDirectory, "clipboard_2026-02-14_001.txt"),
                "existing");

            var saver = new FileSaver(DefaultSettings);
            var content = ClipboardContent.Text("new content");
            var now = new DateTime(2026, 2, 14, 11, 0, 0);

            var result = saver.Save(content, _tempDirectory, now);

            Assert.Equal("clipboard_2026-02-14_002.txt", Path.GetFileName(result.FilePath));
        }

        [Fact]
        public void Save_UsesSelectedMarkdownExtension_InDisplayType()
        {
            var saver = new FileSaver(DefaultSettings);
            var content = ClipboardContent.Text("## heading");
            var now = new DateTime(2026, 2, 14, 12, 0, 0);

            var result = saver.Save(content, _tempDirectory, now, ".md");

            Assert.Equal("clipboard_2026-02-14_001.md", Path.GetFileName(result.FilePath));
            Assert.Equal("Text (.md)", result.DisplayType);
        }

        [Fact]
        public void Save_UsesSelectedShortHtmlExtension_InDisplayType()
        {
            var saver = new FileSaver(DefaultSettings);
            var content = ClipboardContent.Html("<html><body>Hi</body></html>");
            var now = new DateTime(2026, 2, 14, 12, 30, 0);

            var result = saver.Save(content, _tempDirectory, now, ".htm");

            Assert.Equal("clipboard_2026-02-14_001.htm", Path.GetFileName(result.FilePath));
            Assert.Equal("HTML (.htm)", result.DisplayType);
        }

        [Fact]
        public void Save_UsesConfiguredFilenamePrefix()
        {
            var saver = new FileSaver(() => new AppSettings { FilenamePrefix = "snippet" });
            var content = ClipboardContent.Text("hello from clipboard");
            var now = new DateTime(2026, 2, 14, 13, 0, 0);

            var result = saver.Save(content, _tempDirectory, now);

            Assert.Equal("snippet_2026-02-14_001.txt", Path.GetFileName(result.FilePath));
        }

        [Fact]
        public void Save_CopiesAudioFile_UsingOriginalExtension()
        {
            var saver = new FileSaver(DefaultSettings);
            var bytes = new byte[] { 7, 8, 9, 10 };
            var now = new DateTime(2026, 2, 14, 13, 30, 0);

            using (var content = ClipboardContent.Audio(new MemoryStream(bytes), ".mp3"))
            {
                var result = saver.Save(content, _tempDirectory, now);

                Assert.Equal("clipboard_2026-02-14_001.mp3", Path.GetFileName(result.FilePath));
                Assert.Equal("Audio (.mp3)", result.DisplayType);
                Assert.Equal(bytes, File.ReadAllBytes(result.FilePath));
            }
        }

        [Fact]
        public void Save_RejectsChangingCopiedAudioFormat_WhenConversionIsUnsupported()
        {
            var saver = new FileSaver(DefaultSettings);
            var now = new DateTime(2026, 2, 14, 13, 45, 0);

            using (var content = ClipboardContent.Audio(new MemoryStream(new byte[] { 1, 2, 3 }), ".mp3"))
            {
                var ex = Assert.Throws<InvalidOperationException>(() => saver.Save(content, _tempDirectory, now, ".wav"));
                Assert.Contains(".mp3", ex.Message);
            }
        }

        [Fact]
        public void Save_CopiesVideoFile_UsingOriginalExtension()
        {
            var saver = new FileSaver(DefaultSettings);
            var bytes = new byte[] { 5, 4, 3, 2, 1 };
            var now = new DateTime(2026, 2, 14, 14, 0, 0);

            using (var content = ClipboardContent.Video(new MemoryStream(bytes), ".mp4"))
            {
                var result = saver.Save(content, _tempDirectory, now);

                Assert.Equal("clipboard_2026-02-14_001.mp4", Path.GetFileName(result.FilePath));
                Assert.Equal("Video (.mp4)", result.DisplayType);
                Assert.Equal(bytes, File.ReadAllBytes(result.FilePath));
            }
        }

        [Fact]
        public void Save_UsesTranscoder_WhenVideoExtensionChanges()
        {
            var saver = new FileSaver(
                DefaultSettings,
                (videoStream, sourceExtension, outputPath, loadSettings) =>
                {
                    Assert.Equal(".mp4", sourceExtension);
                    Assert.Equal(".webm", Path.GetExtension(outputPath));
                    using (var writer = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        writer.WriteByte(99);
                    }
                });
            var now = new DateTime(2026, 2, 14, 14, 30, 0);

            using (var content = ClipboardContent.Video(new MemoryStream(new byte[] { 1, 2, 3 }), ".mp4"))
            {
                var result = saver.Save(content, _tempDirectory, now, ".webm");

                Assert.Equal("clipboard_2026-02-14_001.webm", Path.GetFileName(result.FilePath));
                Assert.Equal("Video (.webm)", result.DisplayType);
                Assert.Equal(new byte[] { 99 }, File.ReadAllBytes(result.FilePath));
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
    }
}
