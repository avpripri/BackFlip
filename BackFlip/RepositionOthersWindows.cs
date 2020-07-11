using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices; // For the P/Invoke signatures.
using System.Text;

namespace BackFlip
{
    using HWND = IntPtr;

    public static class PositionWindow
    {
        // P/Invoke declarations.
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;

        public static void SendRequest(string windowName, Rectangle position)
        {
            // Find (the first-in-Z-order) Notepad window.
            IntPtr hWnd = FindWindow(null, windowName);

            // If found, position it.
            if (hWnd != IntPtr.Zero)
            {
                // Move the window to (0,0) without changing its size or position
                // in the Z order.
                SetWindowPos(hWnd, IntPtr.Zero, position.Left, position.Top, position.Width, position.Height, SWP_NOZORDER);
            }
        }
    }

}