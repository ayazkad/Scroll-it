using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ScrollIt.Engine
{
    public static class Win32
    {
        public const int WH_MOUSE_LL = 14;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MBUTTONUP = 0x0208;
        public const int WM_XBUTTONDOWN = 0x020B;
        public const int WM_XBUTTONUP = 0x020C;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_MOUSEHWHEEL = 0x020E;
        public const int WHEEL_DELTA = 120;

        // Custom signature flag used in dwExtraInfo to identify synthesized smooth scroll events
        public static readonly UIntPtr SCROLL_IT_SIGNATURE = new UIntPtr(0x5343524C); // 'SCRL' in ASCII

        public const int VK_TAB = 0x09;
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12; // Alt
        public const int VK_ESCAPE = 0x1B;
        public const int VK_PRIOR = 0x21; // Page Up
        public const int VK_NEXT = 0x22;  // Page Down

        public const uint INPUT_MOUSE = 0;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;
        public const uint MOUSEEVENTF_HWHEEL = 0x01000;

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT
        {
            [FieldOffset(0)]
            public uint type;
            [FieldOffset(8)]
            public MOUSEINPUT mi;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Construit le lParam avec les coordonnées écran (X, Y) pour WM_MOUSEWHEEL / WM_MOUSEHWHEEL
        /// Gère correctement les coordonnées d'écrans négatifs (multi-moniteurs).
        /// </summary>
        public static IntPtr MakeLParam(int x, int y)
        {
            return (IntPtr)(((short)y << 16) | ((short)x & 0xFFFF));
        }

        /// <summary>
        /// Construit le wParam avec le delta de défilement et l'état des modificateurs (Ctrl/Shift)
        /// </summary>
        public static IntPtr MakeWParam(short delta, ushort keys = 0)
        {
            return (IntPtr)(((int)delta << 16) | ((int)keys & 0xFFFF));
        }

        /// <summary>
        /// Récupère la fenêtre enfant la plus profonde sous les coordonnées écran
        /// </summary>
        [DllImport("user32.dll")]
        public static extern IntPtr RealChildWindowFromPoint(IntPtr hwndParent, POINT ptParentClientCoords);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT Point);

        public const uint GA_PARENT = 1;
        public const uint GA_ROOT = 2;
        public const uint GA_ROOTOWNER = 3;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        public static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
        public static extern uint TimeEndPeriod(uint uMilliseconds);

        public delegate void TimerProc(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2);

        public const uint TIME_PERIODIC = 1;
        public const uint TIME_KILL_SYNCHRONOUS = 0x0100;

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint timeSetEvent(uint uDelay, uint uResolution, TimerProc lpTimeProc, UIntPtr dwUser, uint fuEvent);

        [DllImport("dwmapi.dll")]
        public static extern int DwmFlush();

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWCP_DEFAULT = 0;
        public const int DWMWCP_DONOTROUND = 1;
        public const int DWMWCP_ROUND = 2;

        public static void EnableWindows11RoundedCorners(IntPtr hwnd)
        {
            SetWindowCornerPreference(hwnd, false);
        }

        public static void SetWindowCornerPreference(IntPtr hwnd, bool isMaximized)
        {
            try
            {
                int cornerPreference = isMaximized ? DWMWCP_DONOTROUND : DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
            }
            catch { }
        }

        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static readonly uint WM_SHOW_SCROLL_IT = RegisterWindowMessage("SCROLL_IT_SHOW_MAIN_WINDOW");

        public const uint SPI_GETWHEELSCROLLLINES = 0x0068;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out uint pvParam, uint fWinIni);

        public static uint GetSystemScrollLines()
        {
            uint lines = 3;
            try
            {
                if (!SystemParametersInfo(SPI_GETWHEELSCROLLLINES, 0, out lines, 0) || lines == 0)
                {
                    lines = 3;
                }
            }
            catch { }
            return lines;
        }

        public static bool IsCtrlPressed()
        {
            return (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
        }

        public static bool IsShiftPressed()
        {
            return (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
        }

        public static bool IsAltPressed()
        {
            return (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, System.Text.StringBuilder lpExeName, ref uint lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private static uint _cachedPid = 0;
        private static string _cachedProcessName = string.Empty;
        private static long _cacheTimestamp = 0;

        public static string GetProcessNameUnderCursor(POINT pt)
        {
            try
            {
                IntPtr hWnd = WindowFromPoint(pt);
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                }

                if (hWnd != IntPtr.Zero)
                {
                    uint processId;
                    GetWindowThreadProcessId(hWnd, out processId);
                    if (processId != 0)
                    {
                        long now = Environment.TickCount;
                        if (processId == _cachedPid && (now - _cacheTimestamp < 2000))
                        {
                            return _cachedProcessName;
                        }

                        string procName = string.Empty;
                        IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
                        if (hProcess != IntPtr.Zero)
                        {
                            try
                            {
                                System.Text.StringBuilder sb = new System.Text.StringBuilder(1024);
                                uint size = (uint)sb.Capacity;
                                if (QueryFullProcessImageName(hProcess, 0, sb, ref size))
                                {
                                    procName = System.IO.Path.GetFileNameWithoutExtension(sb.ToString()).ToLowerInvariant();
                                }
                            }
                            finally
                            {
                                CloseHandle(hProcess);
                            }
                        }

                        if (string.IsNullOrEmpty(procName))
                        {
                            try
                            {
                                Process proc = Process.GetProcessById((int)processId);
                                procName = proc.ProcessName.ToLowerInvariant();
                            }
                            catch { }
                        }

                        _cachedPid = processId;
                        _cachedProcessName = procName;
                        _cacheTimestamp = now;
                        return procName;
                    }
                }
            }
            catch { }
            return string.Empty;
        }
    }
}
