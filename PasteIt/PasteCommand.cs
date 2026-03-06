using System;
using PasteIt.Core;

namespace PasteIt
{
    internal static class PasteCommand
    {
        public static int Execute(string? preferredTargetDirectory, string? extensionOverride, ToastNotification? toast)
        {
            try
            {
                var settings = new SettingsManager().Load();
                var detector = new ClipboardDetector();
                using (var content = detector.Detect())
                {
                    if (content.Type == ClipboardContentType.None)
                    {
                        toast?.ShowError("Clipboard is empty or unsupported.");
                        return 2;
                    }

                    if (content.Type == ClipboardContentType.FileDropList)
                    {
                        toast?.ShowError("Clipboard already contains files.");
                        return 3;
                    }

                    var targetDirectory = ResolveTargetDirectory(preferredTargetDirectory, () => settings);
                    var saver = new FileSaver(() => settings);
                    var saveResult = saver.Save(content, targetDirectory, null, extensionOverride);

                    RecordHistory(content, saveResult);

                    toast?.ShowSuccess(saveResult.DisplayType, saveResult.FilePath);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                toast?.ShowError("Paste failed: " + ex.Message);
                return 1;
            }
        }

        internal static string ResolveTargetDirectory(string? preferredTargetDirectory, Func<AppSettings>? loadSettings = null)
        {
            if (!string.IsNullOrWhiteSpace(preferredTargetDirectory))
            {
                return ExplorerHelper.ResolveTargetDirectory(preferredTargetDirectory);
            }

            try
            {
                var settings = (loadSettings ?? (() => new SettingsManager().Load()))();
                if (!string.IsNullOrWhiteSpace(settings.DefaultSaveLocation))
                {
                    return ExplorerHelper.ResolveTargetDirectory(settings.DefaultSaveLocation);
                }
            }
            catch
            {
            }

            return ExplorerHelper.ResolveTargetDirectory(null);
        }

        private static void RecordHistory(ClipboardContent content, FileSaveResult saveResult)
        {
            try
            {
                var preview = content.Type == ClipboardContentType.Image ||
                              content.Type == ClipboardContentType.Audio ||
                              content.Type == ClipboardContentType.Video
                    ? null
                    : Truncate(content.TextContent, 200);

                var fileSize = 0L;
                try
                {
                    if (System.IO.File.Exists(saveResult.FilePath))
                    {
                        fileSize = new System.IO.FileInfo(saveResult.FilePath).Length;
                    }
                }
                catch
                {
                }

                var entry = new HistoryEntry
                {
                    TimestampUtc = DateTime.UtcNow,
                    FilePath = saveResult.FilePath,
                    ContentType = content.Type.ToString(),
                    DisplayType = saveResult.DisplayType,
                    PreviewText = preview,
                    FileSizeBytes = fileSize
                };

                new HistoryManager().AddEntry(entry);
            }
            catch
            {
                // Best-effort history recording; never break the paste flow.
            }
        }

        private static string? Truncate(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text!.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength) + "…";
        }
    }
}
