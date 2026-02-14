using PasteIt.Core;
using Xunit;

namespace PasteIt.Core.Tests
{
    public class CodeLanguageDetectorTests
    {
        [Fact]
        public void Detect_ReturnsPython_WhenPythonPatternsPresent()
        {
            var detector = new CodeLanguageDetector();
            var text = "import os\n\ndef main():\n    print(\"hello\")\n";

            var result = detector.Detect(text);

            Assert.True(result.IsCode);
            Assert.Equal("Python", result.Language);
            Assert.Equal(".py", result.Extension);
        }

        [Fact]
        public void Detect_ReturnsJson_WhenJsonStructurePresent()
        {
            var detector = new CodeLanguageDetector();
            var text = "{ \"name\": \"paste-it\", \"enabled\": true }";

            var result = detector.Detect(text);

            Assert.True(result.IsCode);
            Assert.Equal("JSON", result.Language);
            Assert.Equal(".json", result.Extension);
        }

        [Fact]
        public void Detect_ReturnsNoMatch_ForPlainSentence()
        {
            var detector = new CodeLanguageDetector();
            var text = "This is a regular sentence with punctuation, but no code syntax at all.";

            var result = detector.Detect(text);

            Assert.False(result.IsCode);
            Assert.Equal(".txt", result.Extension);
        }
    }
}

