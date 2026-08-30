using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.Win32;
using ScrollIt.Engine;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;
using Path = System.IO.Path;
using ProgressBar = System.Windows.Controls.ProgressBar;
using TextBox = System.Windows.Controls.TextBox;

namespace ScrollIt.Setup
{
    public class SetupWindow : Window
    {
        // Pure sleek black theme
        private static readonly SolidColorBrush BgBrush = new SolidColorBrush(Color.FromRgb(8, 10, 14));
        private static readonly SolidColorBrush CardBgBrush = new SolidColorBrush(Color.FromRgb(15, 18, 24));
        private static readonly SolidColorBrush CardBorderBrush = new SolidColorBrush(Color.FromArgb(90, 48, 54, 61));
        private static readonly SolidColorBrush TextWhiteBrush = new SolidColorBrush(Color.FromRgb(240, 246, 252));
        private static readonly SolidColorBrush TextMutedBrush = new SolidColorBrush(Color.FromRgb(139, 148, 158));
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
        private int _currentStep = 1;

        // Configuration state
        private TextBox _txtInstallPath;
        private CheckBox _chkDesktopShortcut;
        private CheckBox _chkStartMenuShortcut;
        private CheckBox _chkAutoStart;
        private CheckBox _chkLaunchAfter;

        // Progress UI
        private ProgressBar _progressBar;
        private TextBlock _lblProgressStatus;
        private TextBlock _lblProgressDetail;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        public SetupWindow()
        {
            I18n.SetAutoLanguage();

            Title = I18n.T("Setup_WindowTitle");
            Width = 620;
            Height = 470;
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
                Stream iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("scroll-it.ico");
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

            ShowStep1();
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
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(68) }); // 2: Bottom navigation bar (pinned to bottom)

            // Title Bar with Windows 11 rounded top corners
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
                Text = I18n.T("Setup_HeaderTitle"),
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

            closeBtn.Click += (s, e) =>
            {
                if (_currentStep < 3)
                {
                    ShowConfirmModal(I18n.T("Setup_CancelConfirm"), () => Close());
                }
                else
                {
                    Close();
                }
            };
            titleBar.Children.Add(closeBtn);

            root.Children.Add(titleBarBorder);
            Grid.SetRow(titleBarBorder, 0);

            // Main Content Area (Row 1)
            _mainContentGrid = new Grid
            {
                Margin = new Thickness(28, 14, 28, 0)
            };
            root.Children.Add(_mainContentGrid);
            Grid.SetRow(_mainContentGrid, 1);

            // Bottom Bar Area (Row 2 - Pinned to bottom)
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

            // In-app Modal Dialog Overlay Grid (Spans full window)
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

        private void ShowConfirmModal(string message, Action onYes, Action onNo = null)
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
                Padding = new Thickness(22, 18, 22, 18),
                MaxWidth = 540,
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

