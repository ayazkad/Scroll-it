using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace ScrollIt.UI
{
    public static class Styles
    {
        // Theme Colors
        public static Color BgDark = Color.FromRgb(13, 17, 23);
        public static Color CardBg = Color.FromRgb(22, 27, 34);
        public static Color CardBorder = Color.FromRgb(48, 54, 61);
        public static Color AccentPrimary = Color.FromRgb(0, 210, 255);
        public static Color AccentSecondary = Color.FromRgb(58, 123, 213);
        public static readonly Color TextWhite = Color.FromRgb(240, 246, 252);
        public static readonly Color TextMuted = Color.FromRgb(139, 148, 158);
        public static readonly Color SuccessGreen = Color.FromRgb(46, 204, 113);
        public static readonly Color DangerRed = Color.FromRgb(231, 76, 60);

        // Brushes
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

        public static event Action ThemeChanged;

        public static void ApplyTheme(string accentName, string backdropStyle)
        {
            // 1. Accent Colors
            if (string.Equals(accentName, "Purple", StringComparison.OrdinalIgnoreCase))
            {
                AccentPrimary = Color.FromRgb(176, 102, 254);
                AccentSecondary = Color.FromRgb(99, 102, 241);
            }
            else if (string.Equals(accentName, "Emerald", StringComparison.OrdinalIgnoreCase))
            {
                AccentPrimary = Color.FromRgb(16, 185, 129);
                AccentSecondary = Color.FromRgb(5, 150, 105);
            }
            else if (string.Equals(accentName, "Sunset", StringComparison.OrdinalIgnoreCase))
            {
                AccentPrimary = Color.FromRgb(255, 101, 132);
                AccentSecondary = Color.FromRgb(255, 142, 83);
            }
            else if (string.Equals(accentName, "Electric", StringComparison.OrdinalIgnoreCase))
            {
                AccentPrimary = Color.FromRgb(59, 130, 246);
                AccentSecondary = Color.FromRgb(29, 78, 216);
            }
            else if (string.Equals(accentName, "Rose", StringComparison.OrdinalIgnoreCase))
            {
                AccentPrimary = Color.FromRgb(244, 63, 94);
                AccentSecondary = Color.FromRgb(236, 72, 153);
            }
            else // Default Cyan
            {
                AccentPrimary = Color.FromRgb(0, 210, 255);
                AccentSecondary = Color.FromRgb(58, 123, 213);
            }

            // 2. Backdrop & Card Backgrounds
            if (string.Equals(backdropStyle, "Acrylic", StringComparison.OrdinalIgnoreCase))
            {
                // Translucent acrylic glass (lets Windows Acrylic blur show through fully)
                BgDark = Color.FromArgb(20, 10, 14, 20);
                CardBg = Color.FromArgb(125, 20, 26, 36);
                CardBorder = Color.FromArgb(70, 75, 95, 120);
            }
            else if (string.Equals(backdropStyle, "Mica", StringComparison.OrdinalIgnoreCase))
            {
                // Translucent Mica tint
                BgDark = Color.FromArgb(35, 12, 16, 22);
                CardBg = Color.FromArgb(145, 22, 28, 38);
                CardBorder = Color.FromArgb(60, 65, 85, 110);
            }
            else if (string.Equals(backdropStyle, "OledBlack", StringComparison.OrdinalIgnoreCase))
            {
                BgDark = Color.FromRgb(5, 7, 10);
                CardBg = Color.FromRgb(13, 16, 23);
                CardBorder = Color.FromRgb(36, 41, 47);
            }
            else // Classic GlassDark
            {
                BgDark = Color.FromRgb(13, 17, 23);
                CardBg = Color.FromRgb(22, 27, 34);
                CardBorder = Color.FromRgb(48, 54, 61);
            }

            // Update live brushes with new non-frozen instances
            BgBrush = new SolidColorBrush(BgDark);
            CardBrush = new SolidColorBrush(CardBg);
            CardBorderBrush = new SolidColorBrush(CardBorder);
            AccentBrush = new SolidColorBrush(AccentPrimary);

            AccentGradient = new LinearGradientBrush(
                AccentPrimary,
                AccentSecondary,
                new Point(0, 0),
                new Point(1, 1)
            );

            if (ThemeChanged != null)
            {
                ThemeChanged();
            }
        }

        public static Border CreateGlassCard(double padding = 16, double cornerRadius = 12)
        {
            return new Border
            {
                Background = CardBrush,
                BorderBrush = CardBorderBrush,
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

        public static ControlTemplate CreateCustomSliderTemplate()
        {
            string hexPrimary = string.Format("#{0:X2}{1:X2}{2:X2}", AccentPrimary.R, AccentPrimary.G, AccentPrimary.B);
            string hexSecondary = string.Format("#{0:X2}{1:X2}{2:X2}", AccentSecondary.R, AccentSecondary.G, AccentSecondary.B);

            string xaml = string.Format(@"
<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='{{x:Type Slider}}'>
    <Grid VerticalAlignment='Center'>
        <!-- Background Track -->
        <Border Height='6' CornerRadius='3' Background='#1A202C' BorderBrush='#5030363D' BorderThickness='1' Margin='0,0,0,0'/>
        <!-- Track Component -->
        <Track x:Name='PART_Track'>
            <Track.DecreaseRepeatButton>
                <RepeatButton Command='Slider.DecreaseLarge' Focusable='False' IsHitTestVisible='False'>
                    <RepeatButton.Template>
                        <ControlTemplate TargetType='{{x:Type RepeatButton}}'>
                            <Border Height='6' CornerRadius='3'>
                                <Border.Background>
                                    <LinearGradientBrush StartPoint='0,0' EndPoint='1,0'>
                                        <GradientStop Color='{0}' Offset='0.0'/>
                                        <GradientStop Color='{1}' Offset='1.0'/>
                                    </LinearGradientBrush>
                                </Border.Background>
                            </Border>
                        </ControlTemplate>
                    </RepeatButton.Template>
                </RepeatButton>
            </Track.DecreaseRepeatButton>
            <Track.IncreaseRepeatButton>
                <RepeatButton Command='Slider.IncreaseLarge' Focusable='False' IsHitTestVisible='False'>
                    <RepeatButton.Template>
                        <ControlTemplate TargetType='{{x:Type RepeatButton}}'>
                            <Border Height='6' Background='Transparent'/>
                        </ControlTemplate>
                    </RepeatButton.Template>
                </RepeatButton>
            </Track.IncreaseRepeatButton>
            <Track.Thumb>
                <Thumb Cursor='Hand'>
                    <Thumb.Template>
                        <ControlTemplate TargetType='{{x:Type Thumb}}'>
                            <Grid Width='18' Height='18'>
                                <Ellipse x:Name='thumbCircle' Fill='White' Stroke='{0}' StrokeThickness='2.5'>
                                    <Ellipse.Effect>
                                        <DropShadowEffect Color='Black' BlurRadius='6' ShadowDepth='2' Opacity='0.45'/>
                                    </Ellipse.Effect>
                                </Ellipse>
                            </Grid>
                            <ControlTemplate.Triggers>
                                <Trigger Property='IsMouseOver' Value='True'>
                                    <Setter TargetName='thumbCircle' Property='StrokeThickness' Value='3.0'/>
                                    <Setter TargetName='thumbCircle' Property='Effect'>
                                        <Setter.Value>
                                            <DropShadowEffect Color='{0}' BlurRadius='10' ShadowDepth='0' Opacity='0.85'/>
                                        </Setter.Value>
                                    </Setter>
                                </Trigger>
                                <Trigger Property='IsDragging' Value='True'>
                                    <Setter TargetName='thumbCircle' Property='StrokeThickness' Value='3.5'/>
                                    <Setter TargetName='thumbCircle' Property='Effect'>
                                        <Setter.Value>
                                            <DropShadowEffect Color='{0}' BlurRadius='12' ShadowDepth='0' Opacity='0.95'/>
                                        </Setter.Value>
                                    </Setter>
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Thumb.Template>
                </Thumb>
            </Track.Thumb>
        </Track>
    </Grid>
</ControlTemplate>", hexPrimary, hexSecondary);
            return (ControlTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
        }

        public static ControlTemplate CreateCustomComboBoxTemplate()
        {
            string hexPrimary = string.Format("#{0:X2}{1:X2}{2:X2}", AccentPrimary.R, AccentPrimary.G, AccentPrimary.B);
            string hexBorder = string.Format("#{0:X2}{1:X2}{2:X2}", CardBorder.R, CardBorder.G, CardBorder.B);

            string xaml = string.Format(@"
<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='{{x:Type ComboBox}}'>
    <Grid>
        <ToggleButton Name='ToggleButton'
                      Focusable='False'
                      IsChecked='{{Binding Path=IsDropDownOpen, Mode=TwoWay, RelativeSource={{RelativeSource TemplatedParent}}}}'
                      ClickMode='Press'
                      Cursor='Hand'>
            <ToggleButton.Template>
                <ControlTemplate TargetType='{{x:Type ToggleButton}}'>
                    <Border Name='Border'
                            Background='#141A23'
                            BorderBrush='{0}'
                            BorderThickness='1'
                            CornerRadius='6'
                            Padding='10,6,10,6'
                            SnapsToDevicePixels='True'>
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width='*'/>
                                <ColumnDefinition Width='24'/>
                            </Grid.ColumnDefinitions>
                            <Path Name='Arrow'
                                  Grid.Column='1'
                                  HorizontalAlignment='Center'
                                  VerticalAlignment='Center'
                                  Data='M 0 0 L 4.5 4.5 L 9 0'
                                  Stroke='#8B949E'
                                  StrokeThickness='1.8'
                                  StrokeStartLineCap='Round'
                                  StrokeEndLineCap='Round'/>
                        </Grid>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property='IsMouseOver' Value='True'>
                            <Setter TargetName='Border' Property='BorderBrush' Value='{1}'/>
                            <Setter TargetName='Border' Property='Background' Value='#1B232F'/>
                            <Setter TargetName='Arrow' Property='Stroke' Value='{1}'/>
                        </Trigger>
                        <Trigger Property='IsChecked' Value='True'>
                            <Setter TargetName='Border' Property='BorderBrush' Value='{1}'/>
                            <Setter TargetName='Border' Property='Background' Value='#1B232F'/>
                            <Setter TargetName='Arrow' Property='Stroke' Value='{1}'/>
                            <Setter TargetName='Arrow' Property='RenderTransform'>
                                <Setter.Value>
                                    <RotateTransform Angle='180' CenterX='4.5' CenterY='2.25'/>
                                </Setter.Value>
                            </Setter>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </ToggleButton.Template>
        </ToggleButton>
        <ContentPresenter Name='ContentSite'
                          IsHitTestVisible='False'
                          Content='{{TemplateBinding SelectionBoxItem}}'
                          ContentTemplate='{{TemplateBinding SelectionBoxItemTemplate}}'
                          ContentTemplateSelector='{{TemplateBinding ItemTemplateSelector}}'
                          Margin='10,3,32,3'
                          VerticalAlignment='Center'
                          HorizontalAlignment='Left'/>
        <Popup Name='Popup'
               Placement='Bottom'
               IsOpen='{{TemplateBinding IsDropDownOpen}}'
               AllowsTransparency='True'
               Focusable='False'
               PopupAnimation='Fade'
               VerticalOffset='4'>
            <Grid Name='DropDown'
                  SnapsToDevicePixels='True'
                  MinWidth='{{TemplateBinding ActualWidth}}'
                  MaxHeight='{{TemplateBinding MaxDropDownHeight}}'>
                <Border Name='DropDownBorder'
                        Background='#141922'
                        BorderThickness='1'
                        BorderBrush='#384454'
                        CornerRadius='8'
                        Margin='0,0,0,4'
                        Padding='4'>
                    <Border.Effect>
                        <DropShadowEffect Color='Black' BlurRadius='14' ShadowDepth='3' Opacity='0.65'/>
                    </Border.Effect>
                    <ScrollViewer Margin='0' SnapsToDevicePixels='True' VerticalScrollBarVisibility='Auto'>
                        <StackPanel IsItemsHost='True' KeyboardNavigation.DirectionalNavigation='Contained'/>
                    </ScrollViewer>
                </Border>
            </Grid>
        </Popup>
    </Grid>
</ControlTemplate>", hexBorder, hexPrimary);

            return (ControlTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
        }

        public static Style CreateCustomComboBoxItemStyle()
        {
            string hexPrimary = string.Format("#{0:X2}{1:X2}{2:X2}", AccentPrimary.R, AccentPrimary.G, AccentPrimary.B);

            string xaml = string.Format(@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       TargetType='{{x:Type ComboBoxItem}}'>
    <Setter Property='SnapsToDevicePixels' Value='True'/>
    <Setter Property='Foreground' Value='#F0F6FC'/>
    <Setter Property='FontSize' Value='12'/>
    <Setter Property='Padding' Value='8,6,8,6'/>
    <Setter Property='Margin' Value='2,1,2,1'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
        <Setter.Value>
            <ControlTemplate TargetType='{{x:Type ComboBoxItem}}'>
                <Border Name='ItemBorder'
                        Background='Transparent'
                        CornerRadius='6'
                        Padding='{{TemplateBinding Padding}}'
                        SnapsToDevicePixels='True'>
                    <ContentPresenter VerticalAlignment='Center' HorizontalAlignment='Left'/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property='IsMouseOver' Value='True'>
                        <Setter TargetName='ItemBorder' Property='Background' Value='#222D3D'/>
                    </Trigger>
                    <Trigger Property='IsSelected' Value='True'>
                        <Setter TargetName='ItemBorder' Property='Background' Value='#243347'/>
                        <Setter TargetName='ItemBorder' Property='BorderBrush' Value='{0}'/>
                        <Setter TargetName='ItemBorder' Property='BorderThickness' Value='1'/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>", hexPrimary);

            return (Style)System.Windows.Markup.XamlReader.Parse(xaml);
        }
        public static Style CreateCustomScrollViewerStyle()
        {
            string hexPrimary = string.Format("#{0:X2}{1:X2}{2:X2}", AccentPrimary.R, AccentPrimary.G, AccentPrimary.B);

            string xaml = string.Format(@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       TargetType='{{x:Type ScrollViewer}}'>
    <Setter Property='OverridesDefaultStyle' Value='True'/>
    <Setter Property='SnapsToDevicePixels' Value='True'/>
    <Setter Property='Template'>
        <Setter.Value>
            <ControlTemplate TargetType='{{x:Type ScrollViewer}}'>
                <Grid Background='Transparent'>
                    <ScrollContentPresenter CanContentScroll='{{TemplateBinding CanContentScroll}}' Margin='{{TemplateBinding Padding}}'/>
                    <ScrollBar Name='PART_VerticalScrollBar'
                               HorizontalAlignment='Right'
                               Value='{{TemplateBinding VerticalOffset}}'
                               Maximum='{{TemplateBinding ScrollableHeight}}'
                               ViewportSize='{{TemplateBinding ViewportHeight}}'
                               Visibility='{{TemplateBinding ComputedVerticalScrollBarVisibility}}'
                               Width='6'
                               Margin='0,2,2,2'
                               Cursor='Arrow'>
                        <ScrollBar.Template>
                            <ControlTemplate TargetType='{{x:Type ScrollBar}}'>
                                <Grid Background='Transparent'>
                                    <Track Name='PART_Track' IsDirectionReversed='True'>
                                        <Track.DecreaseRepeatButton>
                                            <RepeatButton Command='ScrollBar.PageUpCommand' Opacity='0' Focusable='False'/>
                                        </Track.DecreaseRepeatButton>
                                        <Track.IncreaseRepeatButton>
                                            <RepeatButton Command='ScrollBar.PageDownCommand' Opacity='0' Focusable='False'/>
                                        </Track.IncreaseRepeatButton>
                                        <Track.Thumb>
                                            <Thumb Cursor='Hand'>
                                                <Thumb.Template>
                                                    <ControlTemplate TargetType='{{x:Type Thumb}}'>
                                                        <Border Name='thumbBorder'
                                                                Background='#424F60'
                                                                CornerRadius='3'
                                                                Margin='0'
                                                                SnapsToDevicePixels='True'/>
                                                        <ControlTemplate.Triggers>
                                                            <Trigger Property='IsMouseOver' Value='True'>
                                                                <Setter TargetName='thumbBorder' Property='Background' Value='{0}'/>
                                                            </Trigger>
                                                            <Trigger Property='IsDragging' Value='True'>
                                                                <Setter TargetName='thumbBorder' Property='Background' Value='{0}'/>
                                                            </Trigger>
                                                        </ControlTemplate.Triggers>
                                                    </ControlTemplate>
                                                </Thumb.Template>
                                            </Thumb>
                                        </Track.Thumb>
                                    </Track>
                                </Grid>
                            </ControlTemplate>
                        </ScrollBar.Template>
                    </ScrollBar>
                    <ScrollBar Name='PART_HorizontalScrollBar'
                               Grid.Column='0'
                               Grid.Row='0'
                               VerticalAlignment='Bottom'
                               Value='{{TemplateBinding HorizontalOffset}}'
                               Maximum='{{TemplateBinding ScrollableWidth}}'
                               ViewportSize='{{TemplateBinding ViewportWidth}}'
                               Visibility='{{TemplateBinding ComputedHorizontalScrollBarVisibility}}'
                               Height='6'
                               Margin='2,0,2,2'
                               Orientation='Horizontal'
                               Cursor='Arrow'>
                        <ScrollBar.Template>
                            <ControlTemplate TargetType='{{x:Type ScrollBar}}'>
                                <Grid Background='Transparent'>
                                    <Track Name='PART_Track'>
                                        <Track.DecreaseRepeatButton>
                                            <RepeatButton Command='ScrollBar.PageLeftCommand' Opacity='0' Focusable='False'/>
                                        </Track.DecreaseRepeatButton>
                                        <Track.IncreaseRepeatButton>
                                            <RepeatButton Command='ScrollBar.PageRightCommand' Opacity='0' Focusable='False'/>
                                        </Track.IncreaseRepeatButton>
                                        <Track.Thumb>
                                            <Thumb Cursor='Hand'>
                                                <Thumb.Template>
                                                    <ControlTemplate TargetType='{{x:Type Thumb}}'>
                                                        <Border Name='thumbBorderH'
                                                                Background='#424F60'
                                                                CornerRadius='3'
                                                                Margin='0'
                                                                SnapsToDevicePixels='True'/>
                                                        <ControlTemplate.Triggers>
                                                            <Trigger Property='IsMouseOver' Value='True'>
                                                                <Setter TargetName='thumbBorderH' Property='Background' Value='{0}'/>
                                                            </Trigger>
                                                            <Trigger Property='IsDragging' Value='True'>
                                                                <Setter TargetName='thumbBorderH' Property='Background' Value='{0}'/>
                                                            </Trigger>
                                                        </ControlTemplate.Triggers>
                                                    </ControlTemplate>
                                                </Thumb.Template>
                                            </Thumb>
                                        </Track.Thumb>
                                    </Track>
                                </Grid>
                            </ControlTemplate>
                        </ScrollBar.Template>
                    </ScrollBar>
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>", hexPrimary);

            return (Style)System.Windows.Markup.XamlReader.Parse(xaml);
        }
    }
}
