using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PasteIt
{
    [Flags]
    internal enum KeyModifiers : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008
    }

    internal sealed class HotkeyManager : NativeWindow, IDisposable
    {
        private const int WmHotkey = 0x0312;
        private readonly HashSet<int> _registeredHotkeyIds = new HashSet<int>();
        private int _nextHotkeyId;
        private bool _disposed;

        public HotkeyManager()
        {
            CreateHandle(new CreateParams());
        }

        public event EventHandler? HotkeyPressed;

        public int Register(Keys key, KeyModifiers modifiers)
        {
            ThrowIfDisposed();

            var id = ++_nextHotkeyId;
            if (!RegisterHotKey(Handle, id, (uint)modifiers, (uint)key))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to register global hotkey.");
            }

            _registeredHotkeyIds.Add(id);
            return id;
        }

        public void UnregisterAll()
        {
            foreach (var hotkeyId in _registeredHotkeyIds)
            {
                UnregisterHotKey(Handle, hotkeyId);
            }

            _registeredHotkeyIds.Clear();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            UnregisterAll();
            DestroyHandle();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HotkeyManager));
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}

