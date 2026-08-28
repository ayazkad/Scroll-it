using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Shell;
using Microsoft.Win32;
using ScrollIt.Engine;

namespace ScrollIt.UI
{
    public class MainWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        private static readonly Dictionary<string, ImageSource> _iconCache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        private static ImageSource _defaultAppIcon = null;

        // Sliders & Text Blocks
        private Slider _stepSlider;
        private TextBlock _stepValText;
        private Slider _timeSlider;
        private TextBlock _timeValText;
        private Slider _accelSlider;
        private TextBlock _accelValText;
        private Slider _tailSlider;
        private TextBlock _tailValText;

        // Toggle Switch & Status
        private Border _toggleTrack;
        private SolidColorBrush _toggleTrackBrush;
        private Border _toggleThumb;
        private TranslateTransform _toggleThumbTransform;
        private TextBlock _statusText;
        private Button _toggleEnableBtn;

        // Presets
        private StackPanel _presetsContainer;
        private TextBlock _presetDescText;
        private Dictionary<string, Button> _presetButtons = new Dictionary<string, Button>();

        // Navigation & Tab Sliding
        private Grid _tabContentContainer;
        private FrameworkElement _tabPhysicsView;
        private FrameworkElement _tabAppsView;
        private FrameworkElement _tabOptionsView;
        private FrameworkElement _currentView;
        private int _currentTabIndex = 0;
        private Button _btnTabPhysics;
        private Button _btnTabApps;
        private Button _btnTabOptions;
        private StackPanel _tabStack;
        private Border _tabIndicator;
        private TranslateTransform _tabIndicatorTransform;
        private double _currentIndicatorX = 0;
        private double _currentIndicatorW = 0;

        // Apps tab
        private StackPanel _blacklistedListPanel;
        private TextBox _newAppTextBox;
        private ComboBox _runningAppsCombo;

        // Options tab
        private CheckBox _chkAutoStart;
        private CheckBox _chkCtrlZoom;
        private CheckBox _chkMinimizeToTray;

        // Container & Window Controls
        private Border _mainContainer;
        private Border _titleBarBorder;
        private ContentControl _maxBtnContent;

        private bool _isUpdatingUI = false;

        public MainWindow()
        {
            InitializeWindow();
            BuildUI();
            LoadSettingsToUI();

            SettingsManager.SettingsChanged += OnSettingsUpdated;
        }

