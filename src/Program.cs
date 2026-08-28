using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using ScrollIt.Engine;
using ScrollIt.UI;

namespace ScrollIt
{
    public static class Program
    {
        private static MainWindow _mainWindow;
        private static Application _wpfApp;
        private static Mutex _singleInstanceMutex;
        private static EventWaitHandle _showAppEvent;

        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                // Silently handle background thread errors
            };

            // 1. Instant Single Instance Check via Global Named Mutex & Event
            const string mutexName = @"Global\ScrollIt_SingleInstance_Mutex_App";
            const string eventName = @"Global\ScrollIt_Show_Event";

            bool isNewInstance;
            try
            {
                _singleInstanceMutex = new Mutex(true, mutexName, out isNewInstance);
            }
            catch
            {
                isNewInstance = true;
            }

            if (!isNewInstance)
            {
                // Signal already running instance to show itself immediately (0 ms)
                try
                {
                    using (EventWaitHandle showEvent = EventWaitHandle.OpenExisting(eventName))
                    {
                        showEvent.Set();
                    }
                }
                catch { }

                Win32.PostMessage(Win32.HWND_BROADCAST, Win32.WM_SHOW_SCROLL_IT, IntPtr.Zero, IntPtr.Zero);
                return;
            }

            try
            {
                // Create named show event for subsequent instances
                bool createdNewEvent;
                _showAppEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName, out createdNewEvent);

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    while (true)
                    {
                        try
                        {
                            if (_showAppEvent == null) break;
                            _showAppEvent.WaitOne();
                            ShowMainWindow();
                        }
                        catch
                        {
                            break;
                        }
                    }
                });

                // 2. Initialize High-Precision Physics Engine & Mouse Hook (Instant < 10ms)
                ScrollPhysics.Initialize();
                MouseHook.Install();

                // 3. Initialize WPF Application with Explicit Shutdown
                _wpfApp = new Application();
                _wpfApp.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // 4. Initialize System Tray Icon
                TrayManager.Initialize(ShowMainWindow);

                // Check command line arguments
                bool startMinimized = false;
                if (args != null)
                {
                    foreach (string arg in args)
                    {
                        if (arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                            arg.Equals("-minimized", StringComparison.OrdinalIgnoreCase))
                        {
                            startMinimized = true;
                            break;
                        }
                    }
                }

                if (!startMinimized)
                {
                    ShowMainWindow();
                }

                _wpfApp.Run();
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.WriteAllText("error.log", ex.ToString());
                }
                catch { }
            }
            finally
            {
                MouseHook.Uninstall();
                ScrollPhysics.Shutdown();
                if (_singleInstanceMutex != null)
                {
                    try { _singleInstanceMutex.ReleaseMutex(); } catch { }
                    _singleInstanceMutex.Dispose();
                }
                if (_showAppEvent != null)
                {
                    _showAppEvent.Dispose();
                }
            }
        }

        public static void ShowMainWindow()
        {
            if (_wpfApp == null) return;

            _wpfApp.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (_mainWindow == null)
                    {
                        _mainWindow = new MainWindow();
                        _mainWindow.Closed += (s, e) => { _mainWindow = null; };
                    }

                    if (!_mainWindow.IsVisible)
                    {
                        _mainWindow.Show();
                    }

                    if (_mainWindow.WindowState == WindowState.Minimized)
                    {
                        _mainWindow.WindowState = WindowState.Normal;
                    }

                    _mainWindow.Activate();
                    _mainWindow.Focus();
                    _mainWindow.Topmost = true;
                    _mainWindow.Topmost = false;
                }
                catch { }
            }));
        }
    }
}
