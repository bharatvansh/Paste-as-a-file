namespace PasteIt.Core
{
    public sealed class FileSaveResult
    {
        public FileSaveResult(string filePath, string displayType)
        {
            FilePath = filePath;
            DisplayType = displayType;
        }

        public string FilePath { get; }

        public string DisplayType { get; }
    }
}

