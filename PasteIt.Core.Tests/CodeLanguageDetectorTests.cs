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

        [Fact]
        public void Detect_ReturnsTypeScript_ForInterfaceAndTypedMembers()
        {
            var detector = new CodeLanguageDetector();
            var text = "interface User { id: number; name: string; }\nconst u: User = { id: 1, name: \"A\" };";

            var result = detector.Detect(text);

            Assert.True(result.IsCode);
            Assert.Equal("TypeScript", result.Language);
            Assert.Equal(".ts", result.Extension);
        }

        [Fact]
        public void Detect_ReturnsJava_ForMainClassSnippet()
        {
            var detector = new CodeLanguageDetector();
            var text = "public class Hello { public static void main(String[] args) { System.out.println(\"hi\"); } }";

            var result = detector.Detect(text);

            Assert.True(result.IsCode);
            Assert.Equal("Java", result.Language);
            Assert.Equal(".java", result.Extension);
        }

        [Fact]
        public void Detect_ReturnsPowerShell_ForCmdletPipeline()
        {
            var detector = new CodeLanguageDetector();
            var text = "param($Path)\nGet-ChildItem $Path | Where-Object { $_.Length -gt 0 } | ForEach-Object { $_.Name }";

            var result = detector.Detect(text);

            Assert.True(result.IsCode);
            Assert.Equal("PowerShell", result.Language);
            Assert.Equal(".ps1", result.Extension);
        }

        [Fact]
        public void Detect_ReturnsShell_ForBashScript()
        {
            var detector = new CodeLanguageDetector();
            var text = "#!/bin/bash\nexport NAME=world\nif [ -n \"$NAME\" ]; then\n  echo \"hello $NAME\"\nfi";

            var result = detector.Detect(text);

            Assert.True(result.IsCode);
            Assert.Equal("Shell", result.Language);
            Assert.Equal(".sh", result.Extension);
        }

        [Fact]
        public void Detect_ReturnsPhp_ForPhpSnippet()
        {
            var detector = new CodeLanguageDetector();
            var text = "<?php\n$greeting = \"hi\";\necho $greeting;\n";

            var result = detector.Detect(text);

            Assert.True(result.IsCode);
            Assert.Equal("PHP", result.Language);
            Assert.Equal(".php", result.Extension);
        }

        [Fact]
        public void Detect_ReturnsCss_ForCssSnippet()
        {
            var detector = new CodeLanguageDetector();
            var text = ":root { --brand: #1e40af; }\n.card { display: grid; grid-template-columns: 1fr 2fr; margin: 12px; }";

            var result = detector.Detect(text);

            Assert.True(result.IsCode);
            Assert.Equal("CSS", result.Language);
            Assert.Equal(".css", result.Extension);
        }
    }
}
