using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PasteIt
{
    internal sealed class ToastNotification : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private bool _disposed;

        public ToastNotification()
        {
            _notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Application,
                Text = "PasteIt"
            };
        }

        public void ShowSuccess(string displayType, string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            Show("Paste as File", $"Saved {displayType} as {fileName}", ToolTipIcon.Info);
        }

        public void ShowError(string message)
        {
            Show("Paste as File", message, ToolTipIcon.Error);
        }

        private void Show(string title, string message, ToolTipIcon icon)
        {
            if (_disposed)
            {
                return;
            }

            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(2500);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
