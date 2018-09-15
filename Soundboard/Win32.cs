using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Soundboard
{

    internal static class Win32
    {
        private const string User32 = "user32.dll";
        private const string Kernel32 = "kernel32.dll";

        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;

        [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
        public class HookHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private HookHandle()
                : base(true) { }

            protected override bool ReleaseHandle()
            {
                return UnhookWindowsHookEx(handle);
            }
        }

        public delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport(User32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern HookHandle SetWindowsHookEx(int hook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport(User32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport(User32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(HookHandle handle, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
