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

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
    }
}
