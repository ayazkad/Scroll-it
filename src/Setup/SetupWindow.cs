using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;
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
            Title = "Installation de Scroll-it";
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

            // Real vector project logo in titlebar
            UIElement smallLogo = CreateProjectLogo(24);
            FrameworkElement smallElem = smallLogo as FrameworkElement;
            if (smallElem != null) smallElem.Margin = new Thickness(0, 0, 10, 0);
            titleLeft.Children.Add(smallLogo);

            TextBlock titleTxt = new TextBlock
            {
                Text = "Installation de Scroll-it v1.0.0",
                Foreground = TextWhiteBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleLeft.Children.Add(titleTxt);
            titleBar.Children.Add(titleLeft);

            // Close button without OS hover effect
            Button closeBtn = new Button
            {
                Content = "✕",
                Width = 38,
                Height = 32,
                Foreground = TextMutedBrush,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 10, 0),
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
                    if (MessageBox.Show("Voulez-vous vraiment annuler l'installation de Scroll-it ?", "Scroll-it Setup", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        Close();
                    }
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

            outerBorder.Child = root;
            Content = outerBorder;
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

            // Real glowing project logo badge
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
                Text = "Bienvenue dans le programme d'installation de Scroll-it",
                Foreground = TextWhiteBrush,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            centerPanel.Children.Add(welcomeHeading);

            TextBlock welcomeDesc = new TextBlock
            {
                Text = "Scroll-it apporte le défilement ultra-fluide à l'ensemble de vos applications Windows.",
                Foreground = TextMutedBrush,
                FontSize = 13,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 480,
                LineHeight = 18
            };
            centerPanel.Children.Add(welcomeDesc);
            _mainContentGrid.Children.Add(centerPanel);

            // Bottom Buttons (pinned to bottom)
            Button btnCancel = CreateButton("Annuler", false, () => Close());
            btnCancel.Width = 96;
            _bottomBarGrid.Children.Add(btnCancel);
            Grid.SetColumn(btnCancel, 1);

            Button btnNext = CreateButton("Suivant >", true, () => ShowStep2());
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
                Text = "Dossier de destination",
                Foreground = TextWhiteBrush,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            stack.Children.Add(lblPathTitle);

            TextBlock lblPathDesc = new TextBlock
            {
                Text = "Choisissez le dossier dans lequel installer Scroll-it :",
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
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 16)
            };

            Grid pathGrid = new Grid();
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Scroll-it");
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

            Button btnBrowse = CreateButton("Parcourir...", false, () =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Sélectionnez le dossier d'installation pour Scroll-it";
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
                Text = "Options supplémentaires",
                Foreground = TextWhiteBrush,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(lblOptionsTitle);

            _chkDesktopShortcut = new CheckBox
            {
                Content = "Créer un raccourci sur le Bureau",
                Foreground = TextWhiteBrush,
                FontSize = 12,
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            stack.Children.Add(_chkDesktopShortcut);

            _chkStartMenuShortcut = new CheckBox
            {
                Content = "Ajouter Scroll-it au Menu Démarrer",
                Foreground = TextWhiteBrush,
                FontSize = 12,
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            stack.Children.Add(_chkStartMenuShortcut);

            _chkAutoStart = new CheckBox
            {
                Content = "Lancer Scroll-it automatiquement au démarrage de Windows",
                Foreground = TextWhiteBrush,
                FontSize = 12,
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 2),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            stack.Children.Add(_chkAutoStart);

            _mainContentGrid.Children.Add(stack);

            // Bottom Buttons (pinned to bottom)
            Button btnBack = CreateButton("< Précédent", false, () => ShowStep1());
            btnBack.Width = 100;
            _bottomBarGrid.Children.Add(btnBack);
            Grid.SetColumn(btnBack, 1);

            Button btnInstall = CreateButton("Installer", true, () => StartInstallation());
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
                Text = "Installation de Scroll-it en cours...",
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
                Text = "Préparation des fichiers...",
                Foreground = TextMutedBrush,
                FontSize = 11
            };
            stack.Children.Add(_lblProgressDetail);

            _mainContentGrid.Children.Add(stack);

            // Run installation on background worker
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
                        MessageBox.Show("Erreur lors de l'installation : " + ex.Message, "Scroll-it Setup", MessageBoxButton.OK, MessageBoxImage.Error);
                        ShowStep2();
                    }));
                }
            });
        }

        private void PerformInstall(string installDir, bool makeDesktop, bool makeStartMenu, bool autoStart)
        {
            UpdateProgress(15, "Arrêt des processus existants...");
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

            UpdateProgress(35, "Création du répertoire d'installation...");
            if (!Directory.Exists(installDir))
            {
                Directory.CreateDirectory(installDir);
            }

            UpdateProgress(55, "Extraction des fichiers exécutables et ressources...");
            string targetExe = Path.Combine(installDir, "Scroll-it.exe");
            string targetIcon = Path.Combine(installDir, "scroll-it.ico");
            string targetUninstaller = Path.Combine(installDir, "Uninstall.exe");

            // 1. Extract or Copy Scroll-it.exe
            if (!ExtractEmbeddedOrCopy("Scroll-it.exe", targetExe))
            {
                throw new FileNotFoundException("Impossible d'extraire Scroll-it.exe.");
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

            UpdateProgress(75, "Création des raccourcis système...");
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
                    shortcut.Description = "Scroll-it - Moteur de Défilement Fluide pour Windows";
                    shortcut.IconLocation = (File.Exists(targetIcon) ? targetIcon : targetExe) + ",0";
                    shortcut.Save();
                }

                if (makeDesktop)
                {
                    string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Scroll-it.lnk");
                    dynamic shortcut = shell.CreateShortcut(desktop);
                    shortcut.TargetPath = targetExe;
                    shortcut.WorkingDirectory = installDir;
                    shortcut.Description = "Scroll-it - Moteur de Défilement Fluide pour Windows";
                    shortcut.IconLocation = (File.Exists(targetIcon) ? targetIcon : targetExe) + ",0";
                    shortcut.Save();
                }
            }

            UpdateProgress(90, "Enregistrement dans Windows (Paramètres > Applications)...");
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Scroll-it"))
            {
                if (key != null)
                {
                    key.SetValue("DisplayName", "Scroll-it");
                    key.SetValue("DisplayIcon", (File.Exists(targetIcon) ? targetIcon : targetExe) + ",0");
                    key.SetValue("DisplayVersion", "1.0.0");
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

            UpdateProgress(100, "Installation terminée avec succès !");
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
                Text = "Scroll-it a été installé avec succès !",
                Foreground = TextWhiteBrush,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            center.Children.Add(finishTitle);

            TextBlock finishDesc = new TextBlock
            {
                Text = "L'application est prête à l'emploi et intégrée à votre système Windows.",
                Foreground = TextMutedBrush,
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            center.Children.Add(finishDesc);

            _chkLaunchAfter = new CheckBox
            {
                Content = "Lancer Scroll-it maintenant",
                Foreground = TextWhiteBrush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                IsChecked = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            center.Children.Add(_chkLaunchAfter);

            _mainContentGrid.Children.Add(center);

            // Finish button (pinned to bottom right)
            Button btnFinish = CreateButton("Terminer", true, () =>
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

            // Outer Circle with dark gradient fill and cyan border
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

            // Smooth cyan wave
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
                Margin = new Thickness(0, -3, 0, 0), // Shifted upward for perfect optical center
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
                Foreground = TextWhiteBrush,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, isPrimary ? (Brush)AccentGradient : CardBgBrush);
            border.SetValue(Border.BorderBrushProperty, isPrimary ? Brushes.Transparent : CardBorderBrush);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(0));
            border.SetValue(Border.PaddingProperty, new Thickness(16, 8, 16, 8));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);

            ControlTemplate tpl = new ControlTemplate(typeof(Button));
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

                // Fallback to local filesystem
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
