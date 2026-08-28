using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ScrollIt.Engine;

namespace ScrollIt.UI
{
    public static class TrayManager
    {
        private static NotifyIcon _notifyIcon;
        private static ToolStripMenuItem _enableMenuItem;
        private static ToolStripMenuItem _autoStartMenuItem;
        private static ToolStripMenuItem _presetsMenu;
        private static ContextMenuStrip _contextMenu;
        private static Action _showMainWindowAction;

        public static void Initialize(Action showMainWindowAction)
        {
            _showMainWindowAction = showMainWindowAction;

            _notifyIcon = new NotifyIcon
            {
                Icon = CreateAppIcon(),
                Text = "Scroll-it",
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) =>
            {
                if (_showMainWindowAction != null) _showMainWindowAction();
            };

            BuildContextMenu();
            I18n.LanguageChanged += () =>
            {
                BuildContextMenu();
                UpdateState();
            };
        }

        public static void UpdateState()
        {
            if (_notifyIcon == null) return;

            bool enabled = SettingsManager.Current.Enabled;
            _notifyIcon.Text = "Scroll-it";
            if (_enableMenuItem != null)
            {
                _enableMenuItem.Checked = enabled;
                _enableMenuItem.Text = enabled ? I18n.T("Tray_StatusActive") : I18n.T("Tray_StatusPaused");
            }

            if (_autoStartMenuItem != null)
            {
                _autoStartMenuItem.Checked = SettingsManager.Current.StartWithWindows;
            }

            if (_presetsMenu != null)
            {
                string currentPreset = SettingsManager.Current.ActivePreset;
                foreach (ToolStripItem item in _presetsMenu.DropDownItems)
                {
                    ToolStripMenuItem menuItem = item as ToolStripMenuItem;
                    if (menuItem != null)
                    {
                        menuItem.Checked = (menuItem.Text == currentPreset);
                    }
                }
            }
        }

        private static void BuildContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(22, 27, 34);
            menu.ForeColor = Color.FromArgb(240, 246, 252);
            menu.Font = new Font("Segoe UI", 9.25F, FontStyle.Regular);
            menu.Renderer = new DarkMenuRenderer();
            menu.ShowImageMargin = true;
            menu.ShowCheckMargin = false;

            // 1. Enable / Disable toggle
            bool isEnabled = SettingsManager.Current.Enabled;
            _enableMenuItem = new ToolStripMenuItem(
                isEnabled ? I18n.T("Tray_StatusActive") : I18n.T("Tray_StatusPaused"),
                null,
                new EventHandler((s, e) =>
                {
                    SettingsManager.Current.Enabled = !SettingsManager.Current.Enabled;
                    SettingsManager.Save();
                    UpdateState();
                })
            )
            {
                Checked = isEnabled,
                Font = new Font("Segoe UI", 9.25F, FontStyle.Bold)
            };
            menu.Items.Add(_enableMenuItem);

            menu.Items.Add(new ToolStripSeparator());

            // 2. Presets submenu
            _presetsMenu = new ToolStripMenuItem(I18n.T("Tray_PresetsMenu"));
            _presetsMenu.DropDown.BackColor = Color.FromArgb(22, 27, 34);
            _presetsMenu.DropDown.ForeColor = Color.FromArgb(240, 246, 252);
            _presetsMenu.DropDown.Font = new Font("Segoe UI", 9.25F, FontStyle.Regular);

            foreach (var pair in SettingsManager.Presets)
            {
                string pName = pair.Key;
                ToolStripMenuItem item = new ToolStripMenuItem(pName, null, new EventHandler((s, e) =>
                {
                    SettingsManager.ApplyPreset(pName);
                    UpdateState();
                }));
                item.Checked = (pName == SettingsManager.Current.ActivePreset);
                _presetsMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(_presetsMenu);

            menu.Items.Add(new ToolStripSeparator());

            // 3. Settings window
            menu.Items.Add(new ToolStripMenuItem(I18n.T("Tray_Settings"), null, new EventHandler((s, e) =>
            {
                if (_showMainWindowAction != null) _showMainWindowAction();
            })));

            // 4. Auto-start
            _autoStartMenuItem = new ToolStripMenuItem(I18n.T("Tray_AutoStart"), null, new EventHandler((s, e) =>
            {
                bool newState = !SettingsManager.Current.StartWithWindows;
                SettingsManager.SetAutoStart(newState);
                UpdateState();
            }))
            {
                Checked = SettingsManager.Current.StartWithWindows
            };
            menu.Items.Add(_autoStartMenuItem);

            menu.Items.Add(new ToolStripSeparator());

            // 5. Exit
            menu.Items.Add(new ToolStripMenuItem(I18n.T("Tray_Exit"), null, new EventHandler((s, e) =>
            {
                ScrollPhysics.Shutdown();
                MouseHook.Uninstall();
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                Environment.Exit(0);
            })));

            _contextMenu = menu;
            _notifyIcon.ContextMenuStrip = menu;
        }

