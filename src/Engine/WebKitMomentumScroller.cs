using System;
using System.Collections.Generic;

namespace ScrollIt.Engine
{
    /// <summary>
    /// Moteur de défilement avec inertie continue (Momentum), résistance élastique (Rubber-banding)
    /// et oscillateur harmonique amorti (Bounce-back) selon les spécifications Apple / WebKit.
    /// </summary>
    public class WebKitMomentumScroller
    {
        // --- Constantes physiques WebKit / Apple ---
        public const float Friction = 0.998f;          // Friction continue par milliseconde (0.998^dt)
        public const float CutoffVelocity = 0.001f;    // Seuil d'arrêt (~1 px/s au lieu de 10 px/s)
        public const float RubberBandC = 0.55f;        // Constante de résistance Apple
        public const float SpringMass = 1.0f;          // Masse m = 1.0
        public const float SpringK = 170.0f;           // Raideur k = 170
        public const float SpringB = 28.0f;            // Légèrement sur-amorti (> 2 * sqrt(170) ≈ 26.07) pour bloquer l'oscillation inverse

        // --- Dimensions & Limites ---
        public float ViewportDimension { get; set; }
        public float ContentDimension { get; set; }

        public float MaxScroll
        {
            get { return (float)Math.Max(0.0, ContentDimension - ViewportDimension); }
        }

        // --- État du défilement ---
        public float Position { get; private set; }
        public float RawPosition { get; private set; }
        public float Velocity { get; private set; } // en px/ms

        public bool IsInteracting { get; private set; }
        public bool IsAnimating { get; private set; }

        // --- Suivi tactile pour calcul de vélocité ---
        private struct TouchSample
        {
            public float Pos;
            public long Time;
            public TouchSample(float pos, long time) { Pos = pos; Time = time; }
        }

        private readonly List<TouchSample> _touchHistory = new List<TouchSample>(8);
        private float _startPointerY;
        private float _startRawPos;

        // Événement déclenché à chaque changement de position
        public event Action<float> OnPositionChanged;

        public WebKitMomentumScroller(float viewportDim = 600f, float contentDim = 2000f)
        {
            ViewportDimension = viewportDim;
            ContentDimension = contentDim;
        }

        /// <summary>
        /// Applique la compression hyperbolique Apple en butée
        /// </summary>
        public float ApplyRubberBand(float rawPos)
        {
            float max = MaxScroll;
            float dim = ViewportDimension;
            float c = RubberBandC;

            if (rawPos < 0f)
            {
                float overshoot = -rawPos;
                float compressed = (overshoot * dim * c) / (dim + overshoot * c);
                return -compressed;
            }
            if (rawPos > max)
            {
                float overshoot = rawPos - max;
                float compressed = (overshoot * dim * c) / (dim + overshoot * c);
                return max + compressed;
            }

            return rawPos;
        }

        /// <summary>
        /// Met à jour la physique à chaque frame
        /// </summary>
        /// <param name="deltaTimeMs">Temps écoulé depuis la dernière frame en millisecondes</param>
        public void Update(float deltaTimeMs)
        {
            if (IsInteracting || !IsAnimating) return;

            // Plafonner dt pour éviter les sauts en cas de lag
            float dt = (float)Math.Min((double)deltaTimeMs, 64.0);
            if (dt <= 0f) return;

            float max = MaxScroll;
            bool isOutOfBounds = (Position < 0f || Position > max);

            if (isOutOfBounds)
            {
                float remainingDtSec = dt / 1000.0f;
                float subStep = 0.004f;
                float targetPos = (Position < 0f) ? 0f : max;
                float vSec = Velocity * 1000.0f;

                while (remainingDtSec > 0f)
                {
                    float currentStep = Math.Min(remainingDtSec, subStep);
                    float displacement = Position - targetPos;

                    float force = -SpringK * displacement - SpringB * vSec;
                    float accel = force / SpringMass;

                    vSec += accel * currentStep;
                    float nextPos = Position + vSec * currentStep;

                    // Si le ressort traverse la frontière vers l'intérieur, on le bloque net à la butée
                    if ((Position < targetPos && nextPos >= targetPos) || 
                        (Position > targetPos && nextPos <= targetPos))
                    {
                        Position = targetPos;
                        RawPosition = targetPos;
                        Velocity = 0f;
                        IsAnimating = false;
                        if (OnPositionChanged != null) OnPositionChanged(Position);
                        return;
                    }

                    Position = nextPos;
                    remainingDtSec -= currentStep;
                }

                Velocity = vSec / 1000.0f;

                if (Math.Abs(Position - targetPos) < 0.05f && Math.Abs(Velocity) < CutoffVelocity)
                {
                    Position = targetPos;
                    RawPosition = targetPos;
                    Velocity = 0f;
                    IsAnimating = false;
                }
            }
            else
            {
                // --- 2. Inertie continue (Momentum) ---
                // v(t + dt) = v(t) * 0.998^dt
                Velocity *= (float)Math.Pow(Friction, dt);
                Position += Velocity * dt;
                RawPosition = Position;

                if (Math.Abs(Velocity) < CutoffVelocity)
                {
                    Velocity = 0f;
                    IsAnimating = false;

                    // Arrondi strict au pixel entier le plus proche à l'arrêt (Pixel Snapping)
                    Position = (float)Math.Round(Position);
                    RawPosition = Position;

                    if (OnPositionChanged != null)
                    {
                        OnPositionChanged(Position);
                    }
                    return;
                }
            }

            if (OnPositionChanged != null)
            {
                OnPositionChanged(Position);
            }
        }

