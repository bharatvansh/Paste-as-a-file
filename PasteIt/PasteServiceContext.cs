using System.Windows.Forms;
using PasteIt.Core;

namespace PasteIt
{
    internal sealed class PasteServiceContext : ApplicationContext
    {
        private readonly HotkeyManager _hotkeyManager;
        private ToastNotification? _toast;

        public PasteServiceContext()
        {
            _hotkeyManager = new HotkeyManager();
            try
            {
                _hotkeyManager.HotkeyPressed += HandleHotkeyPressed;
                _hotkeyManager.Register(Keys.V, KeyModifiers.Control | KeyModifiers.Shift);
            }
            catch
            {
                _hotkeyManager.HotkeyPressed -= HandleHotkeyPressed;
                _hotkeyManager.Dispose();
                _toast?.Dispose();
                throw;
            }
        }

        private void HandleHotkeyPressed(object? sender, System.EventArgs e)
        {
            if (!ExplorerHelper.IsExplorerInForeground())
            {
                return;
            }

            PasteCommand.Execute(null, null, EnsureToast());
        }

        protected override void ExitThreadCore()
        {
            _hotkeyManager.HotkeyPressed -= HandleHotkeyPressed;
            _hotkeyManager.Dispose();
            _toast?.Dispose();

            base.ExitThreadCore();
        }

        private ToastNotification EnsureToast()
        {
            if (_toast == null)
            {
                _toast = new ToastNotification();
            }

            return _toast;
        }
    }
}
