using System.Windows.Forms;
using PasteIt.Core;

namespace PasteIt
{
    internal sealed class PasteServiceContext : ApplicationContext
    {
        private readonly HotkeyManager _hotkeyManager;
        private readonly ToastNotification _toast;

        public PasteServiceContext()
        {
            _toast = new ToastNotification();
            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.HotkeyPressed += HandleHotkeyPressed;
            _hotkeyManager.Register(Keys.V, KeyModifiers.Control | KeyModifiers.Shift);
        }

        private void HandleHotkeyPressed(object? sender, System.EventArgs e)
        {
            if (!ExplorerHelper.IsExplorerInForeground())
            {
                return;
            }

            PasteCommand.Execute(null, null, _toast);
        }

        protected override void ExitThreadCore()
        {
            _hotkeyManager.HotkeyPressed -= HandleHotkeyPressed;
            _hotkeyManager.Dispose();
            _toast.Dispose();

            base.ExitThreadCore();
        }
    }
}

