namespace PasteIt.Core
{
    public sealed class CodeDetectionResult
    {
        public CodeDetectionResult(
            bool isCode,
            string language,
            string extension,
            int score,
            int threshold)
        {
            IsCode = isCode;
            Language = language;
            Extension = extension;
            Score = score;
            Threshold = threshold;
        }

        public bool IsCode { get; }

        public string Language { get; }

        public string Extension { get; }

        public int Score { get; }

        public int Threshold { get; }

        public static CodeDetectionResult NoMatch() =>
            new CodeDetectionResult(false, "Text", ".txt", 0, 0);
    }
}

