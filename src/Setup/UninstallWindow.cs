using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.Win32;
using ScrollIt.Engine;
using Path = System.IO.Path;

namespace ScrollIt.Setup
{
    public class UninstallWindow : Window
    {
        private static readonly SolidColorBrush BgBrush = new SolidColorBrush(Color.FromRgb(8, 10, 14));
        private static readonly SolidColorBrush CardBgBrush = new SolidColorBrush(Color.FromRgb(15, 18, 24));
        private static readonly SolidColorBrush CardBorderBrush = new SolidColorBrush(Color.FromArgb(90, 48, 54, 61));
        private static readonly SolidColorBrush TextWhiteBrush = new SolidColorBrush(Color.FromRgb(240, 246, 252));
        private static readonly SolidColorBrush TextMutedBrush = new SolidColorBrush(Color.FromRgb(139, 148, 158));
        private static readonly SolidColorBrush DangerBrush = new SolidColorBrush(Color.FromRgb(248, 81, 73));
        private static readonly SolidColorBrush AccentBrush = new SolidColorBrush(Color.FromRgb(0, 210, 255));
        private static readonly LinearGradientBrush AccentGradient = new LinearGradientBrush(
            Color.FromRgb(0, 210, 255),
            Color.FromRgb(0, 120, 255),
            new Point(0, 0),
            new Point(1, 1)
        );

        private Grid _mainContentGrid;
        private Grid _bottomBarGrid;
        private Grid _modalOverlayGrid;
        private TextBlock _titleTxt;
        private ToggleButton _btnLanguageDropdown;
        private Popup _langPopup;
        private CheckBox _chkDeleteSettings;
        private ProgressBar _progressBar;
        private TextBlock _lblStatus;
        private int _currentStep = 1;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        public UninstallWindow()
        {
            I18n.SetAutoLanguage();

            Title = I18n.T("Uninst_WindowTitle");
            Width = 560;
            Height = 390;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;

            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            TextOptions.SetTextHintingMode(this, TextHintingMode.Fixed);
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            SourceInitialized += (s, e) =>
            {
                try
                {
                    IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    int pref = DWMWCP_ROUND;
                    DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
                }
                catch { }
            };

            PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (_btnLanguageDropdown != null && _btnLanguageDropdown.IsChecked == true)
                {
                    if (!_btnLanguageDropdown.IsMouseOver)
                    {
                        _btnLanguageDropdown.IsChecked = false;
                    }
                }
            };

            Deactivated += (s, e) =>
            {
                if (_btnLanguageDropdown != null)
                {
                    _btnLanguageDropdown.IsChecked = false;
                }
            };

            BuildUI();