        public static void ShowNotification(string title, string text)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(2000, title, text, ToolTipIcon.Info);
            }
        }

        public static Icon CreateAppIcon()
        {
            try
            {
                string exeLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                Icon exeIcon = Icon.ExtractAssociatedIcon(exeLocation);
                if (exeIcon != null) return exeIcon;
            }
            catch { }

            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scroll-it.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    return new Icon(iconPath, 32, 32);
                }
            }
            catch { }

            return SystemIcons.Application;
        }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        private Color _bg = Color.FromArgb(22, 27, 34);
        private Color _border = Color.FromArgb(48, 54, 61);
        private Color _selected = Color.FromArgb(35, 43, 56);
        private Color _checked = Color.FromArgb(28, 38, 52);

        public override Color MenuBorder { get { return _border; } }
        public override Color MenuItemBorder { get { return Color.Transparent; } }
        public override Color MenuItemSelected { get { return _selected; } }
        public override Color MenuItemSelectedGradientBegin { get { return _selected; } }
        public override Color MenuItemSelectedGradientEnd { get { return _selected; } }
        public override Color MenuItemPressedGradientBegin { get { return _selected; } }
        public override Color MenuItemPressedGradientEnd { get { return _selected; } }
        public override Color MenuItemPressedGradientMiddle { get { return _selected; } }
        public override Color ToolStripDropDownBackground { get { return _bg; } }
        public override Color ImageMarginGradientBegin { get { return _bg; } }
        public override Color ImageMarginGradientMiddle { get { return _bg; } }
        public override Color ImageMarginGradientEnd { get { return _bg; } }
        public override Color SeparatorDark { get { return _border; } }
        public override Color SeparatorLight { get { return Color.Transparent; } }
        public override Color CheckBackground { get { return _checked; } }
        public override Color CheckSelectedBackground { get { return _selected; } }
        public override Color CheckPressedBackground { get { return _selected; } }
        public override Color ButtonSelectedHighlight { get { return _selected; } }
        public override Color ButtonSelectedHighlightBorder { get { return _border; } }
        public override Color ButtonPressedHighlight { get { return _selected; } }
        public override Color ButtonPressedHighlightBorder { get { return _border; } }
    }

    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color TextColor = Color.FromArgb(240, 246, 252);
        private static readonly Color TextDisabledColor = Color.FromArgb(110, 118, 129);
        private static readonly Color AccentColor = Color.FromArgb(0, 210, 255);
        private static readonly Color SeparatorColor = Color.FromArgb(48, 54, 61);
        private static readonly Color HoverBgColor = Color.FromArgb(35, 43, 56);
        private static readonly Color MenuBgColor = Color.FromArgb(22, 27, 34);

        public DarkMenuRenderer() : base(new DarkColorTable())
        {
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                Rectangle rc = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
                using (GraphicsPath path = GetRoundedRectangle(rc, 4))
                using (SolidBrush brush = new SolidBrush(HoverBgColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? TextColor : TextDisabledColor;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (Pen pen = new Pen(SeparatorColor, 1))
            {
                e.Graphics.DrawLine(pen, 28, y, e.Item.Width - 8, y);
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = e.Item.Enabled ? TextColor : TextDisabledColor;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle rc = e.ImageRectangle;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen pen = new Pen(AccentColor, 2f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                int x = rc.Left + rc.Width / 2 - 4;
                int y = rc.Top + rc.Height / 2;

                Point[] pts = new Point[]
                {
                    new Point(x, y),
                    new Point(x + 3, y + 3),
                    new Point(x + 8, y - 3)
                };
                e.Graphics.DrawLines(pen, pts);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            Rectangle rc = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            using (Pen pen = new Pen(SeparatorColor, 1))
            {
                e.Graphics.DrawRectangle(pen, rc);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            Rectangle rc = new Rectangle(0, 0, e.ToolStrip.Width, e.ToolStrip.Height);
            using (SolidBrush brush = new SolidBrush(MenuBgColor))
            {
                e.Graphics.FillRectangle(brush, rc);
            }
        }

        private static GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
