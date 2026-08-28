using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace ScrollIt.Engine
{
    public static class MouseHook
    {
        private static readonly object _syncLock = new object();
        private static readonly Win32.LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookId = IntPtr.Zero;
        private static volatile bool _isHooked = false;

        // Watchdog & Résilience
        private static Timer _watchdogTimer;
        private static bool _sessionSwitchRegistered = false;
        private const int WatchdogIntervalMs = 5000;

        public static void Install()
        {
            lock (_syncLock)
            {
                if (_isHooked && _hookId != IntPtr.Zero) return;

                InstallHookInternal();
                StartWatchdog();
                RegisterSessionEvents();
            }
        }

        public static void Uninstall()
        {
            lock (_syncLock)
            {
                StopWatchdog();
                UnregisterSessionEvents();
                UninstallHookInternal();
            }
        }

        public static void Reinstall()
        {
            lock (_syncLock)
            {
                UninstallHookInternal();
                InstallHookInternal();
            }
        }

        private static void InstallHookInternal()
        {
            try
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _hookId = Win32.SetWindowsHookEx(
                        Win32.WH_MOUSE_LL,
                        _proc,
                        Win32.GetModuleHandle(curModule.ModuleName),
                        0
                    );
                }

                _isHooked = (_hookId != IntPtr.Zero);
            }
            catch
            {
                _hookId = IntPtr.Zero;
                _isHooked = false;
            }
        }

        private static void UninstallHookInternal()
        {
            try
            {
                if (_hookId != IntPtr.Zero)
                {
                    Win32.UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                }
            }
            catch { }
            finally
            {
                _isHooked = false;
            }
        }

        #region Watchdog & Session Switch Resilience

        private static void StartWatchdog()
        {
            if (_watchdogTimer == null)
            {
                _watchdogTimer = new Timer(WatchdogCallback, null, WatchdogIntervalMs, WatchdogIntervalMs);
            }
        }

        private static void StopWatchdog()
        {
            if (_watchdogTimer != null)
            {
                _watchdogTimer.Dispose();
                _watchdogTimer = null;
            }
        }

        private static void WatchdogCallback(object state)
        {
            try
            {
                lock (_syncLock)
                {
                    // Si le hook est censé être actif mais que le handle est invalide
                    if (_isHooked && _hookId == IntPtr.Zero)
                    {
                        InstallHookInternal();
                    }
                }
            }
            catch { }
        }

        private static void RegisterSessionEvents()
        {
            if (!_sessionSwitchRegistered)
            {
                try
                {
                    SystemEvents.SessionSwitch += OnSessionSwitch;
                    _sessionSwitchRegistered = true;
                }
                catch { }
            }
        }

        private static void UnregisterSessionEvents()
        {
            if (_sessionSwitchRegistered)
            {
                try
                {
                    SystemEvents.SessionSwitch -= OnSessionSwitch;
                    _sessionSwitchRegistered = false;
                }
                catch { }
            }
        }

        private static void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            // Réactivation automatique après déverrouillage de session (Win+L), réouverture de session ou reconnexion
            if (e.Reason == SessionSwitchReason.SessionUnlock ||
                e.Reason == SessionSwitchReason.SessionLogon ||
                e.Reason == SessionSwitchReason.ConsoleConnect ||
                e.Reason == SessionSwitchReason.RemoteConnect)
            {
                Reinstall();
            }
        }

        #endregion

        public static bool IsExcludedOrIncompatible(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return false;

            // Applications Win32 à rendu textuel par ligne fixe / consoles
            if (processName == "notepad" || 
                processName == "cmd" || 
                processName == "powershell" || 
                processName == "pwsh" || 
                processName == "conhost" || 
                processName == "regedit" || 
                processName == "windowsterminal")
            {
                return true;
            }

            // Exclusions personnalisées configurées par l'utilisateur
            var blacklisted = SettingsManager.Current != null ? SettingsManager.Current.BlacklistedApps : null;
            if (blacklisted != null && blacklisted.Count > 0)
            {
                for (int i = 0; i < blacklisted.Count; i++)
                {
                    string entry = blacklisted[i];
                    if (!string.IsNullOrEmpty(entry) &&
                        processName.IndexOf(entry, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                int msg = (int)wParam;
                if (nCode >= 0)
                {
                    // 1. Zéro-latence pour le mouvement de souris (crucial pour souris 4000Hz/8000Hz)
                    if (msg == Win32.WM_MOUSEMOVE)
                    {
                        ScrollPhysics.OnMouseMove();
                        return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);
                    }

                    if (msg == Win32.WM_MOUSEWHEEL || msg == Win32.WM_MOUSEHWHEEL)
                    {
                        Win32.MSLLHOOKSTRUCT hookStruct = (Win32.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.MSLLHOOKSTRUCT));

                        // 2. Pass-through pour les événements synthétiques générés par le moteur physique
                        if (hookStruct.dwExtraInfo == Win32.SCROLL_IT_SIGNATURE)
                        {
                            return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);
                        }

                        // 3. Pass-through si le moteur est désactivé
                        if (SettingsManager.Current == null || !SettingsManager.Current.Enabled)
                        {
                            return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);
                        }

                        // 4. Pass-through immédiat Ctrl + Molette (zoom natif direct sans inertie)
                        if (Win32.IsCtrlPressed())
                        {
                            return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);
                        }

                        // 5. Pass-through natif direct pour applications incompatibles ou dans la liste noire
                        string targetProcess = Win32.GetProcessNameUnderCursor(hookStruct.pt);
                        if (IsExcludedOrIncompatible(targetProcess))
                        {
                            return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);
                        }

                        short delta = (short)((hookStruct.mouseData >> 16) & 0xffff);
                        if (delta == 0)
                        {
                            return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);
                        }

                        // 6. Filtre et Pass-Through pour Pavés Tactiles de Précision (PTP) & molettes haute résolution
                        // Les trackpads et surfaces tactiles envoient des deltas continus et fractionnaires (non multiples de 120)
                        // Déjà gérés de façon optimale par DirectManipulation / le pilote matériel de Windows
                        if (Math.Abs(delta) < Win32.WHEEL_DELTA || (delta % Win32.WHEEL_DELTA != 0))
                        {
                            return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);
                        }

                        // 7. Interception du cran physique & injection dans le moteur physique cadencé
                        bool isHorizontal = (msg == Win32.WM_MOUSEHWHEEL);
                        ScrollPhysics.OnWheel((int)delta, isHorizontal, hookStruct.pt);

                        // Suppression du cran saccadé original
                        return (IntPtr)1;
                    }
                }
            }
            catch
            {
                // En cas d'erreur exceptionnelle, ne jamais bloquer la chaîne de hooks Windows
            }

            return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }
}
