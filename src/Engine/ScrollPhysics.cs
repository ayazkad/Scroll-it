using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ScrollIt.Engine
{
    public static class ScrollPhysics
    {
        private static readonly object _syncLock = new object();
        private static Thread _physicsThread;
        private static readonly AutoResetEvent _wakeEvent = new AutoResetEvent(false);
        private static volatile bool _isRunning = true;

        // Window & Target Latching State
        private static IntPtr _latchedHwnd = IntPtr.Zero;
        private static IntPtr _targetRootHwnd = IntPtr.Zero;
        private static IntPtr _initialForegroundRoot = IntPtr.Zero;
        private static Win32.POINT _latchedPoint;
        private static bool _isShiftHorizontal = false;

        // Vertical Velocity & State (WebKit Momentum)
        private static double _velocityY = 0.0; // px/ms
        private static double _subPixelY = 0.0;
        private static long _lastTickTimeY = 0;
        private static long _lastWheelTimestampY = 0;
        private static double _lastWheelDirectionY = 0.0;
        private static double _currentAccelY = 1.0;

        // Horizontal Velocity & State (WebKit Momentum)
        private static double _velocityX = 0.0; // px/ms
        private static double _subPixelX = 0.0;
        private static long _lastTickTimeX = 0;
        private static long _lastWheelTimestampX = 0;
        private static double _lastWheelDirectionX = 0.0;
        private static double _currentAccelX = 1.0;

        // Constantes physiques haute fréquence (144Hz - 240Hz)
        public const double WebKitFriction = 0.998;
        public const double CutoffVelocity = 0.005; // Seuil de coupure précis adapté aux écrans 144Hz/240Hz
        private const int TargetFrameTimeMs = 4;     // Cadencement ultra-rapide jusqu'à 240 Hz (DWM V-Sync natif)

        // Win32 Modifier Keys pour wParam
        private const ushort MK_CONTROL = 0x0008;
        private const ushort MK_SHIFT = 0x0004;

        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public static void Initialize()
        {
            Win32.TimeBeginPeriod(1);

            _physicsThread = new Thread(VsyncPhysicsLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "ScrollIt_PrecisionPhysicsEngine"
            };
            _physicsThread.Start();
        }

        public static void Shutdown()
        {
            _isRunning = false;
            _wakeEvent.Set();
            Win32.TimeEndPeriod(1);
        }

        public static void Stop()
        {
            lock (_syncLock)
            {
                ResetPhysicsState();
            }
        }

        public static void OnMouseMove()
        {
            if (_targetRootHwnd == IntPtr.Zero) return;
            if (_velocityY == 0.0 && _velocityX == 0.0) return;

            Win32.POINT curPt;
            if (Win32.GetCursorPos(out curPt))
            {
                IntPtr curHwnd = Win32.WindowFromPoint(curPt);
                IntPtr curRoot = Win32.GetAncestor(curHwnd, Win32.GA_ROOT);
                if (curRoot == IntPtr.Zero) curRoot = curHwnd;

                if (curRoot != _targetRootHwnd)
                {
                    lock (_syncLock)
                    {
                        ResetPhysicsState();
                    }
                }
            }
        }

        public static void OnWheel(int rawDelta, bool isHorizontal, Win32.POINT pt)
        {
            if (!SettingsManager.Current.Enabled) return;

            long now = _stopwatch.ElapsedMilliseconds;
            double stepBase = SettingsManager.Current.StepSize;
            double maxAccel = SettingsManager.Current.AccelerationMultiplier;
            double friction = Math.Max(0.980, Math.Min(0.9995, 0.995 + (SettingsManager.Current.FrictionTail - 0.5) * 0.005));

            double lambda = -Math.Log(friction);
            double notchRatio = (double)rawDelta / 120.0;
            double baseDistance = stepBase * notchRatio;

            // Récupération de la cible hors verrou
            IntPtr targetHwnd = Win32.WindowFromPoint(pt);
            IntPtr rootHwnd = Win32.GetAncestor(targetHwnd, Win32.GA_ROOT);
            if (rootHwnd == IntPtr.Zero) rootHwnd = targetHwnd;

            lock (_syncLock)
            {
                long elapsedSinceLastWheel = now - _lastWheelTimestampY;

                // Si on change de fenêtre racine OU si plus de 150 ms se sont écoulées (nouveau geste)
                if (rootHwnd != _targetRootHwnd || elapsedSinceLastWheel > 150 || (_velocityY == 0.0 && _velocityX == 0.0))
                {
                    // Si changement d'application/fenêtre, on remet à zéro l'inertie précédente
                    if (rootHwnd != _targetRootHwnd)
                    {
                        _velocityY = 0.0;
                        _subPixelY = 0.0;
                        _velocityX = 0.0;
                        _subPixelX = 0.0;
                    }

                    _latchedHwnd = targetHwnd;
                    _targetRootHwnd = rootHwnd;

                    IntPtr fg = Win32.GetForegroundWindow();
                    IntPtr fgRoot = Win32.GetAncestor(fg, Win32.GA_ROOT);
                    _initialForegroundRoot = (fgRoot != IntPtr.Zero) ? fgRoot : fg;
                }

                // Le point d'impact physique est TOUJOURS rafraîchi lors d'un vrai coup de molette
                _latchedPoint = pt;

                double direction = rawDelta > 0 ? 1.0 : -1.0;

                if (!isHorizontal)
                {
                    _isShiftHorizontal = false;

                    // Annuler tout mouvement horizontal lors d'un scroll vertical
                    _velocityX = 0.0;
                    _subPixelX = 0.0;
                    _currentAccelX = 1.0;
                    _lastWheelDirectionX = 0.0;

                    bool isReversal = (_lastWheelDirectionY != 0 && _lastWheelDirectionY != direction);

                    if (isReversal)
                    {
                        _currentAccelY = 1.0;
                        _velocityY = baseDistance * lambda;
                    }
                    else
                    {
                        if (elapsedSinceLastWheel < 200 && elapsedSinceLastWheel > 0)
                        {
                            double factor = 1.0 + (200.0 - elapsedSinceLastWheel) / 200.0 * (maxAccel - 1.0) * 0.75;
                            _currentAccelY = Math.Min(maxAccel, _currentAccelY * 1.15 + factor * 0.25);
                        }
                        else
                        {
                            _currentAccelY = 1.0;
                        }

                        double impulse = (baseDistance * lambda) * _currentAccelY;
                        _velocityY += impulse;
                    }

                    _lastTickTimeY = now;
                    _lastWheelTimestampY = now;
                    _lastWheelDirectionY = direction;
                }
                else
                {
                    _isShiftHorizontal = Win32.IsShiftPressed();

                    // Annuler tout mouvement vertical lors d'un scroll horizontal
                    _velocityY = 0.0;
                    _subPixelY = 0.0;
                    _currentAccelY = 1.0;
                    _lastWheelDirectionY = 0.0;

                    bool isReversal = (_lastWheelDirectionX != 0 && _lastWheelDirectionX != direction);

                    if (isReversal)
                    {
                        _currentAccelX = 1.0;
                        _velocityX = baseDistance * lambda;
                    }
                    else
                    {
                        long elapsedX = now - _lastWheelTimestampX;
                        if (elapsedX < 200 && elapsedX > 0)
                        {
                            double factor = 1.0 + (200.0 - elapsedX) / 200.0 * (maxAccel - 1.0) * 0.75;
                            _currentAccelX = Math.Min(maxAccel, _currentAccelX * 1.15 + factor * 0.25);
                        }
                        else
                        {
                            _currentAccelX = 1.0;
                        }

                        double impulse = (baseDistance * lambda) * _currentAccelX;
                        _velocityX += impulse;
                    }

                    _lastTickTimeX = now;
                    _lastWheelTimestampX = now;
                    _lastWheelDirectionX = direction;
                }
            }

            _wakeEvent.Set();
        }

        private static void VsyncPhysicsLoop()
        {
            while (_isRunning)
            {
                bool hasWork;
                lock (_syncLock)
                {
                    hasWork = (_velocityY != 0.0 || _velocityX != 0.0);
                }

                if (!hasWork)
                {
                    _wakeEvent.WaitOne(20);
                    continue;
                }

                // Arrêt immédiat de l'inertie en cours si Ctrl ou Alt est pressé (zoom, raccourci, menu)
                if (Win32.IsCtrlPressed() || Win32.IsAltPressed())
                {
                    lock (_syncLock)
                    {
                        ResetPhysicsState();
                    }
                    continue;
                }

                // Si Shift est pressé en vol pendant un scroll vertical, couper immédiatement le scroll vertical
                lock (_syncLock)
                {
                    if (Win32.IsShiftPressed() && _velocityY != 0.0)
                    {
                        _velocityY = 0.0;
                        _subPixelY = 0.0;
                    }

                    // Si le scroll horizontal a été initié avec Shift et que Shift est relâché, couper immédiatement le scroll horizontal
                    if (_isShiftHorizontal && !Win32.IsShiftPressed() && _velocityX != 0.0)
                    {
                        _velocityX = 0.0;
                        _subPixelX = 0.0;
                        _currentAccelX = 1.0;
                        _lastWheelDirectionX = 0.0;
                        _isShiftHorizontal = false;
                    }
                }

                // Vérification de changement d'application / fenêtre de premier plan
                lock (_syncLock)
                {
                    if (_initialForegroundRoot != IntPtr.Zero)
                    {
                        IntPtr curFg = Win32.GetForegroundWindow();
                        IntPtr curFgRoot = Win32.GetAncestor(curFg, Win32.GA_ROOT);
                        if (curFgRoot == IntPtr.Zero) curFgRoot = curFg;

                        if (curFgRoot != _initialForegroundRoot)
                        {
                            ResetPhysicsState();
                            continue;
                        }
                    }

                    if (_latchedHwnd != IntPtr.Zero && !Win32.IsWindow(_latchedHwnd))
                    {
                        ResetPhysicsState();
                        continue;
                    }
                }

                // Synchronisation V-Sync matérielle via DWM (60, 120, 144, 240 Hz)
                // Élimine le frame jitter en se calant sur le rafraîchissement vertical exact du moniteur
                try
                {
                    if (Win32.DwmFlush() != 0)
                    {
                        Thread.Sleep(TargetFrameTimeMs);
                    }
                }
                catch
                {
                    Thread.Sleep(TargetFrameTimeMs);
                }

                long now = _stopwatch.ElapsedMilliseconds;
                int deltaYToSend = 0;
                int deltaXToSend = 0;
                IntPtr dispatchHwnd = IntPtr.Zero;
                Win32.POINT dispatchPt = new Win32.POINT();
                double friction = Math.Max(0.980, Math.Min(0.9995, 0.995 + (SettingsManager.Current.FrictionTail - 0.5) * 0.005));

                lock (_syncLock)
                {
                    dispatchHwnd = _latchedHwnd;
                    dispatchPt = _latchedPoint;

                    // 1. Momentum Vertical
                    if (_velocityY != 0.0)
                    {
                        double dt = (double)(now - _lastTickTimeY);
                        if (dt > 0)
                        {
                            if (dt > 64.0) dt = 64.0;
                            _lastTickTimeY = now;

                            double step = _velocityY * dt;
                            _subPixelY += step;
                            _velocityY *= Math.Pow(friction, dt);

                            int intY = (int)Math.Truncate(_subPixelY);
                            if (intY != 0)
                            {
                                deltaYToSend = intY;
                                _subPixelY -= intY;
                            }

                            // Arrêt propre sans kick résiduel
                            if (Math.Abs(_velocityY) < CutoffVelocity)
                            {
                                _velocityY = 0.0;
                                _subPixelY = 0.0; // On jette la fraction sans l'envoyer
                                _lastWheelDirectionY = 0.0;
                            }
                        }
                    }

                    // 2. Momentum Horizontal
                    if (_velocityX != 0.0)
                    {
                        double dt = (double)(now - _lastTickTimeX);
                        if (dt > 0)
                        {
                            if (dt > 64.0) dt = 64.0;
                            _lastTickTimeX = now;

                            double step = _velocityX * dt;
                            _subPixelX += step;
                            _velocityX *= Math.Pow(friction, dt);

                            int intX = (int)Math.Truncate(_subPixelX);
                            if (intX != 0)
                            {
                                deltaXToSend = intX;
                                _subPixelX -= intX;
                            }

                            // Arrêt propre sans kick résiduel
                            if (Math.Abs(_velocityX) < CutoffVelocity)
                            {
                                _velocityX = 0.0;
                                _subPixelX = 0.0; // On jette la fraction sans l'envoyer
                                _lastWheelDirectionX = 0.0;
                            }
                        }
                    }

                    if (_velocityY == 0.0 && _velocityX == 0.0)
                    {
                        _latchedHwnd = IntPtr.Zero;
                        _targetRootHwnd = IntPtr.Zero;
                        _initialForegroundRoot = IntPtr.Zero;
                    }
                }

                // Envoi des événements
                if (deltaYToSend != 0)
                {
                    DispatchWheelInput(deltaYToSend, false, dispatchHwnd, dispatchPt);
                }

                if (deltaXToSend != 0)
                {
                    DispatchWheelInput(deltaXToSend, true, dispatchHwnd, dispatchPt);
                }
            }
        }

        private static void DispatchWheelInput(int delta, bool isHorizontal, IntPtr hwnd, Win32.POINT pt)
        {
            if (Win32.IsCtrlPressed() || Win32.IsAltPressed())
            {
                Stop();
                return;
            }

            if (!isHorizontal && Win32.IsShiftPressed())
            {
                return;
            }

            if (hwnd != IntPtr.Zero)
            {
                if (!Win32.IsWindow(hwnd))
                {
                    Stop();
                    return;
                }

                uint msg = isHorizontal ? (uint)Win32.WM_MOUSEHWHEEL : (uint)Win32.WM_MOUSEWHEEL;
                IntPtr wParam = Win32.MakeWParam((short)delta, 0);
                IntPtr lParam = Win32.MakeLParam(pt.x, pt.y);

                if (Win32.PostMessage(hwnd, msg, wParam, lParam))
                {
                    return;
                }
            }

            // Fallback SendInput
            Win32.INPUT[] inputs = new Win32.INPUT[1];
            inputs[0].type = Win32.INPUT_MOUSE;
            inputs[0].mi.dx = 0;
            inputs[0].mi.dy = 0;
            inputs[0].mi.mouseData = unchecked((uint)delta);
            inputs[0].mi.dwFlags = isHorizontal ? Win32.MOUSEEVENTF_HWHEEL : Win32.MOUSEEVENTF_WHEEL;
            inputs[0].mi.time = 0;
            inputs[0].mi.dwExtraInfo = Win32.SCROLL_IT_SIGNATURE;

            Win32.SendInput(1, inputs, MarshalSize);
        }

        private static void ResetPhysicsState()
        {
            _velocityY = 0.0;
            _subPixelY = 0.0;
            _currentAccelY = 1.0;
            _lastWheelDirectionY = 0.0;

            _velocityX = 0.0;
            _subPixelX = 0.0;
            _currentAccelX = 1.0;
            _lastWheelDirectionX = 0.0;

            _latchedHwnd = IntPtr.Zero;
            _targetRootHwnd = IntPtr.Zero;
            _initialForegroundRoot = IntPtr.Zero;
            _isShiftHorizontal = false;
        }

        private static readonly int MarshalSize = Marshal.SizeOf(typeof(Win32.INPUT));
    }
}
