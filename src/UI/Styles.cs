using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace ScrollIt.UI
{
    public static class Styles
    {
        public static readonly Color BgDark = Color.FromRgb(13, 17, 23);
        public static readonly Color CardBg = Color.FromRgb(22, 27, 34);
        public static readonly Color CardBorder = Color.FromRgb(48, 54, 61);
        public static readonly Color AccentPrimary = Color.FromRgb(0, 210, 255);
        public static readonly Color AccentSecondary = Color.FromRgb(58, 123, 213);
        public static readonly Color TextWhite = Color.FromRgb(240, 246, 252);
        public static readonly Color TextMuted = Color.FromRgb(139, 148, 158);
        public static readonly Color SuccessGreen = Color.FromRgb(46, 204, 113);
        public static readonly Color DangerRed = Color.FromRgb(231, 76, 60);

        public static SolidColorBrush BgBrush = new SolidColorBrush(BgDark);
        public static SolidColorBrush CardBrush = new SolidColorBrush(CardBg);
        public static SolidColorBrush CardBorderBrush = new SolidColorBrush(CardBorder);
        public static SolidColorBrush AccentBrush = new SolidColorBrush(AccentPrimary);
        public static SolidColorBrush TextWhiteBrush = new SolidColorBrush(TextWhite);
        public static SolidColorBrush TextMutedBrush = new SolidColorBrush(TextMuted);
        public static SolidColorBrush SuccessBrush = new SolidColorBrush(SuccessGreen);

        public static LinearGradientBrush AccentGradient = new LinearGradientBrush(
            AccentPrimary,
            AccentSecondary,
            new Point(0, 0),
            new Point(1, 1)
        );

        public static Border CreateGlassCard(double padding = 16, double cornerRadius = 12)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(220, 22, 27, 34)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 48, 54, 61)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(cornerRadius),
                Padding = new Thickness(padding),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 4,
                    BlurRadius = 16,
                    Opacity = 0.35
                }
            };
        }

        public static Button CreatePillButton(string text, bool isActive = false)
        {
            Button btn = new Button
            {
                Content = text,
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(4, 0, 4, 0),
                Foreground = isActive ? (Brush)new SolidColorBrush(Colors.Black) : (Brush)TextWhiteBrush,
                Background = isActive ? (Brush)AccentGradient : (Brush)new SolidColorBrush(Color.FromArgb(80, 48, 54, 61)),
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "Border";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(20));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);

            template.VisualTree = border;
            btn.Template = template;
            return btn;
        }

        public static UIElement CreateProjectLogo(double size)
        {
            Canvas canvas = new Canvas { Width = size, Height = size };

            Ellipse circle = new Ellipse
            {
                Width = size - 2,
                Height = size - 2,
                Stroke = AccentBrush,
                StrokeThickness = Math.Max(1.5, size * 0.075),
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
    }
}
