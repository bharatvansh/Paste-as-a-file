using System;
using System.IO;
using PasteIt.Core;
using Xunit;

namespace PasteIt.Core.Tests
{
    public class FileSaverTests : IDisposable
    {
        private readonly string _tempDirectory;

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
            var saver = new FileSaver();
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
            var saver = new FileSaver();
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

            var saver = new FileSaver();
            var content = ClipboardContent.Text("new content");
            var now = new DateTime(2026, 2, 14, 11, 0, 0);

            var result = saver.Save(content, _tempDirectory, now);

            Assert.Equal("clipboard_2026-02-14_002.txt", Path.GetFileName(result.FilePath));
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