            StackPanel headerStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };

            Grid iconCircle = new Grid { Width = 36, Height = 36, Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center };
            Ellipse cBg = new Ellipse { Fill = new SolidColorBrush(Color.FromArgb(40, 0, 210, 255)), Stroke = AccentBrush, StrokeThickness = 1.5 };
            TextBlock qMark = new TextBlock
            {
                Text = "?",
                Foreground = AccentBrush,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconCircle.Children.Add(cBg);
            iconCircle.Children.Add(qMark);
            headerStack.Children.Add(iconCircle);

            TextBlock msgTxt = new TextBlock
            {
                Text = message,
                Foreground = TextWhiteBrush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.NoWrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerStack.Children.Add(msgTxt);
            mStack.Children.Add(headerStack);

            StackPanel btnStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            string yesText = (I18n.CurrentLanguage == AppLanguage.French) ? "Oui" : (I18n.CurrentLanguage == AppLanguage.Russian ? "Да" : "Yes");
            string noText = (I18n.CurrentLanguage == AppLanguage.French) ? "Non" : (I18n.CurrentLanguage == AppLanguage.Russian ? "Нет" : "No");

            Button btnNo = CreateButton(noText, false, () =>
            {
                _modalOverlayGrid.Visibility = Visibility.Collapsed;
                _modalOverlayGrid.Children.Clear();
                if (onNo != null) onNo();
            });
            btnNo.Width = 84;
            btnNo.Margin = new Thickness(0, 0, 10, 0);
            btnStack.Children.Add(btnNo);

            Button btnYes = CreateButton(yesText, true, () =>
            {
                _modalOverlayGrid.Visibility = Visibility.Collapsed;
                _modalOverlayGrid.Children.Clear();
                if (onYes != null) onYes();
            });
            btnYes.Width = 84;
            btnStack.Children.Add(btnYes);

            mStack.Children.Add(btnStack);
            modalCard.Child = mStack;
            _modalOverlayGrid.Children.Add(modalCard);
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

            Button btnOk = CreateButton("OK", true, () =>
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
                        Title = I18n.T("Setup_WindowTitle");
                        if (_titleTxt != null) _titleTxt.Text = I18n.T("Setup_HeaderTitle");
                        UpdateLanguageDropdownText();

                        if (_currentStep == 1) ShowStep1();
                        else if (_currentStep == 2) ShowStep2();
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

        #region Step 1: Welcome Screen
        private void ShowStep1()
        {
            _currentStep = 1;
            _mainContentGrid.Children.Clear();
            _bottomBarGrid.Children.Clear();

            StackPanel centerPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Border logoContainer = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 18),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(0, 210, 255),
                    BlurRadius = 24,
                    ShadowDepth = 0,
                    Opacity = 0.65
                }
            };
            logoContainer.Child = CreateProjectLogo(76);
            centerPanel.Children.Add(logoContainer);

            TextBlock welcomeHeading = new TextBlock
            {
                Text = I18n.T("Setup_WelcomeHeading"),
                Foreground = TextWhiteBrush,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            centerPanel.Children.Add(welcomeHeading);

            TextBlock welcomeDesc = new TextBlock
            {
                Text = I18n.T("Setup_WelcomeDesc"),
                Foreground = TextMutedBrush,
                FontSize = 13,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 480,
                LineHeight = 18
            };
            centerPanel.Children.Add(welcomeDesc);
            _mainContentGrid.Children.Add(centerPanel);

            // Bottom Language Dropdown on Left (Column 0)
            UIElement langBar = CreateLanguageDropdown();
            _bottomBarGrid.Children.Add(langBar);
            Grid.SetColumn(langBar, 0);

            // Bottom Buttons
            Button btnCancel = CreateButton(I18n.T("Setup_BtnCancel"), false, () => ShowConfirmModal(I18n.T("Setup_CancelConfirm"), () => Close()));
            btnCancel.Width = 96;
            _bottomBarGrid.Children.Add(btnCancel);
            Grid.SetColumn(btnCancel, 1);

            Button btnNext = CreateButton(I18n.T("Setup_BtnNext"), true, () => ShowStep2());
            btnNext.Width = 114;
            btnNext.Margin = new Thickness(10, 0, 0, 0);
            _bottomBarGrid.Children.Add(btnNext);
            Grid.SetColumn(btnNext, 2);
        }
        #endregion

        #region Step 2: Custom Path & Options
        private void ShowStep2()
        {
            _currentStep = 2;
            _mainContentGrid.Children.Clear();
            _bottomBarGrid.Children.Clear();

            StackPanel stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            TextBlock lblPathTitle = new TextBlock
            {
                Text = I18n.T("Setup_PathTitle"),
                Foreground = TextWhiteBrush,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            stack.Children.Add(lblPathTitle);

            TextBlock lblPathDesc = new TextBlock
            {
                Text = I18n.T("Setup_PathDesc"),
                Foreground = TextMutedBrush,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(lblPathDesc);

            // Path Selector Card
            Border pathCard = new Border
            {
                Background = CardBgBrush,
                BorderBrush = CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 16)
            };

            Grid pathGrid = new Grid();
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            string defaultPath = _txtInstallPath != null ? _txtInstallPath.Text : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Scroll-it");
            _txtInstallPath = new TextBox
            {
                Text = defaultPath,
                Background = new SolidColorBrush(Color.FromRgb(10, 12, 16)),
                Foreground = TextWhiteBrush,
                BorderBrush = CardBorderBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            pathGrid.Children.Add(_txtInstallPath);
            Grid.SetColumn(_txtInstallPath, 0);

            Button btnBrowse = CreateButton(I18n.T("Setup_BtnBrowse"), false, () =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = I18n.T("Setup_BrowseDialogDesc");
                    fbd.SelectedPath = _txtInstallPath.Text;
                    if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        _txtInstallPath.Text = Path.Combine(fbd.SelectedPath, "Scroll-it");
                    }
                }
            });
            btnBrowse.Padding = new Thickness(12, 6, 12, 6);
            pathGrid.Children.Add(btnBrowse);
            Grid.SetColumn(btnBrowse, 1);

            pathCard.Child = pathGrid;
            stack.Children.Add(pathCard);

            // Options checkboxes
            TextBlock lblOptionsTitle = new TextBlock
            {
                Text = I18n.T("Setup_OptionsTitle"),
                Foreground = TextWhiteBrush,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(lblOptionsTitle);

            _chkDesktopShortcut = new CheckBox
            {
                Content = I18n.T("Setup_ChkDesktop"),
                Foreground = TextWhiteBrush,
                FontSize = 12,
                IsChecked = _chkDesktopShortcut != null ? _chkDesktopShortcut.IsChecked : true,
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            stack.Children.Add(_chkDesktopShortcut);

            _chkStartMenuShortcut = new CheckBox
            {
                Content = I18n.T("Setup_ChkStartMenu"),
                Foreground = TextWhiteBrush,
                FontSize = 12,
                IsChecked = _chkStartMenuShortcut != null ? _chkStartMenuShortcut.IsChecked : true,
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            stack.Children.Add(_chkStartMenuShortcut);

            _chkAutoStart = new CheckBox
            {
                Content = I18n.T("Setup_ChkAutoStart"),
                Foreground = TextWhiteBrush,
                FontSize = 12,
                IsChecked = _chkAutoStart != null ? _chkAutoStart.IsChecked : true,
                Margin = new Thickness(0, 0, 0, 2),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            stack.Children.Add(_chkAutoStart);

            _mainContentGrid.Children.Add(stack);

            // Bottom Language Dropdown on Left (Column 0)
            UIElement langBar = CreateLanguageDropdown();
            _bottomBarGrid.Children.Add(langBar);
            Grid.SetColumn(langBar, 0);

            // Bottom Buttons
            Button btnBack = CreateButton(I18n.T("Setup_BtnBack"), false, () => ShowStep1());
            btnBack.Width = 100;
            _bottomBarGrid.Children.Add(btnBack);
            Grid.SetColumn(btnBack, 1);

            Button btnInstall = CreateButton(I18n.T("Setup_BtnInstall"), true, () => StartInstallation());
            btnInstall.Width = 114;
            btnInstall.Margin = new Thickness(10, 0, 0, 0);
            _bottomBarGrid.Children.Add(btnInstall);
            Grid.SetColumn(btnInstall, 2);
        }
        #endregion

        #region Step 3: Installation Progress
        private void StartInstallation()
        {
            _currentStep = 3;
            _mainContentGrid.Children.Clear();
            _bottomBarGrid.Children.Clear();

            StackPanel stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 480
            };

            _lblProgressStatus = new TextBlock
            {
                Text = I18n.T("Setup_ProgressTitle"),
                Foreground = TextWhiteBrush,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(_lblProgressStatus);

            _progressBar = new ProgressBar
            {
                Height = 10,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Background = CardBgBrush,
                Foreground = AccentBrush,
                BorderBrush = CardBorderBrush,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(_progressBar);

            _lblProgressDetail = new TextBlock
            {
                Text = I18n.T("Setup_ProgressPrep"),
                Foreground = TextMutedBrush,
                FontSize = 11
            };
            stack.Children.Add(_lblProgressDetail);

            _mainContentGrid.Children.Add(stack);

            string targetFolder = _txtInstallPath.Text.Trim();
            bool makeDesktop = _chkDesktopShortcut.IsChecked == true;
            bool makeStartMenu = _chkStartMenuShortcut.IsChecked == true;
            bool autoStart = _chkAutoStart.IsChecked == true;

            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    PerformInstall(targetFolder, makeDesktop, makeStartMenu, autoStart);
                    Dispatcher.Invoke(new Action(() => ShowStep4(targetFolder)));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ShowAlertModal(I18n.T("Setup_ErrorGeneral") + ex.Message, () => ShowStep2());
                    }));
                }
            });
        }

        private void PerformInstall(string installDir, bool makeDesktop, bool makeStartMenu, bool autoStart)
        {
            UpdateProgress(15, I18n.T("Setup_ProgressStopProcesses"));
            foreach (Process p in Process.GetProcessesByName("Scroll-it"))
            {
                try { p.Kill(); p.WaitForExit(1000); } catch { }
            }
            foreach (Process p in Process.GetProcessesByName("scroll-it"))
            {
                try { p.Kill(); p.WaitForExit(1000); } catch { }
            }
            foreach (Process p in Process.GetProcessesByName("Scroll-it-Portable"))
            {
                try { p.Kill(); p.WaitForExit(1000); } catch { }
            }
            Thread.Sleep(300);

            UpdateProgress(35, I18n.T("Setup_ProgressCreateDir"));
            if (!Directory.Exists(installDir))
            {
                Directory.CreateDirectory(installDir);
            }

            UpdateProgress(55, I18n.T("Setup_ProgressExtract"));
            string targetExe = Path.Combine(installDir, "Scroll-it.exe");
            string targetIcon = Path.Combine(installDir, "scroll-it.ico");
            string targetUninstaller = Path.Combine(installDir, "Uninstall.exe");

            // 1. Extract or Copy Scroll-it.exe
            if (!ExtractEmbeddedOrCopy("Scroll-it.exe", targetExe))
            {
                throw new FileNotFoundException(I18n.T("Setup_ErrorExtract"));
            }

            // 2. Extract or Copy scroll-it.ico
            ExtractEmbeddedOrCopy("scroll-it.ico", targetIcon);

            // 3. Copy Setup itself as Uninstall.exe
            string sourceUninstaller = null;
            try { var mod = Process.GetCurrentProcess().MainModule; if (mod != null) sourceUninstaller = mod.FileName; } catch { }
            if (string.IsNullOrEmpty(sourceUninstaller) || !File.Exists(sourceUninstaller))
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                sourceUninstaller = Path.Combine(currentDir, "Scroll-it-Setup.exe");
                if (!File.Exists(sourceUninstaller)) sourceUninstaller = Path.Combine(currentDir, @"bin\Scroll-it-Setup.exe");
            }
            
            if (File.Exists(sourceUninstaller))
            {
                try
                {
                    if (!string.Equals(sourceUninstaller, targetUninstaller, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(sourceUninstaller, targetUninstaller, true);
                    }
                }
                catch { }
            }

            // 4. Save initial language preference into settings.json
            try
            {
                string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scroll-it");
                if (!Directory.Exists(appDataFolder)) Directory.CreateDirectory(appDataFolder);
                string settingsPath = Path.Combine(appDataFolder, "settings.json");
                if (!File.Exists(settingsPath))
                {
                    string initialJson = string.Format("{{\"ActivePreset\":\"Mac OS\",\"AnimationTime\":400.0,\"AccelerationMultiplier\":1.4,\"BlacklistedApps\":[],\"BypassCtrlZoom\":true,\"Enabled\":true,\"FrictionTail\":0.95,\"Language\":\"{0}\",\"MinimizeToTrayOnClose\":true,\"StartWithWindows\":{1},\"StepSize\":120.0}}",
                        I18n.CurrentLanguageCode,
                        autoStart ? "true" : "false");
                    File.WriteAllText(settingsPath, initialJson);
                }
            }
            catch { }

            UpdateProgress(75, I18n.T("Setup_ProgressShortcuts"));
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic shell = Activator.CreateInstance(shellType);

                if (makeStartMenu)
                {
                    string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Scroll-it.lnk");
                    dynamic shortcut = shell.CreateShortcut(startMenu);
                    shortcut.TargetPath = targetExe;
                    shortcut.WorkingDirectory = installDir;
                    shortcut.Description = "Scroll-it - " + I18n.T("AppTagline");
                    shortcut.IconLocation = (File.Exists(targetIcon) ? targetIcon : targetExe) + ",0";
                    shortcut.Save();
                }

                if (makeDesktop)
                {
                    string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Scroll-it.lnk");
                    dynamic shortcut = shell.CreateShortcut(desktop);
                    shortcut.TargetPath = targetExe;
                    shortcut.WorkingDirectory = installDir;
                    shortcut.Description = "Scroll-it - " + I18n.T("AppTagline");
                    shortcut.IconLocation = (File.Exists(targetIcon) ? targetIcon : targetExe) + ",0";
                    shortcut.Save();
                }
            }

            UpdateProgress(90, I18n.T("Setup_ProgressRegistry"));
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Scroll-it"))
            {
                if (key != null)
                {
                    key.SetValue("DisplayName", "Scroll-it");
                    key.SetValue("DisplayIcon", (File.Exists(targetIcon) ? targetIcon : targetExe) + ",0");
                    key.SetValue("DisplayVersion", "1.1.1");
                    key.SetValue("Publisher", "Scroll-it");
                    key.SetValue("InstallLocation", installDir);
                    key.SetValue("UninstallString", "\"" + targetUninstaller + "\"");
                    key.SetValue("QuietUninstallString", "\"" + targetUninstaller + "\" /silent");
                    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                }
            }

            if (autoStart)
            {
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null)
                    {
                        runKey.SetValue("Scroll-it", "\"" + targetExe + "\" --minimized");
                    }
                }
            }

            UpdateProgress(100, I18n.T("Setup_ProgressComplete"));
            Thread.Sleep(400);
        }

        private void UpdateProgress(int percent, string status)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _progressBar.Value = percent;
                _lblProgressDetail.Text = status;
            }));
        }
        #endregion

        #region Step 4: Finish Screen
        private void ShowStep4(string installDir)
        {
            _currentStep = 4;
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
                Text = I18n.T("Setup_FinishTitle"),
                Foreground = TextWhiteBrush,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            center.Children.Add(finishTitle);

            TextBlock finishDesc = new TextBlock
            {
                Text = I18n.T("Setup_FinishDesc"),
                Foreground = TextMutedBrush,
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            center.Children.Add(finishDesc);

            _chkLaunchAfter = new CheckBox
            {
                Content = I18n.T("Setup_ChkLaunchNow"),
                Foreground = TextWhiteBrush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                IsChecked = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            center.Children.Add(_chkLaunchAfter);

            _mainContentGrid.Children.Add(center);

            // Finish button
            Button btnFinish = CreateButton(I18n.T("Setup_BtnFinish"), true, () =>
            {
                if (_chkLaunchAfter.IsChecked == true)
                {
                    string targetExe = Path.Combine(installDir, "Scroll-it.exe");
                    if (File.Exists(targetExe))
                    {
                        Process.Start(new ProcessStartInfo(targetExe) { WorkingDirectory = installDir });
                    }
                }
                Close();
            });
            btnFinish.Width = 120;
            _bottomBarGrid.Children.Add(btnFinish);
            Grid.SetColumn(btnFinish, 2);
        }
        #endregion

        // Real vector project logo
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

        private Button CreateButton(string text, bool isPrimary, Action action)
        {
            Button btn = new Button
            {
                Content = text,
                Foreground = isPrimary ? (Brush)Brushes.Black : TextWhiteBrush,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Height = 32
            };

            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "btnBorder";
            border.SetValue(Border.BackgroundProperty, isPrimary ? (Brush)AccentGradient : CardBgBrush);
            border.SetValue(Border.BorderBrushProperty, isPrimary ? Brushes.Transparent : CardBorderBrush);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.PaddingProperty, new Thickness(16, 6, 16, 6));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);

            ControlTemplate tpl = new ControlTemplate(typeof(Button));

            Trigger hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            if (isPrimary)
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

        private static bool ExtractEmbeddedOrCopy(string resourceOrFileName, string destinationPath)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resName = null;
                foreach (string name in assembly.GetManifestResourceNames())
                {
                    if (name.Equals(resourceOrFileName, StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("." + resourceOrFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        resName = name;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(resName))
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resName))
                    {
                        if (stream != null)
                        {
                            using (FileStream fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                            {
                                byte[] buffer = new byte[81920];
                                int read;
                                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    fs.Write(buffer, 0, read);
                                }
                            }
                            return true;
                        }
                    }
                }

                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string sourceFile = Path.Combine(currentDir, resourceOrFileName);
                if (!File.Exists(sourceFile)) sourceFile = Path.Combine(currentDir, @"bin\" + resourceOrFileName);
                if (!File.Exists(sourceFile)) sourceFile = Path.Combine(currentDir, @"src\" + resourceOrFileName);

                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, destinationPath, true);
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
