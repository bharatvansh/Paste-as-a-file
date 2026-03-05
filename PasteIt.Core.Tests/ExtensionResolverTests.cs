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
    }
}
