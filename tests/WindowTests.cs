using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Remember;

static class WindowTests
{
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
    static int Hit(Window window, Point point)
    {
        var screen = window.PointToScreen(point);
        return (int)SendMessage(new WindowInteropHelper(window).Handle, 0x84, IntPtr.Zero, new IntPtr(((int)screen.Y << 16) | ((int)screen.X & 0xffff)));
    }
    static void Pump()
    {
        var frame = new DispatcherFrame(); Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false)); Dispatcher.PushFrame(frame);
    }
    static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) foreach (var child in Descendants(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    static int HitCentre(Window window, FrameworkElement element) { return Hit(window, element.TranslatePoint(new Point(element.ActualWidth / 2, element.ActualHeight / 2), window)); }
    [STAThread]
    static int Main(string[] args)
    {
        Ui.RenderOffscreenForTests = args.Contains("--offscreen");
        var directory = Path.Combine(Path.GetTempPath(), "Remember-window-tests-" + Guid.NewGuid().ToString("N"));
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using (var stream = typeof(AppHost).Assembly.GetManifestResourceStream("Remember.Theme.xaml")) app.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(stream));
        var host = new AppHost(app, new Storage(directory), true, false);
        try
        {
            host.Start(); host.Main.UpdateLayout(); Pump();
            var main = host.Main;
            int empty = Hit(main, new Point(main.ActualWidth - 100, main.ActualHeight - 130));
            var addButton = Descendants(main).OfType<Button>().First(b => b.Content as string == "+  Напоминание");
            int button = HitCentre(main, addButton);
            Console.WriteLine("Native hit test: empty body = " + empty + " (expected HTCAPTION=2); button = " + button + " (expected HTCLIENT=1)");
            Console.WriteLine("Rendering tier: " + (RenderCapability.Tier >> 16));
            int layouts = 0; main.LayoutUpdated += (s, e) => layouts++;
            var firstDay = Descendants(main).OfType<Button>().First(b => b.Content is Grid && b.ToolTip is string);
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++) { main.Left += i % 2 == 0 ? 1 : -1; Pump(); }
            Console.WriteLine("100 actual window-position changes: " + watch.ElapsedMilliseconds + " ms");
            Console.WriteLine("Layout passes while moving: " + layouts);
            if (empty != 2) { Console.Error.WriteLine("FAIL: empty window body cannot initiate native window dragging."); return 1; }
            if (button != 1) { Console.Error.WriteLine("FAIL: an interactive button is consumed by window dragging."); return 1; }
            for (int i = 0; i < 100; i++) main.Refresh(); main.UpdateLayout();
            if (!Descendants(main).Contains(firstDay)) { Console.Error.WriteLine("FAIL: unchanged refreshes rebuild the calendar."); return 1; }
            if (Descendants(main).OfType<TextBlock>().Count(t => t.Text == "Remember") != 1) { Console.Error.WriteLine("FAIL: duplicate Remember branding."); return 1; }
            var editor = new ReminderEditor(main, null, DateTime.Today); editor.Show(); editor.UpdateLayout(); Pump();
            foreach (var input in Descendants(editor).OfType<TextBox>()) if (HitCentre(editor, input) != 1) { Console.Error.WriteLine("FAIL: window dragging consumes a text field."); return 1; }
            editor.Close();
            Console.WriteLine("PASS: native dragging from empty space preserves interactive controls."); return 0;
        }
        finally { host.Dispose(); foreach (Window w in app.Windows.Cast<Window>().ToArray()) w.Close(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
