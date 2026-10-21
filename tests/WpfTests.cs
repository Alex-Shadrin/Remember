using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Remember;

static class WpfTests
{
    static int count;
    static void Check(bool result, string name) { if (!result) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
    static T Field<T>(object obj, string name) { return (T)obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj); }
    static void Evaluate(AppHost host) { typeof(AppHost).GetMethod("Evaluate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(host, null); }
    static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var node in Descendants(VisualTreeHelper.GetChild(root, i))) yield return node;
    }
    static void Click(Window window, string label)
    { Descendants(window).OfType<Button>().First(b => b.Content as string == label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); }
    static void Screenshot(Window window, string path)
    {
        window.UpdateLayout(); var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using (var stream = File.Create(path)) encoder.Save(stream);
    }
    [STAThread]
    static int Main(string[] args)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Remember-wpf-tests-" + Guid.NewGuid().ToString("N"));
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using (var stream = typeof(AppHost).Assembly.GetManifestResourceStream("Remember.Theme.xaml")) app.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(stream));
        AppHost host = null;
        try
        {
            host = new AppHost(app, new Storage(directory), true, false); host.Start();
            Check(host.Main.IsVisible, "calendar opens");
            Check(((SolidColorBrush)host.Main.Background).Color == Color.FromRgb(18, 20, 21), "window uses the dark palette");
            var editor = new ReminderEditor(host.Main, null, DateTime.Today.AddDays(3));
            app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                Field<TextBox>(editor, "title").Text = "Подписка на музыку";
                Field<ComboBox>(editor, "category").SelectedIndex = 1;
                Check(Field<CheckBox>(editor, "repeat").IsChecked == true, "subscription selects monthly recurrence");
                Check(Descendants(editor).OfType<Button>().First(b => b.Content as string == "Сохранить").ActualHeight > 20, "save button is laid out");
                Click(editor, "Сохранить");
            }));
            Check(editor.ShowDialog() == true, "editor saves a valid reminder");
            Check(editor.Result.Repeat == Repeat.Monthly, "editor persists the repetition rule");
            host.Change(d => d.Reminders.Add(editor.Result));
            Check(host.Storage.Load().Reminders.Count == 1, "GUI reminder persisted to disk");
            var edited = new ReminderEditor(host.Main, editor.Result, editor.Result.Date);
            app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                Field<TextBox>(edited, "date").Text = "31.02.2026"; Click(edited, "Сохранить");
                Check(Field<TextBlock>(edited, "error").Text.Length > 0, "invalid date stays in the editor with an error"); edited.Close();
            }));
            edited.ShowDialog();
            host.Change(d => { d.Reminders[0].Date = DateTime.Today; d.Reminders[0].LeadDays = 0; d.Reminders[0].MinuteOfDay = 0; });
            // Allow the ordinary foreground startup grace to elapse without sleeping the UI thread.
            var frame = new DispatcherFrame(); var wait = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.2) }; wait.Tick += (s, e) => { wait.Stop(); frame.Continue = false; }; wait.Start(); Dispatcher.PushFrame(frame);
            Evaluate(host);
            Check(host.ActiveNotifications.Count == 1, "overdue reminder produces an actual popup");
            var popup = app.Windows.OfType<NotificationWindow>().Single(); popup.UpdateLayout();
            Check(popup.Topmost && popup.IsVisible, "reminder is visible above other windows");
            if (args.Length > 0) { Directory.CreateDirectory(args[0]); Screenshot(popup, Path.Combine(args[0], "notification.png")); }
            Click(popup, "Через 10 минут");
            Check(host.Storage.Load().Reminders[0].SnoozedUntil > DateTime.Now.AddMinutes(9), "snooze survives disk round-trip");
            Evaluate(host); Check(host.ActiveNotifications.Count == 0, "snoozed popup does not reopen immediately");
            host.Change(d => d.Reminders[0].SnoozedUntil = DateTime.Now.AddMinutes(-1)); Evaluate(host);
            Click(app.Windows.OfType<NotificationWindow>().Single(), "Помню");
            Check(host.Storage.Load().Reminders[0].AcknowledgedThrough == DateTime.Today.ToString("yyyy-MM-dd"), "acknowledgement survives restart");
            Evaluate(host); Check(host.ActiveNotifications.Count == 0, "acknowledged occurrence does not reopen");
            host.Change(d =>
            {
                d.Reminders.Clear(); d.Reminders.Add(new Reminder { Title = "Подписка на музыку", Category = Category.Subscription, Date = DateTime.Today.AddDays(3), Repeat = Repeat.Monthly, LeadDays = 2 });
                d.Reminders.Add(new Reminder { Title = "День рождения мамы", Category = Category.Birthday, Date = DateTime.Today.AddDays(6), Repeat = Repeat.Yearly, LeadDays = 3 });
                d.Reminders.Add(new Reminder { Title = "Забрать заказ", Category = Category.Personal, Date = DateTime.Today.AddDays(1), LeadDays = 0 });
            });
            host.Main.UpdateLayout();
            int marks = Descendants(host.Main).OfType<System.Windows.Shapes.Ellipse>().Count();
            Check(marks == 3, "calendar displays a mark for each example event (actual: " + marks + ")");
            if (args.Length > 0) Screenshot(host.Main, Path.Combine(args[0], "calendar.png"));
            host.Main.Close(); Check(host.Main == null, "closing calendar releases its window and keeps background host");
            host.ShowMain(); Check(host.Main.IsVisible, "calendar reopens from tray action");
            host.Dispose(); host.Main.Close();
            var startupData = new AppData(); startupData.Reminders.Add(new Reminder { Title = "Startup overdue", Date = DateTime.Today.AddDays(-1), LeadDays = 0 });
            new Storage(directory).Save(startupData);
            host = new AppHost(app, new Storage(directory), true, true); host.Start(); Evaluate(host);
            Check(host.Main == null && host.ActiveNotifications.Count == 0, "startup is silent during its two-minute grace period");
            Console.WriteLine("All " + count + " WPF integration checks passed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (host != null) host.Dispose(); foreach (Window w in app.Windows.Cast<Window>().ToArray()) w.Close(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
