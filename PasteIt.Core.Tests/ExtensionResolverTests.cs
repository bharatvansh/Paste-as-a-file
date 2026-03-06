using System.Linq;
using PasteIt.Core;
using Xunit;

namespace PasteIt.Core.Tests
{
    public class ExtensionResolverTests
    {
        [Fact]
        public void Resolve_ReturnsMissingExtensionOverrides_ForPlainText()
        {
            var resolver = new ExtensionResolver();
            var content = ClipboardContent.Text("Just a regular sentence.");

            var options = resolver.Resolve(content);

            Assert.Equal(2, options.Count);
            
            Assert.True(options[0].IsDefault);
            Assert.Equal(".txt", options[0].Extension);
            Assert.Equal("Text", options[0].Label);

            Assert.False(options[1].IsDefault);
            Assert.Equal(".md", options[1].Extension);
            Assert.Equal("Markdown", options[1].Label);
        }

        [Fact]
        public void Resolve_ReturnsMarkdownAsDefault_ForHeavyMarkdownText()
        {
            var resolver = new ExtensionResolver();
            var markdownText = "# Title\n\nHere is a [link](https://example.com).\n\n```csharp\nvar x = 1;\n```\n\n- List item 1\n- List item 2";
            var content = ClipboardContent.Text(markdownText);

            var options = resolver.Resolve(content);

            Assert.Equal(2, options.Count);
            
            Assert.True(options[0].IsDefault);
            Assert.Equal(".md", options[0].Extension);
            
            Assert.False(options[1].IsDefault);
            Assert.Equal(".txt", options[1].Extension);
        }

        [Fact]
        public void Resolve_ReturnsLanguageAsDefault_FallbacksToTxt_ForCode()
        {
            var resolver = new ExtensionResolver();
            var content = ClipboardContent.Code("print('hello')", "Python", ".py");

            var options = resolver.Resolve(content);

            Assert.Equal(2, options.Count);
            
            Assert.True(options[0].IsDefault);
            Assert.Equal(".py", options[0].Extension);
            Assert.Equal("Python", options[0].Label);

            Assert.False(options[1].IsDefault);
            Assert.Equal(".txt", options[1].Extension);
            Assert.Equal("Text", options[1].Label);
        }

        [Fact]
        public void Resolve_ReturnsHtmlOptions_ForHtmlContent()
        {
            var resolver = new ExtensionResolver();
            var content = ClipboardContent.Html("<html><body>Hi</body></html>");

            var options = resolver.Resolve(content);

            Assert.Equal(3, options.Count);
            
            Assert.True(options[0].IsDefault);
            Assert.Equal(".html", options[0].Extension);

            Assert.False(options[1].IsDefault);
            Assert.Equal(".htm", options[1].Extension);

            Assert.False(options[2].IsDefault);
            Assert.Equal(".txt", options[2].Extension);
        }

        [Fact]
        public void Resolve_ReturnsOnlySingleOption_ForUrl()
        {
            var resolver = new ExtensionResolver();
            var content = ClipboardContent.Url("https://example.com");

            var options = resolver.Resolve(content);

            Assert.Single(options);
            Assert.True(options[0].IsDefault);
            Assert.Equal(".url", options[0].Extension);
        }

        [Fact]
        public void Resolve_ReturnsMultipleImageFormats_ForImageContent()
        {
            var resolver = new ExtensionResolver();
            using (var bitmap = new System.Drawing.Bitmap(1, 1))
            using (var content = ClipboardContent.Image(bitmap))
            {
                var options = resolver.Resolve(content);

                Assert.Equal(7, options.Count);
                Assert.True(options[0].IsDefault);
                Assert.Equal(".png", options[0].Extension);
                Assert.Contains(options, o => o.Extension == ".jpg");
                Assert.Contains(options, o => o.Extension == ".webp");
                Assert.Contains(options, o => o.Extension == ".bmp");
                Assert.Contains(options, o => o.Extension == ".gif");
                Assert.Contains(options, o => o.Extension == ".tiff");
                Assert.Contains(options, o => o.Extension == ".ico");
            }
        }

        [Fact]
        public void Resolve_ReturnsOriginalAudioFormatOnly_ForCopiedAudioFiles()
        {
            var resolver = new ExtensionResolver();
            using (var content = ClipboardContent.Audio(new System.IO.MemoryStream(new byte[] { 1, 2, 3 }), ".mp3"))
            {
                var options = resolver.Resolve(content);

                Assert.Single(options);
                Assert.True(options[0].IsDefault);
                Assert.Equal(".mp3", options[0].Extension);
                Assert.Equal("MP3", options[0].Label);
            }
        }

        [Fact]
        public void Resolve_ReturnsOriginalVideoFormatOnly_WhenVideoConversionUnavailable()
        {
            var resolver = new ExtensionResolver(() => false);
            using (var content = ClipboardContent.Video(new System.IO.MemoryStream(new byte[] { 1, 2, 3 }), ".webm"))
            {
                var options = resolver.Resolve(content);

                Assert.Single(options);
                Assert.True(options[0].IsDefault);
                Assert.Equal(".webm", options[0].Extension);
                Assert.Equal("WebM Video", options[0].Label);
            }
        }

        [Fact]
        public void Resolve_ReturnsMultipleVideoFormats_WhenVideoConversionAvailable()
        {
            var resolver = new ExtensionResolver(() => true);
            using (var content = ClipboardContent.Video(new System.IO.MemoryStream(new byte[] { 1, 2, 3 }), ".mp4"))
            {
                var options = resolver.Resolve(content);

                Assert.Equal(".mp4", options[0].Extension);
                Assert.True(options[0].IsDefault);
                Assert.Contains(options, o => o.Extension == ".webm");
                Assert.Contains(options, o => o.Extension == ".mkv");
                Assert.True(options.Count > 1);
            }
        }
    }
}
