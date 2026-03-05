namespace PasteIt.Core
{
    public sealed class FileExtensionOption
    {
        public FileExtensionOption(string label, string extension, bool isDefault)
        {
            Label = label;
            Extension = extension;
            IsDefault = isDefault;
        }

        public string Label { get; }

        public string Extension { get; }

        public bool IsDefault { get; }

        public string DisplayText => $"{Label} ({Extension})";
    }
}
