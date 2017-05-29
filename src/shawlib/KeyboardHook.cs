using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShawLib
{
    public class KeyHookEventArgs : EventArgs
    {
        public Keys Key;
    }

    public class KeyboardHook : IDisposable
    {
        IntPtr hook = IntPtr.Zero;
        Func<KeyHookEventArgs, bool> OnKey;
        bool disposed = false;

        public KeyboardHook(Func<KeyHookEventArgs, bool> onKey)
        {
            OnKey = onKey;
            hook = NativeMethods.SetWindowsHookEx(HookType.WH_KEYBOARD_LL, HookCallback, IntPtr.Zero, 0);
        }

        IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && wParam == (IntPtr)WindowsMessages.KEYDOWN || wParam == (IntPtr)WindowsMessages.SYSKEYDOWN)
            {
                var key = Marshal.ReadInt32(lParam);
                var forward = OnKey(new KeyHookEventArgs()
                {
                    Key = (Keys)key
                });
                if (!forward)
                    return lParam;
            }
            return NativeMethods.CallNextHookEx(hook, code, wParam, lParam);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {

            }

            NativeMethods.UnhookWindowsHookEx(hook);
            disposed = true;
        }
    }
}
