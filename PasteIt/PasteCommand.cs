using System;
using PasteIt.Core;

namespace PasteIt
{
    internal static class PasteCommand
    {
        public static int Execute(string? preferredTargetDirectory, ToastNotification? toast)
        {
            try
            {
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

                    var targetDirectory = ExplorerHelper.ResolveTargetDirectory(preferredTargetDirectory);
                    var saver = new FileSaver();
                    var saveResult = saver.Save(content, targetDirectory);

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
    }
}

