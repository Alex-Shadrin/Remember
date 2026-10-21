using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace Remember
{
    public static class Ui
    {
        public static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
        public static SolidColorBrush Brush(string hex) { var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex); b.Freeze(); return b; }
        public static readonly Brush Bg = Brush("#121415"), Card = Brush("#1B1E20"), Line = Brush("#272B2E"), Raised = Brush("#222529"), Text = Brush("#F4F4F2"), Secondary = Brush("#A9AFB5"), Muted = Brush("#737B83"), Accent = Brush("#79AFFF");
        public static Brush Color(Category c) { return c == Category.Subscription ? Brush("#C4A1FF") : c == Category.Birthday ? Brush("#F1BC8B") : Accent; }
        public static TextBlock Label(string text, double size = 13, Brush color = null)
        { return new TextBlock { Text = text, FontSize = size, Foreground = color ?? Text, VerticalAlignment = VerticalAlignment.Center }; }
        public static TextBlock Icon(string glyph, double size = 16, Brush color = null)
        { var t = Label(glyph, size, color); t.FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"); return t; }
        public static StackPanel Stack(double gap = 0) { return new StackPanel { Margin = new Thickness(gap) }; }
        public static Border Panel(UIElement child, double padding = 16, Brush bg = null)
        { return new Border { Background = bg ?? Card, BorderBrush = Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(padding), Child = child }; }
        public static Button Button(string text, Action action, string style = null)
        {
            var b = new Button { Content = text };
            if (style != null) b.Style = (Style)Application.Current.FindResource(style);
            b.Click += (s, e) => action();
            return b;
        }
        public static Button IconButton(string glyph, string tooltip, Action action)
        {
            var b = Button("", action, "Quiet"); b.Content = Icon(glyph); b.ToolTip = tooltip;
            b.Width = 34; b.Height = 34; b.Padding = new Thickness(0);
            System.Windows.Automation.AutomationProperties.SetName(b, tooltip);
            return b;
        }
        public static FrameworkElement Field(string caption, FrameworkElement input, string hint = null)
        {
            var stack = Stack(); stack.Margin = new Thickness(0, 0, 0, 15);
            var label = Label(caption, 12, Secondary); label.Margin = new Thickness(0, 0, 0, 7); stack.Children.Add(label);
            System.Windows.Automation.AutomationProperties.SetName(input, caption);
            stack.Children.Add(input);
            if (hint != null) { var h = Label(hint, 11, Muted); h.TextWrapping = TextWrapping.Wrap; h.Margin = new Thickness(0, 6, 0, 0); stack.Children.Add(h); }
            return stack;
        }
        public static Grid Columns(params GridLength[] widths)
        { var g = new Grid(); foreach (var w in widths) g.ColumnDefinitions.Add(new ColumnDefinition { Width = w }); return g; }
        public static void Put(Grid g, UIElement child, int column) { Grid.SetColumn(child, column); g.Children.Add(child); }
        public static string DayText(DateTime date)
        {
            int days = (date.Date - DateTime.Today).Days;
            return days == 0 ? "Сегодня" : days == 1 ? "Завтра" : days < 0 ? "Просрочено на " + (-days) + " дн." : "Через " + days + " дн.";
        }

        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
        public static void Chrome(Window window)
        {
            window.Background = Bg; window.Foreground = Text; window.FontFamily = new FontFamily("Segoe UI"); window.FontSize = 13;
            window.UseLayoutRounding = true; window.SnapsToDevicePixels = true;
            window.WindowStyle = WindowStyle.None;
            WindowChrome.SetWindowChrome(window, new WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(window.ResizeMode == ResizeMode.NoResize ? 0 : 6), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(12), UseAeroCaptionButtons = false });
            window.SourceInitialized += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                int dark = 1, corner = 2;
                try { DwmSetWindowAttribute(hwnd, 20, ref dark, 4); DwmSetWindowAttribute(hwnd, 33, ref corner, 4); } catch (DllNotFoundException) { }
            };
        }
        public static DockPanel TitleBar(Window window, string title, bool minimize = false)
        {
            var bar = new DockPanel { LastChildFill = true, Margin = new Thickness(16, 9, 10, 0), Background = Brushes.Transparent };
            var controls = new StackPanel { Orientation = Orientation.Horizontal };
            if (minimize) controls.Children.Add(IconButton("\uE921", "Свернуть", () => window.WindowState = WindowState.Minimized));
            controls.Children.Add(IconButton("\uE8BB", minimize ? "Скрыть в трей" : "Закрыть", window.Close));
            DockPanel.SetDock(controls, Dock.Right); bar.Children.Add(controls);
            bar.Children.Add(Label(title, 12, Secondary));
            bar.MouseLeftButtonDown += (s, e) => { if (e.ChangedButton == MouseButton.Left && e.OriginalSource is TextBlock) { try { window.DragMove(); } catch (InvalidOperationException) { } } };
            return bar;
        }
        public static DockPanel DialogLayout(Window window, string title, UIElement body)
        {
            var root = new DockPanel(); var top = TitleBar(window, title); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
            var host = new Border { Padding = new Thickness(24, 17, 24, 24), Child = body }; root.Children.Add(host); return root;
        }
        public static void Dialog(Window window, Window owner, double width, double height)
        {
            window.Owner = owner; window.Width = width; window.Height = Math.Min(height, SystemParameters.WorkArea.Height - 24); window.ResizeMode = ResizeMode.NoResize;
            window.WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
            window.ShowInTaskbar = false; Chrome(window);
            window.PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) { window.Close(); e.Handled = true; } };
        }
        public static void Error(Window owner, string message) { new MessageDialog(owner, "Не удалось выполнить действие", message).ShowDialog(); }
    }
}