        // ==========================================
        // GESTION DES GESTES & ENTRÉES
        // ==========================================

        public void OnPointerDown(float y, long nowMs)
        {
            IsInteracting = true;
            IsAnimating = false;
            Velocity = 0f;

            _startPointerY = y;
            _startRawPos = RawPosition;

            _touchHistory.Clear();
            _touchHistory.Add(new TouchSample(y, nowMs));
        }

        public void OnPointerMove(float y, long nowMs)
        {
            if (!IsInteracting) return;

            float deltaY = _startPointerY - y;
            RawPosition = _startRawPos + deltaY;
            Position = ApplyRubberBand(RawPosition);

            _touchHistory.Add(new TouchSample(y, nowMs));
            while (_touchHistory.Count > 0 && (nowMs - _touchHistory[0].Time > 100))
            {
                _touchHistory.RemoveAt(0);
            }

            if (OnPositionChanged != null)
            {
                OnPositionChanged(Position);
            }
        }

        public void OnPointerUp(long nowMs)
        {
            if (!IsInteracting) return;

            IsInteracting = false;
            RawPosition = Position;

            // Calcul de la vitesse moyenne sur les ~100 derniers ms
            if (_touchHistory.Count >= 2)
            {
                TouchSample first = _touchHistory[0];
                TouchSample last = _touchHistory[_touchHistory.Count - 1];
                float dt = last.Time - first.Time;
                float dy = first.Pos - last.Pos;

                Velocity = (dt > 10f) ? (dy / dt) : 0f;
            }
            else
            {
                Velocity = 0f;
            }

            IsAnimating = true;
        }

        public void OnWheel(float deltaY, bool isTrackpad = false)
        {
            if (isTrackpad)
            {
                RawPosition += deltaY;
                Position = ApplyRubberBand(RawPosition);
                Velocity = 0f;

                if (OnPositionChanged != null) OnPositionChanged(Position);

                if (Position < 0f || Position > MaxScroll)
                {
                    IsAnimating = true;
                }
            }
            else
            {
                Velocity += deltaY * 0.035f;
                IsAnimating = true;
            }
        }

        public void ScrollTo(float targetPosition)
        {
            float max = MaxScroll;
            Position = (targetPosition < 0f) ? 0f : (targetPosition > max ? max : targetPosition);
            RawPosition = Position;
            Velocity = 0f;
            IsAnimating = false;

            if (OnPositionChanged != null)
            {
                OnPositionChanged(Position);
            }
        }

        // ==========================================================
        // SCROLL LATCHING & WPF CONFINEMENT (overscroll-behavior: contain)
        // ==========================================================

        private static WebKitMomentumScroller _activeScroller = null;
        private static DateTime _lastWheelTime = DateTime.MinValue;
        public const int ScrollLatchTimeoutMs = 250;

        /// <summary>
        /// Achemine les événements globaux de molette avec verrouillage de cible (Scroll Latching).
        /// Si un scroll est déjà en cours depuis moins de 250 ms, la cible active est conservée.
        /// </summary>
        public static void HandleGlobalWheel(WebKitMomentumScroller target, float deltaY, bool isTrackpad = false)
        {
            if (target == null) return;

            DateTime now = DateTime.UtcNow;

            if (_activeScroller != null && (now - _lastWheelTime).TotalMilliseconds < ScrollLatchTimeoutMs)
            {
                _activeScroller.OnWheel(deltaY, isTrackpad);
            }
            else
            {
                _activeScroller = target;
                _activeScroller.OnWheel(deltaY, isTrackpad);
            }

            _lastWheelTime = now;
        }

        /// <summary>
        /// Gestionnaire d'événement pour WPF (PreviewMouseWheel).
        /// Bloque la remontée du scroll vers le parent (équivalent overscroll-behavior: contain).
        /// </summary>
        public void HandlePreviewMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e == null) return;

            // En WPF, e.Delta > 0 = vers le haut (scroller vers le bas en position)
            OnWheel(-e.Delta, false);

            // Bloque la propagation vers le ScrollViewer parent
            e.Handled = true;
        }
    }
}
