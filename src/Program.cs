using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Remember
{
    public static class Startup
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static bool Enabled
        {
            get { using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && k.GetValue("Remember") != null; }
        }
        public static void Set(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (enabled) key.SetValue("Remember", "\"" + Assembly.GetExecutingAssembly().Location + "\" --background");
                else key.DeleteValue("Remember", false);
            }
        }
    }

    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            bool test = args.Contains("--data-dir");
            string dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Remember");
            if (test)
            {
                int i = Array.IndexOf(args, "--data-dir");
                if (i + 1 >= args.Length) return 2;
                dataDirectory = Path.GetFullPath(args[i + 1]);
            }
            string identity;
            using (var sha = SHA256.Create()) identity = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(dataDirectory.ToUpperInvariant()))).Replace("-", "").Substring(0, 24);
            bool created;
            using (var mutex = new Mutex(true, @"Local\Remember-" + identity, out created))
            using (var activate = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\Remember-Activate-" + identity))
            {
                if (!created) { if (!args.Contains("--background")) activate.Set(); return 0; }
                var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Remember.Theme.xaml"))
                    application.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(stream));
                AppHost host = null;
                try
                {
                    host = new AppHost(application, new Storage(dataDirectory), test, args.Contains("--background"));
                    var registration = ThreadPool.RegisterWaitForSingleObject(activate, (s, timedOut) => application.Dispatcher.BeginInvoke(new Action(host.ShowMain)), null, -1, false);
                    application.DispatcherUnhandledException += (s, e) =>
                    {
                        if (e.Exception is IOException || e.Exception is UnauthorizedAccessException || e.Exception is ArgumentException)
                        { Ui.Error(host.Main, e.Exception.Message); e.Handled = true; }
                    };
                    application.SessionEnding += (s, e) => host.Exit();
                    host.Start();
                    application.Run();
                    registration.Unregister(null);
                    host.Dispose();
                }
                catch (Exception ex)
                {
                    if (host != null) host.Dispose();
                    Forms.MessageBox.Show(L.T(ex.Message), L.T("Remember — не удалось запустить"), Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Error);
                    return 1;
                }
            }
            return 0;
        }
    }

    public sealed class AppHost : IDisposable
    {
        public AppData Data { get; private set; }
        public Storage Storage { get; private set; }
        public MainWindow Main { get; private set; }
        public bool IsTest { get; private set; }
        public bool IsExiting { get; private set; }
        public long Revision { get; private set; }
        public readonly Dictionary<string, Occurrence> ActiveNotifications = new Dictionary<string, Occurrence>();
        public event Action Changed;
        readonly Application application;
        readonly bool background;
        readonly DispatcherTimer timer;
        readonly Stopwatch startupClock = Stopwatch.StartNew();
        readonly TimeSpan startupGrace;
        Forms.NotifyIcon tray;
        NotificationWindow notification;
        bool disposed;

        public AppHost(Application app, Storage storage, bool test, bool background)
        {
            application = app; Storage = storage; IsTest = test; this.background = background; Data = storage.Load();
            L.Configure(Data.Language); Appearance.Apply(Data.Theme);
            startupGrace = TimeSpan.FromSeconds(background ? Data.StartupDelaySeconds : 3);
            timer = new DispatcherTimer(DispatcherPriority.Background); timer.Tick += (s, e) => Evaluate();
            SystemEvents.PowerModeChanged += OnPowerModeChanged; SystemEvents.TimeChanged += OnTimeChanged;
        }
        public void Start()
        {
            tray = new Forms.NotifyIcon { Text = "Remember", Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location), Visible = true };
            RebuildTray();
            tray.MouseClick += (s, e) => { if (e.Button == Forms.MouseButtons.Left) ShowMain(); };
            if (!background) ShowMain(); Arm();
            if (!string.IsNullOrEmpty(Storage.Warning)) application.Dispatcher.BeginInvoke(new Action(() => { ShowMain(); new MessageDialog(Main, "Восстановление данных", L.T(Storage.Warning)).ShowDialog(); }));
        }
        void RebuildTray()
        {
            var oldMenu = tray.ContextMenuStrip;
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add(L.T("Открыть календарь"), null, (s, e) => ShowMain());
            menu.Items.Add(L.T("Добавить напоминание"), null, (s, e) => { ShowMain(); Edit(null, DateTime.Today); });
            menu.Items.Add(L.T("Настройки"), null, (s, e) => { ShowMain(); OpenSettings(); });
            menu.Items.Add(new Forms.ToolStripSeparator()); menu.Items.Add(L.T("Выйти из Remember"), null, (s, e) => Exit());
            tray.ContextMenuStrip = menu; if (oldMenu != null) oldMenu.Dispose();
        }
        public void OpenSettings()
        {
            var dialog = new SettingsDialog(Main, this);
            if (dialog.ShowDialog() == true) ApplyPreferences();
        }
        public void ApplyPreferences()
        {
            string language = Data.Language == "en" ? "en" : "ru", theme = Data.Theme == "sage" || Data.Theme == "light" ? Data.Theme : "graphite";
            if (language == L.Language && theme == Appearance.Theme) return;
            L.Configure(Data.Language); Appearance.Apply(Data.Theme); if (tray != null) RebuildTray();
            if (Main != null)
            {
                var view = Main.CaptureView(); Main.Close();
                Main = new MainWindow(this, view); application.MainWindow = Main; Main.Show(); Main.Activate();
            }
            foreach (var popup in application.Windows.OfType<NotificationWindow>().ToArray()) popup.CloseForAppearanceChange();
            if (ActiveNotifications.Count > 0) { notification = new NotificationWindow(this); notification.Show(); }
        }
        public void ShowMain()
        {
            if (IsExiting) return;
            if (Main == null) { Main = new MainWindow(this); application.MainWindow = Main; }
            if (!Main.IsVisible) Main.Show();
            if (Main.WindowState == WindowState.Minimized) Main.WindowState = WindowState.Normal;
            Main.Refresh(); Main.Activate();
        }
        public void MainClosed(MainWindow window) { if (Main == window) Main = null; }
        public void Save() { Storage.Save(Data); }
        public bool Change(Action<AppData> mutation, Window owner = null)
        {
            var before = new AppData { Version = Data.Version, StartupDelaySeconds = Data.StartupDelaySeconds, Sound = Data.Sound, Language = Data.Language, Theme = Data.Theme, Reminders = Data.Reminders.Select(r => r.Copy()).ToList() };
            try { mutation(Data); Save(); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { Data = before; Ui.Error(owner ?? Main, L.F("Изменения не сохранены.\n{0}", ex.Message)); return false; }
            Revision++;
            if (Changed != null) Changed();
            return true;
        }
        public void Edit(Reminder reminder, DateTime date)
        {
            var editor = new ReminderEditor(Main, reminder, date);
            if (editor.ShowDialog() != true) return;
            if (Change(data =>
            {
                int index = data.Reminders.FindIndex(r => r.Id == editor.Result.Id);
                if (index < 0) data.Reminders.Add(editor.Result); else data.Reminders[index] = editor.Result;
            })) Evaluate();
        }
        public void Delete(Reminder r)
        {
            new MessageDialog(Main, "Удалить напоминание?", L.F("«{0}» будет удалено вместе с правилом повторения.", r.Title), () =>
            { if (Change(data => data.Reminders.RemoveAll(x => x.Id == r.Id))) Evaluate(); }).ShowDialog();
        }
        public void Toggle(Reminder r)
        { if (Change(data => { var item = data.Reminders.First(x => x.Id == r.Id); item.Enabled = !item.Enabled; })) Evaluate(); }
        public void Acknowledge(Occurrence occurrence)
        {
            if (Change(data => ApplyResolution(data.Reminders.First(r => r.Id == occurrence.Reminder.Id), occurrence, true, DateTime.Now))) Evaluate();
        }
        static void ApplyResolution(Reminder r, Occurrence occurrence, bool acknowledge, DateTime until)
        {
            if (acknowledge)
            {
                if (string.CompareOrdinal(r.AcknowledgedThrough, occurrence.Key) < 0) r.AcknowledgedThrough = occurrence.Key;
                r.SnoozedUntil = null; r.SnoozedOccurrence = null;
            }
            else { r.SnoozedOccurrence = occurrence.Key; r.SnoozedUntil = until; }
        }
        public bool ResolveAll(bool acknowledge, DateTime until)
        {
            var active = ActiveNotifications.Values.ToArray();
            if (active.Length > 0 && !Change(data =>
            {
                foreach (var occurrence in active)
                {
                    var r = data.Reminders.FirstOrDefault(x => x.Id == occurrence.Reminder.Id);
                    if (r != null) ApplyResolution(r, occurrence, acknowledge, until);
                }
            }, notification)) return false;
            ActiveNotifications.Clear(); return true;
        }
        public void NotificationClosed() { notification = null; Arm(); }
        public void PreviewNotification() { new NotificationWindow(this, true).Show(); }

        void Evaluate()
        {
            timer.Stop(); if (IsExiting) return;
            if (startupClock.Elapsed < startupGrace) { Arm(); return; }
            var now = DateTime.Now;
            var due = Data.Reminders.Select(r => Schedule.Pending(r, now)).Where(o => o != null && o.NotifyAt <= now).ToArray();
            ActiveNotifications.Clear(); foreach (var o in due) ActiveNotifications[o.Reminder.Id] = o;
            if (due.Length > 0)
            {
                if (notification == null)
                {
                    notification = new NotificationWindow(this); notification.Show(); notification.Activate();
                    if (Data.Sound) System.Media.SystemSounds.Exclamation.Play();
                }
                else notification.Refresh();
            }
            else if (notification != null) notification.Close();
            if (Main != null && Main.IsVisible) Main.Refresh(); Arm();
        }
        void Arm()
        {
            timer.Stop(); if (IsExiting) return;
            var now = DateTime.Now;
            var next = now.Date.AddDays(1); // Update the calendar at midnight; otherwise sleep until needed.
            if (startupClock.Elapsed < startupGrace) next = now.Add(startupGrace - startupClock.Elapsed);
            else
            {
                foreach (var r in Data.Reminders)
                {
                    var o = Schedule.Pending(r, now);
                    if (o == null || ActiveNotifications.ContainsKey(r.Id)) continue;
                    if (o.NotifyAt < next) next = o.NotifyAt;
                }
            }
            var interval = next - now;
            timer.Interval = interval < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : interval;
            timer.Start();
        }
        void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume && !IsExiting) application.Dispatcher.BeginInvoke(new Action(() => { timer.Stop(); timer.Interval = TimeSpan.FromSeconds(10); timer.Start(); }));
        }
        void OnTimeChanged(object sender, EventArgs e) { if (!IsExiting) application.Dispatcher.BeginInvoke(new Action(Evaluate)); }

        public void Export(Window owner)
        {
            var picker = new SaveFileDialog { Title = L.T("Экспорт напоминаний"), Filter = L.T("Копия Remember (*.json)|*.json"), FileName = "Remember-" + DateTime.Now.ToString("yyyy-MM-dd") + ".json" };
            if (picker.ShowDialog(owner) != true) return;
            try { Storage.Export(Data, picker.FileName); new MessageDialog(owner, "Копия сохранена", picker.FileName).ShowDialog(); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { Ui.Error(owner, ex.Message); }
        }
        public void Import(Window owner)
        {
            var picker = new OpenFileDialog { Title = L.T("Импорт напоминаний"), Filter = L.T("Копия Remember (*.json)|*.json") };
            if (picker.ShowDialog(owner) != true) return;
            try
            {
                var imported = Storage.Read(picker.FileName); int count = 0;
                if (Change(data =>
                {
                    foreach (var r in imported.Reminders)
                        if (!data.Reminders.Any(existing => existing.Id == r.Id)) { data.Reminders.Add(r); count++; }
                }, owner)) { Evaluate(); new MessageDialog(owner, "Импорт завершён", L.F("Добавлено напоминаний: {0}. Существующие записи сохранены.", count)).ShowDialog(); }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is System.Runtime.Serialization.SerializationException) { Ui.Error(owner, L.F("Не удалось прочитать копию. {0}", ex.Message)); }
        }
        public void Exit()
        {
            if (IsExiting) return; IsExiting = true; timer.Stop();
            if (tray != null) tray.Visible = false;
            application.Shutdown();
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true; IsExiting = true; timer.Stop();
            SystemEvents.PowerModeChanged -= OnPowerModeChanged; SystemEvents.TimeChanged -= OnTimeChanged;
            if (tray != null) { var icon = tray.Icon; var menu = tray.ContextMenuStrip; tray.Dispose(); if (icon != null) icon.Dispose(); if (menu != null) menu.Dispose(); }
        }
    }
}
