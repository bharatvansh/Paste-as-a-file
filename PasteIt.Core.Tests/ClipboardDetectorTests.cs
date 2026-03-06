using System;
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
    }
}
