using System.Runtime.InteropServices;

namespace ComputerClub.Client;

/// Foydalanuvchining bloklangan ekrandan chiqishiga urinishini ushlab turadi.
/// Alt+Tab, Alt+F4, Win tugmasi va boshqa shortcut'lar serverga yuborilmaydi.
public sealed class LockGuard : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private const int VK_TAB = 0x09;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_F4 = 0x73;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_APPS = 0x5D;
    private const int VK_TAB2 = 0x0F;
    private const int VK_DELETE = 0x2E;
    private const int VK_C = 0x43;
    private const int VK_L = 0x4C;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam, IntPtr uExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private IntPtr _hook = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc;
    private bool _disposed;

    public bool IsActive => _hook != IntPtr.Zero;

    public void Start()
    {
        if (_hook != IntPtr.Zero)
        {
            return;
        }

        _proc = HookCallback;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
    }

    public void Stop()
    {
        if (_hook == IntPtr.Zero)
        {
            return;
        }

        UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
        _proc = null;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam, IntPtr uExtraInfo)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            bool alt = (GetKeyState(0x12) & 0x8000) != 0;
            bool ctrl = (GetKeyState(0x11) & 0x8000) != 0;

            // Alt+Tab / Alt+Shift+Tab / Alt+Esc
            if (alt && (data.vkCode == VK_TAB || data.vkCode == VK_TAB2 || data.vkCode == VK_ESCAPE))
            {
                return (IntPtr)1;
            }

            // Alt+F4
            if (alt && data.vkCode == VK_F4)
            {
                return (IntPtr)1;
            }

            // Win tugmasi va kontekst menyu
            if (data.vkCode is VK_LWIN or VK_RWIN or VK_APPS)
            {
                return (IntPtr)1;
            }

            // Ctrl+Alt+Del
            if (ctrl && alt && data.vkCode == VK_DELETE)
            {
                return (IntPtr)1;
            }

            // Ctrl+Alt+C/L — kopyalash va yangi oyna
            if (ctrl && alt && data.vkCode is VK_C or VK_L)
            {
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }
}