            try
            {
                Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("scroll-it.ico");
                if (iconStream != null)
                {
                    Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconStream);
                }
                else
                {
                    string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"src\scroll-it.ico");
                    if (!System.IO.File.Exists(iconPath)) iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scroll-it.ico");
                    if (System.IO.File.Exists(iconPath))
                    {
                        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
                    }
                }
            }
            catch { }

            ShowConfirmScreen();
        }

        private void BuildUI()
        {
            Border outerBorder = new Border
            {
                Background = BgBrush,
                BorderBrush = CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0)
            };

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) }); // 0: Title bar
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 1: Dynamic Content
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) }); // 2: Bottom navigation bar (pinned to bottom)

            Border titleBarBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(14, 17, 22)),
                CornerRadius = new CornerRadius(9, 9, 0, 0)
            };
            titleBarBorder.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };

            Grid titleBar = new Grid();
            titleBarBorder.Child = titleBar;

            StackPanel titleLeft = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(18, 0, 0, 0)
            };

            UIElement smallLogo = CreateProjectLogo(24);
            FrameworkElement smallElem = smallLogo as FrameworkElement;
            if (smallElem != null) smallElem.Margin = new Thickness(0, 0, 10, 0);
            titleLeft.Children.Add(smallLogo);

            _titleTxt = new TextBlock
            {
                Text = I18n.T("Uninst_HeaderTitle"),
                Foreground = TextWhiteBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleLeft.Children.Add(_titleTxt);
            titleBar.Children.Add(titleLeft);

            // Right side: Close button
            Button closeBtn = new Button
            {
                Content = "✕",
                Width = 34,
                Height = 28,
                Foreground = TextMutedBrush,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 12, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            FrameworkElementFactory closeBorder = new FrameworkElementFactory(typeof(Border));
            closeBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            FrameworkElementFactory closeContent = new FrameworkElementFactory(typeof(ContentPresenter));
            closeContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            closeContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            closeBorder.AppendChild(closeContent);
            ControlTemplate closeTpl = new ControlTemplate(typeof(Button));
            closeTpl.VisualTree = closeBorder;
            closeBtn.Template = closeTpl;

            closeBtn.Click += (s, e) => Close();
            titleBar.Children.Add(closeBtn);

            root.Children.Add(titleBarBorder);
            Grid.SetRow(titleBarBorder, 0);

            _mainContentGrid = new Grid { Margin = new Thickness(28, 16, 28, 0) };
            root.Children.Add(_mainContentGrid);
            Grid.SetRow(_mainContentGrid, 1);

            _bottomBarGrid = new Grid
            {
                Margin = new Thickness(28, 0, 28, 18),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            _bottomBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _bottomBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _bottomBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.Children.Add(_bottomBarGrid);
            Grid.SetRow(_bottomBarGrid, 2);

            // In-app Modal Dialog Overlay Grid
            _modalOverlayGrid = new Grid
            {
                Visibility = Visibility.Collapsed
            };
            root.Children.Add(_modalOverlayGrid);
            Grid.SetRow(_modalOverlayGrid, 0);
            Grid.SetRowSpan(_modalOverlayGrid, 3);

            outerBorder.Child = root;
            Content = outerBorder;
        }

        private void ShowAlertModal(string message, Action onOk = null)
        {
            _modalOverlayGrid.Children.Clear();
            _modalOverlayGrid.Visibility = Visibility.Visible;

            Border backdrop = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(190, 0, 0, 0)),
                CornerRadius = new CornerRadius(9)
            };
            _modalOverlayGrid.Children.Add(backdrop);

            Border modalCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(18, 22, 29)),
                BorderBrush = CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(24, 20, 24, 20),
                MaxWidth = 440,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 24,
                    ShadowDepth = 4,
                    Opacity = 0.85
                }
            };

            StackPanel mStack = new StackPanel();

            TextBlock msgTxt = new TextBlock
            {
                Text = message,
                Foreground = TextWhiteBrush,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 18),
                LineHeight = 18
            };
            mStack.Children.Add(msgTxt);

            StackPanel btnStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button btnOk = CreateButton("OK", CardBgBrush, () =>
            {
                _modalOverlayGrid.Visibility = Visibility.Collapsed;
                _modalOverlayGrid.Children.Clear();
                if (onOk != null) onOk();
            });
            btnOk.Width = 84;
            btnStack.Children.Add(btnOk);

            mStack.Children.Add(btnStack);
            modalCard.Child = mStack;
            _modalOverlayGrid.Children.Add(modalCard);
        }

        private UIElement CreateLanguageDropdown()
        {
            _btnLanguageDropdown = new ToggleButton
            {
                Height = 32,
                Foreground = TextWhiteBrush,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            UpdateLanguageDropdownText();

            ControlTemplate tpl = new ControlTemplate(typeof(ToggleButton));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "btnBorder";
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(22, 27, 34)));
            border.SetValue(Border.BorderBrushProperty, CardBorderBrush);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.PaddingProperty, new Thickness(12, 4, 12, 4));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);

            Trigger hoverTrigger = new Trigger { Property = ToggleButton.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, AccentBrush, "btnBorder"));
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 37, 48)), "btnBorder"));
            tpl.Triggers.Add(hoverTrigger);

            Trigger checkedTrigger = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
            checkedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, AccentBrush, "btnBorder"));
            checkedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 37, 48)), "btnBorder"));
            tpl.Triggers.Add(checkedTrigger);

            tpl.VisualTree = border;
            _btnLanguageDropdown.Template = tpl;

            _langPopup = new Popup
            {
                PlacementTarget = _btnLanguageDropdown,
                Placement = PlacementMode.Bottom,
                StaysOpen = true,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade
            };

            System.Windows.Data.Binding binding = new System.Windows.Data.Binding("IsChecked")
            {
                Source = _btnLanguageDropdown,
                Mode = System.Windows.Data.BindingMode.TwoWay
            };
            _langPopup.SetBinding(Popup.IsOpenProperty, binding);

            Border popupBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(22, 27, 34)),
                BorderBrush = CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4),
                Margin = new Thickness(0, 4, 0, 0),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 16,
                    ShadowDepth = 3,
                    Opacity = 0.7
                }
            };

            StackPanel pStack = new StackPanel { Width = 110 };

            Action<string, AppLanguage> addItem = (label, lang) =>
            {
                Button itemBtn = new Button
                {
                    Content = label,
                    Foreground = (I18n.CurrentLanguage == lang) ? AccentBrush : TextWhiteBrush,
                    FontSize = 12,
                    FontWeight = (I18n.CurrentLanguage == lang) ? FontWeights.Bold : FontWeights.Normal,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Margin = new Thickness(0, 1, 0, 1)
                };

                ControlTemplate iTpl = new ControlTemplate(typeof(Button));
                FrameworkElementFactory iBorder = new FrameworkElementFactory(typeof(Border));
                iBorder.Name = "iBorder";
                iBorder.SetValue(Border.BackgroundProperty, (I18n.CurrentLanguage == lang) ? new SolidColorBrush(Color.FromArgb(40, 0, 210, 255)) : Brushes.Transparent);
                iBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
                iBorder.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));

                FrameworkElementFactory iContent = new FrameworkElementFactory(typeof(ContentPresenter));
                iContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                iContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                iBorder.AppendChild(iContent);

                Trigger iHover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
                iHover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(32, 40, 52)), "iBorder"));
                iHover.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
                iTpl.Triggers.Add(iHover);

                iTpl.VisualTree = iBorder;
                itemBtn.Template = iTpl;

                itemBtn.Click += (s, e) =>
                {
                    _btnLanguageDropdown.IsChecked = false;
                    if (I18n.CurrentLanguage != lang)
                    {
                        I18n.CurrentLanguage = lang;
                        Title = I18n.T("Uninst_WindowTitle");
                        if (_titleTxt != null) _titleTxt.Text = I18n.T("Uninst_HeaderTitle");
                        UpdateLanguageDropdownText();

                        if (_currentStep == 1) ShowConfirmScreen();
                        else if (_currentStep == 3) ShowFinishScreen();
                    }
                };
                pStack.Children.Add(itemBtn);
            };

            addItem("Français", AppLanguage.French);
            addItem("English", AppLanguage.English);
            addItem("Русский", AppLanguage.Russian);

            popupBorder.Child = pStack;
            _langPopup.Child = popupBorder;

            Grid wrapGrid = new Grid();
            wrapGrid.Children.Add(_btnLanguageDropdown);
            wrapGrid.Children.Add(_langPopup);
            return wrapGrid;
        }

        private void UpdateLanguageDropdownText()
        {
            if (_btnLanguageDropdown == null) return;
            string curName = "Français";
            if (I18n.CurrentLanguage == AppLanguage.English) curName = "English";
            else if (I18n.CurrentLanguage == AppLanguage.Russian) curName = "Русский";
            _btnLanguageDropdown.Content = curName + "  ▾";
        }

        private void ShowConfirmScreen()
        {
            _currentStep = 1;
            _mainContentGrid.Children.Clear();
            _bottomBarGrid.Children.Clear();

            StackPanel stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            TextBlock heading = new TextBlock
            {
                Text = I18n.T("Uninst_ConfirmHeading"),
                Foreground = TextWhiteBrush,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(heading);

            TextBlock desc = new TextBlock
            {
                Text = I18n.T("Uninst_ConfirmDesc"),
                Foreground = TextMutedBrush,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18,
                Margin = new Thickness(0, 0, 0, 16)
            };
            stack.Children.Add(desc);

            _chkDeleteSettings = new CheckBox
            {
                Content = I18n.T("Uninst_ChkDeleteSettings"),
                Foreground = TextWhiteBrush,
                FontSize = 12,
                IsChecked = _chkDeleteSettings != null ? _chkDeleteSettings.IsChecked : false,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            stack.Children.Add(_chkDeleteSettings);

            _mainContentGrid.Children.Add(stack);

            // Bottom Language Dropdown on Left (Column 0)
            UIElement langBar = CreateLanguageDropdown();
            _bottomBarGrid.Children.Add(langBar);
            Grid.SetColumn(langBar, 0);

            // Bottom Buttons
            Button btnCancel = CreateButton(I18n.T("Uninst_BtnCancel"), CardBgBrush, () => Close());
            btnCancel.Width = 100;
            _bottomBarGrid.Children.Add(btnCancel);
            Grid.SetColumn(btnCancel, 1);

            Button btnUninstall = CreateButton(I18n.T("Uninst_BtnUninstall"), DangerBrush, () => StartUninstall());
            btnUninstall.Width = 120;
            btnUninstall.Margin = new Thickness(10, 0, 0, 0);
            _bottomBarGrid.Children.Add(btnUninstall);
            Grid.SetColumn(btnUninstall, 2);
        }

        private void StartUninstall()
        {
            _currentStep = 2;
            _mainContentGrid.Children.Clear();
            _bottomBarGrid.Children.Clear();

            StackPanel stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 440
            };

            TextBlock title = new TextBlock
            {
                Text = I18n.T("Uninst_ProgressTitle"),
                Foreground = TextWhiteBrush,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(title);

            _progressBar = new ProgressBar
            {
                Height = 10,
                Minimum = 0,
                Maximum = 100,
                Value = 10,
                Background = CardBgBrush,
                Foreground = DangerBrush,
                BorderBrush = CardBorderBrush,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(_progressBar);

            _lblStatus = new TextBlock
            {
                Text = I18n.T("Uninst_ProgressStop"),
                Foreground = TextMutedBrush,
                FontSize = 11
            };
            stack.Children.Add(_lblStatus);

            _mainContentGrid.Children.Add(stack);

            bool deleteSettings = _chkDeleteSettings.IsChecked == true;

            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    PerformUninstall(deleteSettings);
                    Dispatcher.Invoke(new Action(() => ShowFinishScreen()));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ShowAlertModal(I18n.T("Uninst_ErrorGeneral") + ex.Message, () => Close());
                    }));
                }
            });
        }

        private void PerformUninstall(bool deleteSettings)
        {
            UpdateProgress(20, I18n.T("Uninst_ProgressStop"));
            foreach (Process p in Process.GetProcessesByName("Scroll-it"))
            {
                try { p.Kill(); p.WaitForExit(1000); } catch { }
            }
            foreach (Process p in Process.GetProcessesByName("scroll-it"))
            {
                try { p.Kill(); p.WaitForExit(1000); } catch { }
            }

            UpdateProgress(40, I18n.T("Uninst_ProgressAutoStart"));
            try
            {
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null)
                    {
                        runKey.DeleteValue("Scroll-it", false);
                        runKey.DeleteValue("scroll-it", false);
                        runKey.DeleteValue("ScrollIt", false);
                    }
                }
            }
            catch { }

            UpdateProgress(60, I18n.T("Uninst_ProgressShortcuts"));
            string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Scroll-it.lnk");
            if (File.Exists(startMenu)) { try { File.Delete(startMenu); } catch { } }

            string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Scroll-it.lnk");
            if (File.Exists(desktop)) { try { File.Delete(desktop); } catch { } }

            UpdateProgress(80, I18n.T("Uninst_ProgressRegistry"));
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Scroll-it", false);
            }
            catch { }

            if (deleteSettings)
            {
                UpdateProgress(90, I18n.T("Uninst_ProgressSettings"));
                string settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scroll-it");
                if (Directory.Exists(settingsFolder)) { try { Directory.Delete(settingsFolder, true); } catch { } }
            }

            UpdateProgress(100, I18n.T("Uninst_ProgressComplete"));
            Thread.Sleep(400);

            string installDir = AppDomain.CurrentDomain.BaseDirectory;
            if (installDir.IndexOf("Scroll-it", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = string.Format("/c timeout /t 2 /nobreak >nul & rmdir /s /q \"{0}\"", installDir.TrimEnd('\\')),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
            }
        }

        private void UpdateProgress(int percent, string status)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _progressBar.Value = percent;
                _lblStatus.Text = status;
            }));
        }

        private void ShowFinishScreen()
        {
            _currentStep = 3;
            _mainContentGrid.Children.Clear();
            _bottomBarGrid.Children.Clear();

            StackPanel center = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            UIElement checkBadge = CreateSuccessCheckmark(64);
            center.Children.Add(checkBadge);

            TextBlock finishTitle = new TextBlock
            {
                Text = I18n.T("Uninst_FinishTitle"),
                Foreground = TextWhiteBrush,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            center.Children.Add(finishTitle);

            _mainContentGrid.Children.Add(center);

            // Finish button
            Button btnClose = CreateButton(I18n.T("Uninst_BtnClose"), CardBgBrush, () => Close());
            btnClose.Width = 110;
            _bottomBarGrid.Children.Add(btnClose);
            Grid.SetColumn(btnClose, 2);
        }

        private static UIElement CreateProjectLogo(double size)
        {
            Canvas canvas = new Canvas { Width = size, Height = size };

            Ellipse circle = new Ellipse
            {
                Width = size - 2,
                Height = size - 2,
                Stroke = AccentBrush,
                StrokeThickness = Math.Max(1.5, size * 0.07),
                Fill = new SolidColorBrush(Color.FromRgb(16, 22, 34))
            };
            Canvas.SetLeft(circle, 1);
            Canvas.SetTop(circle, 1);
            canvas.Children.Add(circle);

            System.Windows.Shapes.Path wave = new System.Windows.Shapes.Path
            {
                Stroke = AccentBrush,
                StrokeThickness = Math.Max(2.0, size * 0.11),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = Geometry.Parse(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "M {0:F1} {1:F1} Q {2:F1} {3:F1} {4:F1} {5:F1} Q {6:F1} {7:F1} {8:F1} {9:F1}",
                    size * 0.32, size * 0.34,
                    size * 0.72, size * 0.40,
                    size * 0.50, size * 0.52,
                    size * 0.28, size * 0.64,
                    size * 0.68, size * 0.68
                ))
            };
            canvas.Children.Add(wave);
            return canvas;
        }

        private static UIElement CreateSuccessCheckmark(double size)
        {
            Grid grid = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };

            Ellipse circle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Color.FromRgb(46, 160, 67))
            };
            grid.Children.Add(circle);

            System.Windows.Shapes.Path check = new System.Windows.Shapes.Path
            {
                Stroke = Brushes.White,
                StrokeThickness = Math.Max(3.2, size * 0.075),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -3, 0, 0),
                Data = Geometry.Parse("M 0 13 L 9.5 22.5 L 26.5 2")
            };
            grid.Children.Add(check);
            return grid;
        }

        private Button CreateButton(string text, Brush bg, Action action)
        {
            bool isAccent = (bg == DangerBrush || bg == AccentGradient);
            Button btn = new Button
            {
                Content = text,
                Foreground = isAccent ? Brushes.White : TextWhiteBrush,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Height = 32
            };

            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "btnBorder";
            border.SetValue(Border.BackgroundProperty, bg);
            border.SetValue(Border.BorderBrushProperty, isAccent ? Brushes.Transparent : CardBorderBrush);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.PaddingProperty, new Thickness(16, 6, 16, 6));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);

            ControlTemplate tpl = new ControlTemplate(typeof(Button));

            Trigger hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            if (isAccent)
            {
                hoverTrigger.Setters.Add(new Setter(Border.OpacityProperty, 0.9, "btnBorder"));
            }
            else
            {
                hoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, AccentBrush, "btnBorder"));
                hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 37, 48)), "btnBorder"));
            }
            tpl.Triggers.Add(hoverTrigger);

            tpl.VisualTree = border;
            btn.Template = tpl;

            btn.Click += (s, e) => action();
            return btn;
        }
    }
}
