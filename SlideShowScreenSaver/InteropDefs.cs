using System;
using System.Runtime.InteropServices;

namespace SlideShowScreenSaver
{
    //[Serializable, StructLayout(LayoutKind.Sequential)]
    public class Win32API
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);
    }
    
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public abstract class WindowStyles
    {
        public const uint WS_CHILD = 0x40000000;
        public const uint WS_VISIBLE = 0x10000000;
        public const uint WS_CLIPCHILDREN = 0x02000000;
    }
}