        private void InitializeWindow()
        {
            Title = "Scroll-it";
            Width = 860;
            Height = 690;
            MinWidth = 840;
            MinHeight = 685;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.CanResize;

            // Optimisation du rendu visuel et netteté du texte (ClearType + Snapping)
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            TextOptions.SetTextHintingMode(this, TextHintingMode.Fixed);
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"src\scroll-it.ico");
                if (!System.IO.File.Exists(iconPath)) iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scroll-it.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
                }
            }
            catch { }

            SourceInitialized += (s, e) =>
            {
                IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                System.Windows.Interop.HwndSource.FromHwnd(handle).AddHook(WindowProc);
                Win32.EnableWindows11RoundedCorners(handle);
            };

            StateChanged += (s, e) =>
            {
                bool isMax = (WindowState == WindowState.Maximized);
                try
                {
                    IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    if (handle != IntPtr.Zero)
                    {
                        Win32.SetWindowCornerPreference(handle, isMax);
                    }
                }
                catch { }

                if (_maxBtnContent != null)
                {
                    _maxBtnContent.Content = isMax ? CreateRestoreVectorIcon() : CreateMaximizeVectorIcon();
                }
                if (_mainContainer != null)
                {
                    _mainContainer.BorderThickness = isMax ? new Thickness(0) : new Thickness(1);
                    _mainContainer.CornerRadius = isMax ? new CornerRadius(0) : new CornerRadius(10);
                }
                if (_titleBarBorder != null)
                {
                    _titleBarBorder.CornerRadius = isMax ? new CornerRadius(0) : new CornerRadius(9, 9, 0, 0);
                }
            };

            // Handle close to minimize to tray
            Closing += (s, e) =>
            {
                if (SettingsManager.Current.MinimizeToTrayOnClose)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0024) // WM_GETMINMAXINFO
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            else if (msg == 0x0084 && WindowState != WindowState.Maximized) // WM_NCHITTEST (Snappy edge & corner resizing)
            {
                int x = unchecked((short)(long)lParam);
                int y = unchecked((short)((long)lParam >> 16));
                Point pt = PointFromScreen(new Point(x, y));

                int resizeBorder = 10;
                bool isLeft = pt.X <= resizeBorder;
                bool isRight = pt.X >= ActualWidth - resizeBorder;
                bool isTop = pt.Y <= resizeBorder;
                bool isBottom = pt.Y >= ActualHeight - resizeBorder;

                if (isTop && isLeft) { handled = true; return (IntPtr)13; /* HTTOPLEFT */ }
                if (isTop && isRight) { handled = true; return (IntPtr)14; /* HTTOPRIGHT */ }
                if (isBottom && isLeft) { handled = true; return (IntPtr)16; /* HTBOTTOMLEFT */ }
                if (isBottom && isRight) { handled = true; return (IntPtr)17; /* HTBOTTOMRIGHT */ }
                if (isLeft) { handled = true; return (IntPtr)10; /* HTLEFT */ }
                if (isRight) { handled = true; return (IntPtr)11; /* HTRIGHT */ }
                if (isTop) { handled = true; return (IntPtr)12; /* HTTOP */ }
                if (isBottom) { handled = true; return (IntPtr)15; /* HTBOTTOM */ }
            }
            else if (msg == Win32.WM_SHOW_SCROLL_IT && Win32.WM_SHOW_SCROLL_IT != 0)
            {
                Show();
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
                Activate();
                Focus();
                Topmost = true;
                Topmost = false;
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            Win32.MINMAXINFO mmi = (Win32.MINMAXINFO)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(Win32.MINMAXINFO));
            
            // Hard minimum sizing limit at OS level so content never breaks or truncates
            mmi.ptMinTrackSize.x = (int)MinWidth;
            mmi.ptMinTrackSize.y = (int)MinHeight;

            IntPtr monitor = Win32.MonitorFromWindow(hwnd, 2 /* MONITOR_DEFAULTTONEAREST */);
            if (monitor != IntPtr.Zero)
            {
                Win32.MONITORINFO monitorInfo = new Win32.MONITORINFO();
                monitorInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win32.MONITORINFO));
                Win32.GetMonitorInfo(monitor, ref monitorInfo);
                Win32.RECT rcWorkArea = monitorInfo.rcWork;
                Win32.RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = rcWorkArea.left - rcMonitorArea.left;
                mmi.ptMaxPosition.y = rcWorkArea.top - rcMonitorArea.top;
                mmi.ptMaxSize.x = rcWorkArea.right - rcWorkArea.left;
                mmi.ptMaxSize.y = rcWorkArea.bottom - rcWorkArea.top;
            }
            System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, true);
        }

        private void BuildUI()
        {
            // Outer container (clean, sleek, hardware-accelerated dark frame)
            _mainContainer = new Border
            {
                Background = Styles.BgBrush,
                BorderBrush = Styles.CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10)
            };

            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) }); // TitleBar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) }); // Sub Header / Tabs
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content

            // 1. TitleBar & Header
            rootGrid.Children.Add(BuildTitleBar());
            Grid.SetRow(rootGrid.Children[0], 0);

            // 2. Tab Navigation Bar
            rootGrid.Children.Add(BuildTabBar());
            Grid.SetRow(rootGrid.Children[1], 1);

            // 3. Main Views
            _tabPhysicsView = BuildPhysicsView();
            _tabAppsView = BuildAppsView();
            _tabOptionsView = BuildOptionsView();

            _tabAppsView.Visibility = Visibility.Collapsed;
            _tabOptionsView.Visibility = Visibility.Collapsed;
            _tabPhysicsView.Visibility = Visibility.Visible;
            _tabPhysicsView.Opacity = 1.0;
            _tabPhysicsView.RenderTransform = new TranslateTransform();

            _tabContentContainer = new Grid
            {
                ClipToBounds = true,
                Margin = new Thickness(24, 10, 24, 20)
            };

            _tabContentContainer.Children.Add(_tabPhysicsView);
            _tabContentContainer.Children.Add(_tabAppsView);
            _tabContentContainer.Children.Add(_tabOptionsView);
            _currentView = _tabPhysicsView;
            _currentTabIndex = 0;

            rootGrid.Children.Add(_tabContentContainer);
            Grid.SetRow(_tabContentContainer, 2);

            _mainContainer.Child = rootGrid;
            Content = _mainContainer;
        }

        private UIElement BuildTitleBar()
        {
            _titleBarBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 21, 28)),
                CornerRadius = new CornerRadius(9, 9, 0, 0),
                Margin = new Thickness(0)
            };

            Grid bar = new Grid();
            _titleBarBorder.Child = bar;

            _titleBarBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
                }
                else if (e.ButtonState == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

            StackPanel leftPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0)
            };

            // Real Glowing Project Logo
            Border iconBadge = new Border
            {
                Margin = new Thickness(0, 0, 12, 0),
                Effect = new DropShadowEffect
                {
                    Color = Styles.AccentPrimary,
                    BlurRadius = 16,
                    ShadowDepth = 0,
                    Opacity = 0.65
                }
            };
            iconBadge.Child = Styles.CreateProjectLogo(36);
            leftPanel.Children.Add(iconBadge);

            TextBlock title = new TextBlock
            {
                Text = "Scroll-it",
                Foreground = Styles.TextWhiteBrush,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            leftPanel.Children.Add(title);

            bar.Children.Add(leftPanel);

            // Right side: Window Controls
            StackPanel rightPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };

            // Minimize Button (Clean Vector Line)
            Button minBtn = CreateVectorWinButton(CreateMinimizeVectorIcon(), () => { WindowState = WindowState.Minimized; });

            // Maximize / Fullscreen Button (Clean Vector Box / Double-Box)
            _maxBtnContent = new ContentControl { Content = CreateMaximizeVectorIcon() };
            Button maxBtn = CreateVectorWinButton(_maxBtnContent, () =>
            {
                WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
            });

            // Close Button (Clean Vector Cross)
            Button closeBtn = CreateVectorWinButton(CreateCloseVectorIcon(), () => { Close(); }, true);

            rightPanel.Children.Add(minBtn);
            rightPanel.Children.Add(maxBtn);
            rightPanel.Children.Add(closeBtn);

            bar.Children.Add(rightPanel);
            return _titleBarBorder;
        }

        private UIElement CreateMinimizeVectorIcon()
        {
            return new Rectangle
            {
                Width = 11,
                Height = 1.3,
                Fill = Styles.TextMutedBrush,
                SnapsToDevicePixels = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private UIElement CreateMaximizeVectorIcon()
        {
            return new Border
            {
                Width = 10,
                Height = 10,
                BorderBrush = Styles.TextMutedBrush,
                BorderThickness = new Thickness(1.3),
                SnapsToDevicePixels = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private UIElement CreateRestoreVectorIcon()
        {
            Canvas canvas = new Canvas { Width = 11, Height = 11 };
            Border back = new Border
            {
                Width = 8,
                Height = 8,
                BorderBrush = Styles.TextMutedBrush,
                BorderThickness = new Thickness(1.3),
                SnapsToDevicePixels = true
            };
            Canvas.SetLeft(back, 3);
            Canvas.SetTop(back, 0);

            Border front = new Border
            {
                Width = 8,
                Height = 8,
                BorderBrush = Styles.TextMutedBrush,
                BorderThickness = new Thickness(1.3),
                Background = new SolidColorBrush(Color.FromArgb(200, 16, 21, 28)),
                SnapsToDevicePixels = true
            };
            Canvas.SetLeft(front, 0);
            Canvas.SetTop(front, 3);

            canvas.Children.Add(back);
            canvas.Children.Add(front);
            return canvas;
        }

        private UIElement CreateCloseVectorIcon()
        {
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M 0 0 L 10 10 M 0 10 L 10 0"),
                Stroke = Styles.TextMutedBrush,
                StrokeThickness = 1.3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                SnapsToDevicePixels = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            return path;
        }

        private Button CreateVectorWinButton(UIElement icon, Action action, bool isDanger = false)
        {
            Button btn = new Button
            {
                Width = 44,
                Height = 32,
                Margin = new Thickness(1, 0, 1, 0),
                Cursor = Cursors.Hand,
                Focusable = false
            };

            ControlTemplate tpl = new ControlTemplate(typeof(Button));
            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(Border));
            factory.Name = "btnBorder";
            factory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(content);
            tpl.VisualTree = factory;

            btn.Template = tpl;
            btn.Content = icon;

            SolidColorBrush normalHoverBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
            SolidColorBrush dangerHoverBrush = new SolidColorBrush(Color.FromRgb(232, 17, 35)); // Sleek Windows Red
            SolidColorBrush dangerPressedBrush = new SolidColorBrush(Color.FromRgb(196, 43, 28));

            System.Windows.Shapes.Path closePath = icon as System.Windows.Shapes.Path;

            btn.MouseEnter += (s, e) =>
            {
                Border b = btn.Template.FindName("btnBorder", btn) as Border;
                if (b != null)
                {
                    b.Background = isDanger ? dangerHoverBrush : normalHoverBrush;
                }
                if (isDanger && closePath != null)
                {
                    closePath.Stroke = Brushes.White;
                }
            };

            btn.MouseLeave += (s, e) =>
            {
                Border b = btn.Template.FindName("btnBorder", btn) as Border;
                if (b != null)
                {
                    b.Background = Brushes.Transparent;
                }
                if (isDanger && closePath != null)
                {
                    closePath.Stroke = Styles.TextMutedBrush;
                }
            };

            btn.PreviewMouseDown += (s, e) =>
            {
                Border b = btn.Template.FindName("btnBorder", btn) as Border;
                if (b != null)
                {
                    b.Background = isDanger ? dangerPressedBrush : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                }
            };

            btn.PreviewMouseUp += (s, e) =>
            {
                Border b = btn.Template.FindName("btnBorder", btn) as Border;
                if (b != null)
                {
                    b.Background = isDanger ? dangerHoverBrush : normalHoverBrush;
                }
            };

            btn.Click += (s, e) => action();
            return btn;
        }

        private UIElement BuildTabBar()
        {
            Border tabBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 13, 17, 23)),
                BorderBrush = Styles.CardBorderBrush,
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(20, 0, 20, 0)
            };

            Grid tabContainer = new Grid();

            _tabStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            _btnTabPhysics = CreateNavTab("Physique & Presets", Styles.CreateProjectLogo(18), true, () => SwitchTab(0));
            _btnTabApps = CreateNavTab("Applications & Exclusions", CreateTabEmoji("🎮"), false, () => SwitchTab(1));
            _btnTabOptions = CreateNavTab("Options & Démarrage", CreateTabEmoji("⚙"), false, () => SwitchTab(2));

            _tabStack.Children.Add(_btnTabPhysics);
            _tabStack.Children.Add(_btnTabApps);
            _tabStack.Children.Add(_btnTabOptions);

            _tabIndicatorTransform = new TranslateTransform();
            _tabIndicator = new Border
            {
                Height = 3,
                CornerRadius = new CornerRadius(1.5),
                Background = Styles.AccentGradient,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                RenderTransform = _tabIndicatorTransform,
                Effect = new DropShadowEffect
                {
                    Color = Styles.AccentPrimary,
                    BlurRadius = 12,
                    ShadowDepth = 0,
                    Opacity = 0.85
                }
            };

            tabContainer.Children.Add(_tabStack);
            tabContainer.Children.Add(_tabIndicator);

            // Toggle active switch button (placed on the right of the Tab Bar)
            _toggleEnableBtn = new Button
            {
                Width = 106,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            FrameworkElementFactory toggleBorder = new FrameworkElementFactory(typeof(Border));
            toggleBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(120, 22, 27, 34)));
            toggleBorder.SetValue(Border.BorderBrushProperty, Styles.CardBorderBrush);
            toggleBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            toggleBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(16));
            toggleBorder.SetValue(Border.PaddingProperty, new Thickness(8, 4, 10, 4));

            FrameworkElementFactory toggleContent = new FrameworkElementFactory(typeof(ContentPresenter));
            toggleContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            toggleContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            toggleBorder.AppendChild(toggleContent);

            ControlTemplate toggleTpl = new ControlTemplate(typeof(Button));
            toggleTpl.VisualTree = toggleBorder;
            _toggleEnableBtn.Template = toggleTpl;

            _toggleEnableBtn.Click += (s, e) =>
            {
                SettingsManager.Current.Enabled = !SettingsManager.Current.Enabled;
                SettingsManager.Save();
                UpdateStatusUI(true);
                TrayManager.UpdateState();
            };

            Grid statusGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            bool isInitEnabled = SettingsManager.Current.Enabled;
            _toggleTrackBrush = new SolidColorBrush(isInitEnabled ? Styles.SuccessGreen : Color.FromRgb(48, 54, 61));
            _toggleTrack = new Border
            {
                Width = 34,
                Height = 18,
                CornerRadius = new CornerRadius(9),
                Background = _toggleTrackBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            _toggleThumbTransform = new TranslateTransform(isInitEnabled ? 16 : 0, 0);
            _toggleThumb = new Border
            {
                Width = 12,
                Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(3, 0, 0, 0),
                RenderTransform = _toggleThumbTransform,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 1,
                    BlurRadius = 4,
                    Opacity = 0.45
                }
            };
            _toggleTrack.Child = _toggleThumb;

            _statusText = new TextBlock
            {
                Text = isInitEnabled ? "Actif" : "Inactif",
                Foreground = isInitEnabled ? Styles.TextWhiteBrush : Styles.TextMutedBrush,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            Grid.SetColumn(_toggleTrack, 0);
            Grid.SetColumn(_statusText, 1);
            statusGrid.Children.Add(_toggleTrack);
            statusGrid.Children.Add(_statusText);
            _toggleEnableBtn.Content = statusGrid;

            tabContainer.Children.Add(_toggleEnableBtn);

            tabBorder.Child = tabContainer;

            Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateTabIndicator(0, false);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            };

            SizeChanged += (s, e) =>
            {
                UpdateTabIndicator(_currentTabIndex, false);
            };

            return tabBorder;
        }

        private Button CreateNavTab(string title, UIElement icon, bool active, Action onClick)
        {
            StackPanel contentStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (icon != null)
            {
                Border iconWrap = new Border
                {
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = icon
                };
                contentStack.Children.Add(iconWrap);
            }

            TextBlock text = new TextBlock
            {
                Text = title,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            contentStack.Children.Add(text);

            Button btn = new Button
            {
                Content = contentStack,
                Foreground = active ? Styles.AccentBrush : Styles.TextMutedBrush,
                FontWeight = active ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(0, 0, 24, 0),
                Cursor = Cursors.Hand
            };

            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            border.SetValue(Border.PaddingProperty, new Thickness(4, 10, 4, 10));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);

            ControlTemplate tpl = new ControlTemplate(typeof(Button));
            tpl.VisualTree = border;
            btn.Template = tpl;

            btn.MouseEnter += (s, e) =>
            {
                if (btn.FontWeight != FontWeights.Bold)
                {
                    btn.Foreground = Styles.TextWhiteBrush;
                }
            };
            btn.MouseLeave += (s, e) =>
            {
                if (btn.FontWeight != FontWeights.Bold)
                {
                    btn.Foreground = Styles.TextMutedBrush;
                }
            };

            btn.Click += (s, e) => onClick();
            return btn;
        }

        private UIElement CreateTabEmoji(string emoji)
        {
            return new TextBlock
            {
                Text = emoji,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void UpdateTabIndicator(int index, bool animate = true)
        {
            Button targetBtn = (index == 0) ? _btnTabPhysics : (index == 1 ? _btnTabApps : _btnTabOptions);
            if (targetBtn == null || _tabIndicator == null || _tabStack == null) return;

            try
            {
                if (targetBtn.ActualWidth <= 0)
                {
                    targetBtn.UpdateLayout();
                    _tabStack.UpdateLayout();
                }

                Point pt = targetBtn.TranslatePoint(new Point(0, 0), _tabStack);
                double targetX = pt.X;
                double targetW = targetBtn.ActualWidth;

                if (targetW <= 0) targetW = 140;

                double fromX = (_currentIndicatorW > 0) ? _currentIndicatorX : targetX;
                double fromW = (_currentIndicatorW > 0) ? _currentIndicatorW : targetW;

                if (!animate)
                {
                    _tabIndicatorTransform.BeginAnimation(TranslateTransform.XProperty, null);
                    _tabIndicator.BeginAnimation(FrameworkElement.WidthProperty, null);
                    _tabIndicatorTransform.X = targetX;
                    _tabIndicator.Width = targetW;
                    _currentIndicatorX = targetX;
                    _currentIndicatorW = targetW;
                }
                else
                {
                    QuarticEase ease = new QuarticEase { EasingMode = EasingMode.EaseOut };
                    TimeSpan duration = TimeSpan.FromMilliseconds(260);

                    DoubleAnimation slideAnim = new DoubleAnimation(fromX, targetX, duration) { EasingFunction = ease };
                    DoubleAnimation widthAnim = new DoubleAnimation(fromW, targetW, duration) { EasingFunction = ease };

                    _currentIndicatorX = targetX;
                    _currentIndicatorW = targetW;

                    slideAnim.Completed += (s, e) =>
                    {
                        _tabIndicatorTransform.BeginAnimation(TranslateTransform.XProperty, null);
                        _tabIndicatorTransform.X = targetX;
                    };
                    widthAnim.Completed += (s, e) =>
                    {
                        _tabIndicator.BeginAnimation(FrameworkElement.WidthProperty, null);
                        _tabIndicator.Width = targetW;
                    };

                    _tabIndicatorTransform.BeginAnimation(TranslateTransform.XProperty, slideAnim);
                    _tabIndicator.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);
                }
            }
            catch { }
        }

        private void SwitchTab(int index)
        {
            if (index == _currentTabIndex && _currentView != null) return;

            int oldIndex = _currentTabIndex;
            bool slideFromRight = (index > oldIndex);
            _currentTabIndex = index;

            _btnTabPhysics.Foreground = (index == 0) ? Styles.AccentBrush : Styles.TextMutedBrush;
            _btnTabPhysics.FontWeight = (index == 0) ? FontWeights.Bold : FontWeights.Normal;

            _btnTabApps.Foreground = (index == 1) ? Styles.AccentBrush : Styles.TextMutedBrush;
            _btnTabApps.FontWeight = (index == 1) ? FontWeights.Bold : FontWeights.Normal;

            _btnTabOptions.Foreground = (index == 2) ? Styles.AccentBrush : Styles.TextMutedBrush;
            _btnTabOptions.FontWeight = (index == 2) ? FontWeights.Bold : FontWeights.Normal;

            UpdateTabIndicator(index, true);

            if (index == 1)
            {
                RefreshAppsList();
            }

            FrameworkElement nextView = (index == 0) ? _tabPhysicsView : (index == 1 ? _tabAppsView : _tabOptionsView);
            AnimateTabContentTransition(_currentView, nextView, slideFromRight);
            _currentView = nextView;
        }

        private void AnimateTabContentTransition(FrameworkElement oldView, FrameworkElement newView, bool slideFromRight)
        {
            if (newView == null) return;

            FrameworkElement[] allViews = new FrameworkElement[] { _tabPhysicsView, _tabAppsView, _tabOptionsView };

            // 1. Immediately cancel all running animations on inactive views and hide them
            foreach (FrameworkElement v in allViews)
            {
                if (v != null && v != newView)
                {
                    v.BeginAnimation(UIElement.OpacityProperty, null);
                    TranslateTransform trans = v.RenderTransform as TranslateTransform;
                    if (trans != null)
                    {
                        trans.BeginAnimation(TranslateTransform.XProperty, null);
                        trans.X = 0;
                    }
                    v.Opacity = 0.0;
                    v.Visibility = Visibility.Collapsed;
                }
            }

            // 2. Setup incoming view
            if (!_tabContentContainer.Children.Contains(newView))
            {
                _tabContentContainer.Children.Add(newView);
            }

            TranslateTransform newTrans = newView.RenderTransform as TranslateTransform;
            if (newTrans == null)
            {
                newTrans = new TranslateTransform();
                newView.RenderTransform = newTrans;
            }

            // Clear any lingering animation on the incoming view
            newView.BeginAnimation(UIElement.OpacityProperty, null);
            newTrans.BeginAnimation(TranslateTransform.XProperty, null);

            double enterX = slideFromRight ? 35.0 : -35.0;
            newTrans.X = enterX;
            newView.Opacity = 0.0;
            newView.Visibility = Visibility.Visible;

            QuarticEase ease = new QuarticEase { EasingMode = EasingMode.EaseOut };
            TimeSpan duration = TimeSpan.FromMilliseconds(220);

            DoubleAnimation enterSlide = new DoubleAnimation(enterX, 0.0, duration) { EasingFunction = ease };
            DoubleAnimation enterFade = new DoubleAnimation(0.0, 1.0, duration) { EasingFunction = ease };

            enterSlide.Completed += (s, e) =>
            {
                newTrans.BeginAnimation(TranslateTransform.XProperty, null);
                newTrans.X = 0;
            };

            enterFade.Completed += (s, e) =>
            {
                newView.BeginAnimation(UIElement.OpacityProperty, null);
                newView.Opacity = 1.0;
            };

            newTrans.BeginAnimation(TranslateTransform.XProperty, enterSlide);
            newView.BeginAnimation(UIElement.OpacityProperty, enterFade);
        }

        private FrameworkElement BuildPhysicsView()
        {
            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0)
            };
            StackPanel stack = new StackPanel { Margin = new Thickness(4) };

            // Presets Bar
            TextBlock presetsTitle = new TextBlock
            {
                Text = "Profils de fluidité (1-Clic)",
                Foreground = Styles.TextWhiteBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(4, 0, 0, 8)
            };
            stack.Children.Add(presetsTitle);

            _presetsContainer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 6)
            };

            foreach (var pair in SettingsManager.Presets)
            {
                string pName = pair.Key;
                Button pBtn = Styles.CreatePillButton(pName, SettingsManager.Current.ActivePreset == pName);
                pBtn.Click += (s, e) =>
                {
                    SelectPreset(pName, true);
                };
                _presetButtons[pName] = pBtn;
                _presetsContainer.Children.Add(pBtn);
            }
            stack.Children.Add(_presetsContainer);

            _presetDescText = new TextBlock
            {
                Text = SettingsManager.Presets.ContainsKey(SettingsManager.Current.ActivePreset)
                    ? SettingsManager.Presets[SettingsManager.Current.ActivePreset].Description
                    : "Paramètres personnalisés ajustés manuellement.",
                Foreground = Styles.TextMutedBrush,
                FontSize = 12,
                Margin = new Thickness(6, 0, 0, 12),
                TextWrapping = TextWrapping.Wrap
            };
            stack.Children.Add(_presetDescText);

            // Sliders Cards
            Border slidersCard = Styles.CreateGlassCard(16, 12);
            StackPanel slidersStack = new StackPanel();

            // 1. Step Size
            slidersStack.Children.Add(CreateSliderRow(
                "Taille du pas (Step Size)",
                "Distance parcourue pour un cran de molette (défaut Windows : 120 px)",
                20, 300, 1,
                out _stepSlider, out _stepValText,
                (val) =>
                {
                    if (_isUpdatingUI) return;
                    SettingsManager.Current.StepSize = val;
                    SettingsManager.Current.ActivePreset = "Personnalisé";
                    SettingsManager.Save();
                },
                "px"
            ));

            // 2. Animation Time
            slidersStack.Children.Add(CreateSliderRow(
                "Durée d'animation (Animation Time)",
                "Temps d'amortissement de la transition fluide",
                100, 900, 10,
                out _timeSlider, out _timeValText,
                (val) =>
                {
                    if (_isUpdatingUI) return;
                    SettingsManager.Current.AnimationTime = val;
                    SettingsManager.Current.ActivePreset = "Personnalisé";
                    SettingsManager.Save();
                },
                "ms"
            ));

            // 3. Acceleration Multiplier
            slidersStack.Children.Add(CreateSliderRow(
                "Multiplicateur d'accélération (Inertia)",
                "Vitesse exponentielle lors de coups de molette rapides consécutifs",
                1.0, 4.5, 0.1,
                out _accelSlider, out _accelValText,
                (val) =>
                {
                    if (_isUpdatingUI) return;
                    SettingsManager.Current.AccelerationMultiplier = Math.Round(val, 1);
                    SettingsManager.Current.ActivePreset = "Personnalisé";
                    SettingsManager.Save();
                },
                "x"
            ));

            // 4. Deceleration Tail (Friction)
            slidersStack.Children.Add(CreateSliderRow(
                "Queue de décélération (Tail / Friction)",
                "Douceur de la glisse finale avant l'arrêt complet",
                0.20, 0.95, 0.01,
                out _tailSlider, out _tailValText,
                (val) =>
                {
                    if (_isUpdatingUI) return;
                    SettingsManager.Current.FrictionTail = Math.Round(val, 2);
                    SettingsManager.Current.ActivePreset = "Personnalisé";
                    SettingsManager.Save();
                },
                ""
            ));

            slidersCard.Child = slidersStack;
            stack.Children.Add(slidersCard);

            scroll.Content = stack;
            return scroll;
        }

        private UIElement CreateSliderRow(
            string title, string description,
            double min, double max, double tick,
            out Slider outSlider, out TextBlock outValText,
            Action<double> onChange, string unit = "")
        {
            StackPanel panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

            Grid titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel labelStack = new StackPanel();
            TextBlock tBlock = new TextBlock
            {
                Text = title,
                Foreground = Styles.TextWhiteBrush,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13
            };
            TextBlock dBlock = new TextBlock
            {
                Text = description,
                Foreground = Styles.TextMutedBrush,
                FontSize = 11
            };
            labelStack.Children.Add(tBlock);
            labelStack.Children.Add(dBlock);
            titleGrid.Children.Add(labelStack);
            Grid.SetColumn(labelStack, 0);

            Border valBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 0, 210, 255)),
                BorderBrush = Styles.AccentBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock valText = new TextBlock
            {
                Text = "100" + unit,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };
            valBadge.Child = valText;
            titleGrid.Children.Add(valBadge);
            Grid.SetColumn(valBadge, 1);

            panel.Children.Add(titleGrid);

            // Slider with [-] and [+] Stepper Buttons for precision adjustment
            Grid sliderGrid = new Grid { Margin = new Thickness(0, 5, 0, 0) };
            sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Slider slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                TickFrequency = tick,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            slider.ValueChanged += (s, e) =>
            {
                valText.Text = (unit == "x" ? slider.Value.ToString("0.0") : (unit == "ms" || unit == "px" ? ((int)slider.Value).ToString() : slider.Value.ToString("0.00"))) + (string.IsNullOrEmpty(unit) ? "" : " " + unit);
                onChange(slider.Value);
            };

            Button btnMinus = CreateStepperButton("−", () =>
            {
                double newVal = Math.Round(slider.Value - tick, 2);
                if (newVal < min) newVal = min;
                slider.Value = newVal;
            });

            Button btnPlus = CreateStepperButton("+", () =>
            {
                double newVal = Math.Round(slider.Value + tick, 2);
                if (newVal > max) newVal = max;
                slider.Value = newVal;
            });

            sliderGrid.Children.Add(btnMinus);
            Grid.SetColumn(btnMinus, 0);

            sliderGrid.Children.Add(slider);
            Grid.SetColumn(slider, 1);

            sliderGrid.Children.Add(btnPlus);
            Grid.SetColumn(btnPlus, 2);

            panel.Children.Add(sliderGrid);

            outSlider = slider;
            outValText = valText;
            return panel;
        }

        private Button CreateStepperButton(string symbol, Action onClick)
        {
            Button btn = new Button
            {
                Width = 28,
                Height = 28,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };

            ControlTemplate tpl = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "Border";
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(200, 22, 27, 34)));
            border.SetValue(Border.BorderBrushProperty, Styles.CardBorderBrush);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            FrameworkElementFactory text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetValue(TextBlock.TextProperty, symbol);
            text.SetValue(TextBlock.ForegroundProperty, Styles.TextWhiteBrush);
            text.SetValue(TextBlock.FontSizeProperty, 14.0);
            text.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            text.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            text.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(text);

            Trigger mouseOverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(255, 35, 43, 56)), "Border"));
            mouseOverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, Styles.AccentBrush, "Border"));
            tpl.Triggers.Add(mouseOverTrigger);

            Trigger pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(255, 20, 25, 32)), "Border"));
            tpl.Triggers.Add(pressedTrigger);

            tpl.VisualTree = border;
            btn.Template = tpl;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private FrameworkElement BuildAppsView()
        {
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel stack = new StackPanel { Margin = new Thickness(4) };

            Border infoCard = Styles.CreateGlassCard(16, 12);
            StackPanel infoStack = new StackPanel();
            TextBlock infoTitle = new TextBlock
            {
                Text = "Exceptions & Liste Noire d'Applications",
                Foreground = Styles.TextWhiteBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 15
            };
            TextBlock infoDesc = new TextBlock
            {
                Text = "Scroll-It se désactive automatiquement sur les exécutables ci-dessous (idéal pour les jeux compétitifs, logiciels de modélisation 3D / CAD ou applications sensibles).",
                Foreground = Styles.TextMutedBrush,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            infoStack.Children.Add(infoTitle);
            infoStack.Children.Add(infoDesc);
            infoCard.Child = infoStack;
            stack.Children.Add(infoCard);

            // Add App section
            Border addCard = Styles.CreateGlassCard(16, 12);
            addCard.Margin = new Thickness(0, 16, 0, 16);
            StackPanel addStack = new StackPanel();

            TextBlock addTitle = new TextBlock
            {
                Text = "Ajouter une application",
                Foreground = Styles.TextWhiteBrush,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 8)
            };
            addStack.Children.Add(addTitle);

            Grid addGrid = new Grid();
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _newAppTextBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 10, 13, 18)),
                Foreground = Styles.TextWhiteBrush,
                BorderBrush = Styles.CardBorderBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            addGrid.Children.Add(_newAppTextBox);
            Grid.SetColumn(_newAppTextBox, 0);

            Button addBtn = Styles.CreatePillButton("+ Ajouter", true);
            addBtn.Click += (s, e) =>
            {
                string app = _newAppTextBox.Text.Trim().ToLowerInvariant().Replace(".exe", "");
                if (!string.IsNullOrEmpty(app) && !SettingsManager.Current.BlacklistedApps.Contains(app))
                {
                    SettingsManager.Current.BlacklistedApps.Add(app);
                    SettingsManager.Save();
                    _newAppTextBox.Text = "";
                    RefreshAppsList();
                }
            };
            addGrid.Children.Add(addBtn);
            Grid.SetColumn(addBtn, 1);

            addStack.Children.Add(addGrid);

            // Quick add from running processes
            Grid quickGrid = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            quickGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            quickGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _runningAppsCombo = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 16, 21, 28)),
                Foreground = Brushes.Black,
                BorderBrush = Styles.CardBorderBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 10, 0),
                MaxDropDownHeight = 220
            };
            ScrollViewer.SetCanContentScroll(_runningAppsCombo, false);
            quickGrid.Children.Add(_runningAppsCombo);
            Grid.SetColumn(_runningAppsCombo, 0);

            Button quickAddBtn = Styles.CreatePillButton("Ajouter le processus", false);
            quickAddBtn.Click += (s, e) =>
            {
                if (_runningAppsCombo.SelectedItem != null)
                {
                    string sel = null;
                    ComboBoxItem cbi = _runningAppsCombo.SelectedItem as ComboBoxItem;
                    if (cbi != null && cbi.Tag != null)
                    {
                        sel = cbi.Tag.ToString().ToLowerInvariant();
                    }
                    else
                    {
                        sel = _runningAppsCombo.SelectedItem.ToString().ToLowerInvariant();
                    }

                    if (!string.IsNullOrEmpty(sel) && !SettingsManager.Current.BlacklistedApps.Contains(sel))
                    {
                        SettingsManager.Current.BlacklistedApps.Add(sel);
                        SettingsManager.Save();
                        RefreshAppsList();
                    }
                }
            };
            quickGrid.Children.Add(quickAddBtn);
            Grid.SetColumn(quickAddBtn, 1);

            addStack.Children.Add(quickGrid);
            addCard.Child = addStack;
            stack.Children.Add(addCard);

            // Blacklisted Apps List
            Border listCard = Styles.CreateGlassCard(16, 12);
            _blacklistedListPanel = new StackPanel();
            listCard.Child = _blacklistedListPanel;
            stack.Children.Add(listCard);

            scroll.Content = stack;
            return scroll;
        }

        private void RefreshAppsList()
        {
            if (_blacklistedListPanel == null) return;
            _blacklistedListPanel.Children.Clear();

            TextBlock listHeader = new TextBlock
            {
                Text = string.Format("Applications désactivées ({0})", SettingsManager.Current.BlacklistedApps.Count),
                Foreground = Styles.TextWhiteBrush,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 12)
            };
            _blacklistedListPanel.Children.Add(listHeader);

            if (SettingsManager.Current.BlacklistedApps.Count == 0)
            {
                Border emptyBox = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(60, 13, 17, 23)),
                    BorderBrush = Styles.CardBorderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 20, 16, 20),
                    Margin = new Thickness(0, 4, 0, 4)
                };
                StackPanel emptyStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                TextBlock emptyIcon = new TextBlock
                {
                    Text = "✨",
                    FontSize = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                TextBlock emptyTitle = new TextBlock
                {
                    Text = "Aucune application désactivée",
                    Foreground = Styles.TextWhiteBrush,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                TextBlock emptyDesc = new TextBlock
                {
                    Text = "Scroll-it est actif et fluide sur l'ensemble de vos logiciels et jeux.",
                    Foreground = Styles.TextMutedBrush,
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                emptyStack.Children.Add(emptyIcon);
                emptyStack.Children.Add(emptyTitle);
                emptyStack.Children.Add(emptyDesc);
                emptyBox.Child = emptyStack;
                _blacklistedListPanel.Children.Add(emptyBox);
            }
            else
            {
                foreach (string app in SettingsManager.Current.BlacklistedApps)
                {
                    string currentApp = app;
                    Border itemBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(100, 13, 17, 23)),
                        BorderBrush = Styles.CardBorderBrush,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(0, 0, 0, 6)
                    };

                    Grid itemGrid = new Grid();
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    StackPanel leftPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    // Application Icon
                    Image appIconImg = new Image
                    {
                        Source = GetAppIcon(currentApp),
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(0, 0, 10, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    RenderOptions.SetBitmapScalingMode(appIconImg, BitmapScalingMode.HighQuality);
                    leftPanel.Children.Add(appIconImg);

                    TextBlock name = new TextBlock
                    {
                        Text = currentApp + ".exe",
                        Foreground = Styles.TextWhiteBrush,
                        FontWeight = FontWeights.Medium,
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    leftPanel.Children.Add(name);

                    itemGrid.Children.Add(leftPanel);
                    Grid.SetColumn(leftPanel, 0);

                    Button delBtn = new Button
                    {
                        Content = "✕ Supprimer",
                        Foreground = new SolidColorBrush(Styles.DangerRed),
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Cursor = Cursors.Hand
                    };
                    FrameworkElementFactory delBorder = new FrameworkElementFactory(typeof(Border));
                    delBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
                    delBorder.SetValue(Border.PaddingProperty, new Thickness(6, 3, 6, 3));
                    FrameworkElementFactory delContent = new FrameworkElementFactory(typeof(ContentPresenter));
                    delContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    delContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                    delBorder.AppendChild(delContent);
                    ControlTemplate delTpl = new ControlTemplate(typeof(Button));
                    delTpl.VisualTree = delBorder;
                    delBtn.Template = delTpl;

                    delBtn.Click += (s, e) =>
                    {
                        SettingsManager.Current.BlacklistedApps.Remove(currentApp);
                        SettingsManager.Save();
                        RefreshAppsList();
                    };
                    itemGrid.Children.Add(delBtn);
                    Grid.SetColumn(delBtn, 1);

                    itemBorder.Child = itemGrid;
                    _blacklistedListPanel.Children.Add(itemBorder);
                }
            }

            // Populate running apps combo asynchronously
            PopulateRunningAppsComboAsync();
        }

        private void PopulateRunningAppsComboAsync()
        {
            if (_runningAppsCombo == null) return;

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    List<string> appList = new List<string>();

                    foreach (Process p in Process.GetProcesses())
                    {
                        try
                        {
                            string pName = p.ProcessName.ToLowerInvariant();
                            if (!string.IsNullOrEmpty(pName) && !seen.Contains(pName) && !SettingsManager.Current.BlacklistedApps.Contains(pName))
                            {
                                // Filter out headless background services without visible windows
                                if (p.MainWindowHandle != IntPtr.Zero || !string.IsNullOrEmpty(p.MainWindowTitle))
                                {
                                    seen.Add(pName);
                                    appList.Add(pName);
                                }
                            }
                        }
                        catch { }
                    }

                    appList.Sort(StringComparer.OrdinalIgnoreCase);

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_runningAppsCombo == null) return;
                        _runningAppsCombo.Items.Clear();

                        foreach (string pName in appList)
                        {
                            ComboBoxItem cbi = new ComboBoxItem
                            {
                                Tag = pName,
                                Padding = new Thickness(4, 3, 4, 3),
                                Foreground = Brushes.Black
                            };
                            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
                            Image cbiImg = new Image
                            {
                                Source = GetAppIcon(pName),
                                Width = 16,
                                Height = 16,
                                Margin = new Thickness(0, 0, 8, 0),
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            RenderOptions.SetBitmapScalingMode(cbiImg, BitmapScalingMode.HighQuality);
                            TextBlock cbiText = new TextBlock
                            {
                                Text = pName + ".exe",
                                VerticalAlignment = VerticalAlignment.Center,
                                FontSize = 12
                            };
                            sp.Children.Add(cbiImg);
                            sp.Children.Add(cbiText);
                            cbi.Content = sp;

                            _runningAppsCombo.Items.Add(cbi);
                        }

                        if (_runningAppsCombo.Items.Count > 0) _runningAppsCombo.SelectedIndex = 0;
                    }));
                }
                catch { }
            });
        }

        public static ImageSource GetAppIcon(string appName)
        {
            if (string.IsNullOrEmpty(appName)) return GetDefaultAppIcon();
            appName = appName.Replace(".exe", "").Trim();

            if (_iconCache.ContainsKey(appName))
            {
                return _iconCache[appName];
            }

            ImageSource result = null;

            // 1. Try from currently running processes
            try
            {
                Process[] procs = Process.GetProcessesByName(appName);
                foreach (Process p in procs)
                {
                    try
                    {
                        if (p.MainModule != null)
                        {
                            string exePath = p.MainModule.FileName;
                            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                            {
                                result = ExtractWpfIcon(exePath);
                                if (result != null) break;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // 2. Try App Paths in Windows Registry
            if (result == null)
            {
                try
                {
                    string[] regKeys = new string[]
                    {
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + appName + ".exe",
                        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\" + appName + ".exe"
                    };

                    foreach (string key in regKeys)
                    {
                        using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(key) ?? Registry.LocalMachine.OpenSubKey(key))
                        {
                            if (rk != null)
                            {
                                string path = rk.GetValue(null) as string;
                                if (!string.IsNullOrEmpty(path))
                                {
                                    path = path.Trim('\"');
                                    if (File.Exists(path))
                                    {
                                        result = ExtractWpfIcon(path);
                                        if (result != null) break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // 3. Fallback: Sleek vector app icon
            if (result == null)
            {
                result = GetDefaultAppIcon();
            }

            _iconCache[appName] = result;
            return result;
        }

        private static ImageSource ExtractWpfIcon(string filePath)
        {
            try
            {
                using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(filePath))
                {
                    if (icon != null)
                    {
                        using (System.Drawing.Bitmap bmp = icon.ToBitmap())
                        {
                            IntPtr hBitmap = bmp.GetHbitmap();
                            try
                            {
                                ImageSource wpfBmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    hBitmap,
                                    IntPtr.Zero,
                                    Int32Rect.Empty,
                                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()
                                );
                                wpfBmp.Freeze();
                                return wpfBmp;
                            }
                            finally
                            {
                                DeleteObject(hBitmap);
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static ImageSource GetDefaultAppIcon()
        {
            if (_defaultAppIcon != null) return _defaultAppIcon;

            DrawingGroup group = new DrawingGroup();
            using (DrawingContext dc = group.Open())
            {
                Brush bg = new SolidColorBrush(Color.FromArgb(180, 22, 27, 34));
                Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 210, 255)), 1.0);
                dc.DrawRoundedRectangle(bg, borderPen, new Rect(1, 1, 22, 22), 4, 4);

                Pen cyanPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 210, 255)), 1.3);
                dc.DrawRoundedRectangle(null, cyanPen, new Rect(5, 5, 14, 10), 2, 2);
                dc.DrawLine(cyanPen, new Point(9, 17), new Point(15, 17));
                dc.DrawLine(cyanPen, new Point(12, 15), new Point(12, 17));
            }
            DrawingImage img = new DrawingImage(group);
            img.Freeze();
            _defaultAppIcon = img;
            return _defaultAppIcon;
        }

        private FrameworkElement BuildOptionsView()
        {
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel stack = new StackPanel { Margin = new Thickness(4) };

            Border card = Styles.CreateGlassCard(20, 12);
            StackPanel cStack = new StackPanel();

            TextBlock optTitle = new TextBlock
            {
                Text = "Options Système & Comportement",
                Foreground = Styles.TextWhiteBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Margin = new Thickness(0, 0, 0, 16)
            };
            cStack.Children.Add(optTitle);

            // Auto-start CheckBox
            _chkAutoStart = new CheckBox
            {
                Content = "🚀 Lancer Scroll-it automatiquement au démarrage de Windows",
                Foreground = Styles.TextWhiteBrush,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 16),
                IsChecked = SettingsManager.Current.StartWithWindows,
                Cursor = Cursors.Hand
            };
            _chkAutoStart.Checked += (s, e) => { SettingsManager.SetAutoStart(true); TrayManager.UpdateState(); };
            _chkAutoStart.Unchecked += (s, e) => { SettingsManager.SetAutoStart(false); TrayManager.UpdateState(); };
            cStack.Children.Add(_chkAutoStart);

            // Ctrl Zoom CheckBox
            _chkCtrlZoom = new CheckBox
            {
                Content = "🔍 Préserver le zoom natif Ctrl + Molette (zoom précis sans interpolation)",
                Foreground = Styles.TextWhiteBrush,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 16),
                IsChecked = SettingsManager.Current.BypassCtrlZoom,
                Cursor = Cursors.Hand
            };
            _chkCtrlZoom.Checked += (s, e) => { SettingsManager.Current.BypassCtrlZoom = true; SettingsManager.Save(); };
            _chkCtrlZoom.Unchecked += (s, e) => { SettingsManager.Current.BypassCtrlZoom = false; SettingsManager.Save(); };
            cStack.Children.Add(_chkCtrlZoom);

            // Minimize to tray on close
            _chkMinimizeToTray = new CheckBox
            {
                Content = "📥 Réduire dans la barre des tâches (Systray) lors de la fermeture",
                Foreground = Styles.TextWhiteBrush,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 24),
                IsChecked = SettingsManager.Current.MinimizeToTrayOnClose,
                Cursor = Cursors.Hand
            };
            _chkMinimizeToTray.Checked += (s, e) => { SettingsManager.Current.MinimizeToTrayOnClose = true; SettingsManager.Save(); };
            _chkMinimizeToTray.Unchecked += (s, e) => { SettingsManager.Current.MinimizeToTrayOnClose = false; SettingsManager.Save(); };
            cStack.Children.Add(_chkMinimizeToTray);

            // Reset Defaults Button
            Button resetBtn = Styles.CreatePillButton("↺ Réinitialiser tous les réglages par défaut", false);
            resetBtn.Click += (s, e) =>
            {
                SettingsManager.Current.BypassCtrlZoom = true;
                SettingsManager.Current.MinimizeToTrayOnClose = true;
                if (_chkCtrlZoom != null) _chkCtrlZoom.IsChecked = true;
                if (_chkMinimizeToTray != null) _chkMinimizeToTray.IsChecked = true;
                SelectPreset("Mac OS", true);
            };
            cStack.Children.Add(resetBtn);

            card.Child = cStack;
            stack.Children.Add(card);

            scroll.Content = stack;
            return scroll;
        }

        private Stopwatch _presetAnimStopwatch = null;
        private EventHandler _presetRenderHandler = null;
        private double _animStartStep, _animTargetStep;
        private double _animStartTime, _animTargetTime;
        private double _animStartAccel, _animTargetAccel;
        private double _animStartTail, _animTargetTail;

        private void StopPresetAnimation()
        {
            if (_presetRenderHandler != null)
            {
                CompositionTarget.Rendering -= _presetRenderHandler;
                _presetRenderHandler = null;
            }
            _presetAnimStopwatch = null;

            if (_stepSlider != null) _stepSlider.IsSnapToTickEnabled = true;
            if (_timeSlider != null) _timeSlider.IsSnapToTickEnabled = true;
            if (_accelSlider != null) _accelSlider.IsSnapToTickEnabled = true;
            if (_tailSlider != null) _tailSlider.IsSnapToTickEnabled = true;
        }

        private void SelectPreset(string pName, bool animate = true)
        {
            if (!SettingsManager.Presets.ContainsKey(pName)) return;

            ScrollPreset preset = SettingsManager.Presets[pName];

            // 1. Update settings directly under _isUpdatingUI guard
            _isUpdatingUI = true;
            SettingsManager.Current.ActivePreset = pName;
            SettingsManager.Current.StepSize = preset.StepSize;
            SettingsManager.Current.AnimationTime = preset.AnimationTime;
            SettingsManager.Current.AccelerationMultiplier = preset.AccelerationMultiplier;
            SettingsManager.Current.FrictionTail = preset.FrictionTail;
            SettingsManager.Save();
            TrayManager.UpdateState();

            // 2. Highlight active preset button immediately
            foreach (var pair in _presetButtons)
            {
                bool isAct = (pair.Key == pName);
                pair.Value.Background = isAct ? (Brush)Styles.AccentGradient : (Brush)new SolidColorBrush(Color.FromArgb(80, 48, 54, 61));
                pair.Value.Foreground = isAct ? (Brush)Brushes.Black : (Brush)Styles.TextWhiteBrush;
            }

            // 3. Smooth fade on description text
            string desc = preset.Description;
            if (_presetDescText != null)
            {
                if (animate && _presetDescText.Text != desc)
                {
                    DoubleAnimation fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(80));
                    fadeOut.Completed += (s, e) =>
                    {
                        _presetDescText.Text = desc;
                        _presetDescText.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(160)));
                    };
                    _presetDescText.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }
                else
                {
                    _presetDescText.Text = desc;
                }
            }

            // 4. Smoothly animate sliders to target preset values
            if (!animate)
            {
                StopPresetAnimation();
                if (_stepSlider != null) _stepSlider.Value = preset.StepSize;
                if (_timeSlider != null) _timeSlider.Value = preset.AnimationTime;
                if (_accelSlider != null) _accelSlider.Value = preset.AccelerationMultiplier;
                if (_tailSlider != null) _tailSlider.Value = preset.FrictionTail;
                _isUpdatingUI = false;
            }
            else
            {
                AnimateSlidersTo(preset.StepSize, preset.AnimationTime, preset.AccelerationMultiplier, preset.FrictionTail);
            }
        }

        private void LoadSettingsToUI(bool animate = false)
        {
            if (_isUpdatingUI) return;

            AppSettings cfg = SettingsManager.Current;

            _isUpdatingUI = true;
            StopPresetAnimation();

            if (_stepSlider != null) _stepSlider.Value = cfg.StepSize;
            if (_timeSlider != null) _timeSlider.Value = cfg.AnimationTime;
            if (_accelSlider != null) _accelSlider.Value = cfg.AccelerationMultiplier;
            if (_tailSlider != null) _tailSlider.Value = cfg.FrictionTail;

            if (_chkAutoStart != null) _chkAutoStart.IsChecked = cfg.StartWithWindows;
            if (_chkCtrlZoom != null) _chkCtrlZoom.IsChecked = cfg.BypassCtrlZoom;
            if (_chkMinimizeToTray != null) _chkMinimizeToTray.IsChecked = cfg.MinimizeToTrayOnClose;

            // Highlight active preset pill
            foreach (var pair in _presetButtons)
            {
                bool isAct = (pair.Key == cfg.ActivePreset);
                pair.Value.Background = isAct ? (Brush)Styles.AccentGradient : (Brush)new SolidColorBrush(Color.FromArgb(80, 48, 54, 61));
                pair.Value.Foreground = isAct ? (Brush)Brushes.Black : (Brush)Styles.TextWhiteBrush;
            }

            string desc = SettingsManager.Presets.ContainsKey(cfg.ActivePreset)
                ? SettingsManager.Presets[cfg.ActivePreset].Description
                : "Paramètres personnalisés ajustés manuellement.";

            if (_presetDescText != null)
            {
                _presetDescText.Text = desc;
            }

            UpdateStatusUI();
            _isUpdatingUI = false;
        }

        private void AnimateSlidersTo(double targetStep, double targetTime, double targetAccel, double targetTail)
        {
            StopPresetAnimation();

            _isUpdatingUI = true;

            // Disable snap-to-tick temporarily for ultra-fluid interpolation
            if (_stepSlider != null) _stepSlider.IsSnapToTickEnabled = false;
            if (_timeSlider != null) _timeSlider.IsSnapToTickEnabled = false;
            if (_accelSlider != null) _accelSlider.IsSnapToTickEnabled = false;
            if (_tailSlider != null) _tailSlider.IsSnapToTickEnabled = false;

            _animStartStep = (_stepSlider != null) ? _stepSlider.Value : targetStep;
            _animTargetStep = targetStep;

            _animStartTime = (_timeSlider != null) ? _timeSlider.Value : targetTime;
            _animTargetTime = targetTime;

            _animStartAccel = (_accelSlider != null) ? _accelSlider.Value : targetAccel;
            _animTargetAccel = targetAccel;

            _animStartTail = (_tailSlider != null) ? _tailSlider.Value : targetTail;
            _animTargetTail = targetTail;

            _presetAnimStopwatch = Stopwatch.StartNew();
            const double durationMs = 380.0;

            _presetRenderHandler = (s, e) =>
            {
                if (_presetAnimStopwatch == null) return;

                double elapsed = _presetAnimStopwatch.Elapsed.TotalMilliseconds;
                double t = Math.Min(1.0, elapsed / durationMs);

                // Quartic ease out: 1 - (1 - t)^4
                double ease = 1.0 - Math.Pow(1.0 - t, 4);

                _isUpdatingUI = true;

                if (t >= 1.0)
                {
                    if (_stepSlider != null) { _stepSlider.Value = _animTargetStep; _stepSlider.IsSnapToTickEnabled = true; }
                    if (_timeSlider != null) { _timeSlider.Value = _animTargetTime; _timeSlider.IsSnapToTickEnabled = true; }
                    if (_accelSlider != null) { _accelSlider.Value = _animTargetAccel; _accelSlider.IsSnapToTickEnabled = true; }
                    if (_tailSlider != null) { _tailSlider.Value = _animTargetTail; _tailSlider.IsSnapToTickEnabled = true; }
                    StopPresetAnimation();
                    _isUpdatingUI = false;
                    return;
                }

                if (_stepSlider != null)
                    _stepSlider.Value = _animStartStep + (_animTargetStep - _animStartStep) * ease;
                if (_timeSlider != null)
                    _timeSlider.Value = _animStartTime + (_animTargetTime - _animStartTime) * ease;
                if (_accelSlider != null)
                    _accelSlider.Value = _animStartAccel + (_animTargetAccel - _animStartAccel) * ease;
                if (_tailSlider != null)
                    _tailSlider.Value = _animStartTail + (_animTargetTail - _animStartTail) * ease;
            };

            CompositionTarget.Rendering += _presetRenderHandler;
        }

        private void UpdateStatusUI(bool animate = false)
        {
            bool enabled = SettingsManager.Current.Enabled;
            double targetX = enabled ? 16.0 : 0.0;
            Color targetColor = enabled ? Styles.SuccessGreen : Color.FromRgb(48, 54, 61);

            if (_toggleThumbTransform != null)
            {
                if (!animate)
                {
                    _toggleThumbTransform.BeginAnimation(TranslateTransform.XProperty, null);
                    _toggleThumbTransform.X = targetX;
                }
                else
                {
                    QuarticEase ease = new QuarticEase { EasingMode = EasingMode.EaseOut };
                    DoubleAnimation slideAnim = new DoubleAnimation(_toggleThumbTransform.X, targetX, TimeSpan.FromMilliseconds(220))
                    {
                        EasingFunction = ease
                    };
                    _toggleThumbTransform.BeginAnimation(TranslateTransform.XProperty, slideAnim);
                }
            }

            if (_toggleTrackBrush != null)
            {
                if (!animate)
                {
                    _toggleTrackBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
                    _toggleTrackBrush.Color = targetColor;
                }
                else
                {
                    QuarticEase ease = new QuarticEase { EasingMode = EasingMode.EaseOut };
                    ColorAnimation colorAnim = new ColorAnimation(_toggleTrackBrush.Color, targetColor, TimeSpan.FromMilliseconds(220))
                    {
                        EasingFunction = ease
                    };
                    _toggleTrackBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
                }
            }

            if (_statusText != null)
            {
                string targetText = enabled ? "Actif" : "Inactif";
                Brush targetBrush = enabled ? Styles.TextWhiteBrush : Styles.TextMutedBrush;

                if (!animate)
                {
                    _statusText.Text = targetText;
                    _statusText.Foreground = targetBrush;
                    _statusText.Opacity = 1.0;
                }
                else if (_statusText.Text != targetText)
                {
                    DoubleAnimation fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(80));
                    fadeOut.Completed += (s, e) =>
                    {
                        _statusText.Text = targetText;
                        _statusText.Foreground = targetBrush;
                        _statusText.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(140)));
                    };
                    _statusText.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }
            }
        }

        private void OnSettingsUpdated()
        {
            if (_isUpdatingUI) return;
            Dispatcher.Invoke(new Action(() =>
            {
                if (_isUpdatingUI) return;
                LoadSettingsToUI(false);
                TrayManager.UpdateState();
            }));
        }
    }
}
