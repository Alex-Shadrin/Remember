using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Remember
{
    public sealed class MessageDialog : Window
    {
        public MessageDialog(Window owner, string title, string message, Action confirmed = null)
        {
            Title = "Remember — " + title; Ui.Dialog(this, owner, 420, 260);
            var body = Ui.Stack(); var heading = Ui.Label(title, 18); heading.FontWeight = FontWeights.SemiBold;
            body.Children.Add(heading);
            var text = Ui.Label(message, 13, Ui.Secondary); text.TextWrapping = TextWrapping.Wrap; text.Margin = new Thickness(0, 14, 0, 20); body.Children.Add(text);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            if (confirmed != null) { var cancel = Ui.Button("Отмена", Close, "Quiet"); cancel.Margin = new Thickness(0, 0, 8, 0); actions.Children.Add(cancel); }
            actions.Children.Add(Ui.Button(confirmed == null ? "Понятно" : "Удалить", () => { if (confirmed != null) confirmed(); Close(); }, "Primary"));
            body.Children.Add(actions); Content = Ui.DialogLayout(this, "Remember", body);
        }
    }

    public sealed class JumpDialog : Window
    {
        public DateTime Selected;
        public JumpDialog(Window owner, DateTime current)
        {
            Title = "Перейти к месяцу"; Ui.Dialog(this, owner, 350, 265); Selected = current;
            var body = Ui.Stack(); body.Children.Add(Ui.Label("Перейти к месяцу", 20));
            var row = Ui.Columns(new GridLength(1, GridUnitType.Star), new GridLength(12), new GridLength(94)); row.Margin = new Thickness(0, 20, 0, 3);
            var month = new ComboBox(); for (int i = 1; i <= 12; i++) month.Items.Add(Ui.Ru.DateTimeFormat.GetMonthName(i)); month.SelectedIndex = current.Month - 1;
            var year = new TextBox { Text = current.Year.ToString(), MaxLength = 4 };
            Ui.Put(row, Ui.Field("Месяц", month), 0); Ui.Put(row, Ui.Field("Год", year), 2); body.Children.Add(row);
            var error = Ui.Label("", 11, Ui.Brush("#FF7373")); body.Children.Add(error);
            var go = Ui.Button("Перейти", () =>
            {
                int y; if (!int.TryParse(year.Text, out y) || y < 1900 || y > 9998) { error.Text = "Год: от 1900 до 9998"; return; }
                Selected = new DateTime(y, month.SelectedIndex + 1, 1); DialogResult = true;
            }, "Primary"); go.IsDefault = true; body.Children.Add(go); Content = Ui.DialogLayout(this, "Быстрый переход", body);
        }
    }

    public sealed class ReminderEditor : Window
    {
        public Reminder Result;
        readonly TextBox title, date, time, days, notes;
        readonly ComboBox category, frequency;
        readonly CheckBox repeat, enabled;
        readonly TextBlock error;
        readonly Reminder original;

        public ReminderEditor(Window owner, Reminder reminder, DateTime selectedDate)
        {
            original = reminder; var r = reminder ?? new Reminder { Date = selectedDate.Date };
            Title = reminder == null ? "Новое напоминание" : "Изменить напоминание";
            Ui.Dialog(this, owner, 440, 710);
            var body = Ui.Stack(); var h = Ui.Label(Title, 22); h.FontWeight = FontWeights.SemiBold; h.Margin = new Thickness(0, 0, 0, 20); body.Children.Add(h);
            title = new TextBox { Text = r.Title, MaxLength = 160 };
            body.Children.Add(Ui.Field("Название", title));
            category = new ComboBox(); foreach (Category c in Enum.GetValues(typeof(Category))) category.Items.Add(Schedule.CategoryText(c)); category.SelectedIndex = (int)r.Category;
            body.Children.Add(Ui.Field("Категория", category));
            var when = Ui.Columns(new GridLength(1, GridUnitType.Star), new GridLength(12), new GridLength(106));
            date = new TextBox { Text = r.Date.ToString("dd.MM.yyyy"), MaxLength = 10 };
            time = new TextBox { Text = TimeSpan.FromMinutes(r.MinuteOfDay).ToString(@"hh\:mm"), MaxLength = 5 };
            Ui.Put(when, Ui.Field("Дата события", date), 0); Ui.Put(when, Ui.Field("Время сигнала", time), 2); body.Children.Add(when);
            days = new TextBox { Text = r.LeadDays.ToString(), MaxLength = 3, Width = 65, HorizontalAlignment = HorizontalAlignment.Left };
            var lead = Ui.Columns(new GridLength(77), new GridLength(1, GridUnitType.Star)); Ui.Put(lead, days, 0); Ui.Put(lead, Ui.Label("дн. до события · 0 — в этот день", 12, Ui.Secondary), 1);
            body.Children.Add(Ui.Field("Напомнить заранее", lead));
            var repeats = Ui.Columns(new GridLength(1, GridUnitType.Star), new GridLength(175));
            repeat = new CheckBox { Content = "Повторять", IsChecked = r.Repeat != Repeat.Once, VerticalAlignment = VerticalAlignment.Center };
            frequency = new ComboBox(); frequency.Items.Add("Каждый месяц"); frequency.Items.Add("Каждый год"); frequency.Items.Add("Каждую неделю");
            frequency.SelectedIndex = r.Repeat == Repeat.Yearly ? 1 : r.Repeat == Repeat.Weekly ? 2 : 0;
            frequency.IsEnabled = repeat.IsChecked == true;
            repeat.Checked += (s, e) => frequency.IsEnabled = true; repeat.Unchecked += (s, e) => frequency.IsEnabled = false;
            Ui.Put(repeats, repeat, 0); Ui.Put(repeats, frequency, 1); repeats.Margin = new Thickness(0, 0, 0, 16); body.Children.Add(repeats);
            category.SelectionChanged += (s, e) =>
            {
                if (original != null) return;
                if (category.SelectedIndex == 1 || category.SelectedIndex == 2) { repeat.IsChecked = true; frequency.SelectedIndex = category.SelectedIndex == 1 ? 0 : 1; }
            };
            notes = new TextBox { Text = r.Notes, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 65, MaxLength = 2000, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            body.Children.Add(Ui.Field("Заметка · необязательно", notes));
            enabled = new CheckBox { Content = "Напоминание включено", IsChecked = r.Enabled }; body.Children.Add(enabled);
            error = Ui.Label("", 11, Ui.Brush("#FF7373")); error.TextWrapping = TextWrapping.Wrap; error.MinHeight = 24; error.Margin = new Thickness(0, 6, 0, 4);
            var buttons = Ui.Columns(new GridLength(1, GridUnitType.Star), new GridLength(10), new GridLength(1, GridUnitType.Star));
            Ui.Put(buttons, Ui.Button("Отмена", Close), 0); var save = Ui.Button("Сохранить", Save, "Primary"); save.IsDefault = true; Ui.Put(buttons, save, 2);
            var layout = new DockPanel(); var footer = Ui.Stack(); footer.Children.Add(error); footer.Children.Add(buttons); DockPanel.SetDock(footer, Dock.Bottom); layout.Children.Add(footer);
            layout.Children.Add(new ScrollViewer { Content = body }); Content = Ui.DialogLayout(this, "Remember", layout);
            Loaded += (s, e) => { title.Focus(); if (original != null) title.SelectAll(); };
        }
        void Save()
        {
            DateTime day; TimeSpan clock; int lead;
            if (!DateTime.TryParseExact(date.Text.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out day)) { error.Text = "Дата: ДД.ММ.ГГГГ, например 25.12.2026"; date.Focus(); return; }
            if (!TimeSpan.TryParseExact(time.Text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out clock) || clock.TotalHours >= 24) { error.Text = "Время: ЧЧ:ММ, например 09:00"; time.Focus(); return; }
            if (!int.TryParse(days.Text, out lead) || lead < 0 || lead > 365) { error.Text = "Укажите от 0 до 365 дней."; days.Focus(); return; }
            var r = original == null ? new Reminder() : original.Copy();
            var rule = repeat.IsChecked != true ? Repeat.Once : frequency.SelectedIndex == 1 ? Repeat.Yearly : frequency.SelectedIndex == 2 ? Repeat.Weekly : Repeat.Monthly;
            // A title or note edit must not resurrect a completed occurrence.
            bool rescheduled = original != null && (r.Date.Date != day.Date || r.Repeat != rule || r.LeadDays != lead || r.MinuteOfDay != (int)clock.TotalMinutes || (!r.Enabled && enabled.IsChecked == true));
            r.Title = title.Text.Trim(); r.Date = day; r.MinuteOfDay = (int)clock.TotalMinutes; r.LeadDays = lead; r.Category = (Category)category.SelectedIndex;
            r.Repeat = rule; r.Notes = notes.Text.Trim(); r.Enabled = enabled.IsChecked == true;
            if (rescheduled) { r.AcknowledgedThrough = null; r.SnoozedUntil = null; r.SnoozedOccurrence = null; }
            try { r.Validate(); } catch (ArgumentException ex) { error.Text = ex.Message; return; }
            Result = r; DialogResult = true;
        }
    }

    public sealed class SettingsDialog : Window
    {
        public SettingsDialog(Window owner, AppHost host)
        {
            Title = "Настройки Remember"; Ui.Dialog(this, owner, 450, 590);
            var body = Ui.Stack(); var h = Ui.Label("Всегда под рукой", 22); h.FontWeight = FontWeights.SemiBold; h.Margin = new Thickness(0, 0, 0, 12); body.Children.Add(h);
            var info = Ui.Label("Закрытие окна скрывает календарь в трей.\nПолный выход — через меню значка Remember.", 13, Ui.Secondary); info.LineHeight = 22; info.Margin = new Thickness(0, 0, 0, 22); body.Children.Add(info);
            var startup = new CheckBox { Content = "Запускать вместе с Windows", IsChecked = Startup.Enabled, IsEnabled = !host.IsTest };
            startup.Margin = new Thickness(0, 0, 0, 18); body.Children.Add(startup);
            var delay = new TextBox { Text = (host.Data.StartupDelaySeconds / 60).ToString(), MaxLength = 2 };
            body.Children.Add(Ui.Field("Задержка после входа в Windows, минут", delay, "Окно сигнала появится после этой задержки. От 0 до 60 минут."));
            var sound = new CheckBox { Content = "Звук вместе с окном напоминания", IsChecked = host.Data.Sound }; sound.Margin = new Thickness(0, 0, 0, 22); body.Children.Add(sound);
            var tools = Ui.Columns(new GridLength(1, GridUnitType.Star), new GridLength(10), new GridLength(1, GridUnitType.Star));
            Ui.Put(tools, Ui.Button("Экспорт копии", () => host.Export(this)), 0); Ui.Put(tools, Ui.Button("Импорт копии", () => host.Import(this)), 2); tools.Margin = new Thickness(0, 0, 0, 15); body.Children.Add(tools);
            body.Children.Add(Ui.Button("Показать пробное напоминание", () => host.PreviewNotification()));
            var privacy = Ui.Label("Всё хранится на этом компьютере.\nБез аккаунтов, интернета и телеметрии.", 12, Ui.Muted); privacy.LineHeight = 20; privacy.Margin = new Thickness(0, 20, 0, 15); body.Children.Add(privacy);
            var error = Ui.Label("", 11, Ui.Brush("#FF7373")); error.TextWrapping = TextWrapping.Wrap; error.MinHeight = 30; body.Children.Add(error);
            body.Children.Add(Ui.Button("Сохранить настройки", () =>
            {
                int minutes; if (!int.TryParse(delay.Text, out minutes) || minutes < 0 || minutes > 60) { error.Text = "Укажите от 0 до 60 минут."; return; }
                var oldDelay = host.Data.StartupDelaySeconds; var oldSound = host.Data.Sound; var oldStartup = Startup.Enabled;
                try
                {
                    if (!host.IsTest) Startup.Set(startup.IsChecked == true);
                    host.Data.StartupDelaySeconds = minutes * 60; host.Data.Sound = sound.IsChecked == true;
                    host.Save(); Close();
                }
                catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
                {
                    host.Data.StartupDelaySeconds = oldDelay; host.Data.Sound = oldSound;
                    try { if (!host.IsTest) Startup.Set(oldStartup); } catch { }
                    error.Text = "Не удалось сохранить: " + ex.Message;
                }
            }, "Primary"));
            Content = Ui.DialogLayout(this, "Настройки", new ScrollViewer { Content = body });
        }
    }

    public sealed class NotificationWindow : Window
    {
        readonly AppHost host;
        readonly StackPanel items;
        readonly TextBlock count;
        readonly bool preview;
        bool resolved;
        public NotificationWindow(AppHost host, bool preview = false)
        {
            this.host = host; this.preview = preview;
            Title = preview ? "Remember — пробное напоминание" : "Remember — пора вспомнить";
            Width = 450; Height = 430; MinWidth = 390; MinHeight = 350; Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen; Ui.Chrome(this);
            var root = new DockPanel(); var header = Ui.TitleBar(this, preview ? "Пробное напоминание" : "Remember · напоминание"); DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
            var body = new DockPanel { Margin = new Thickness(24, 14, 24, 24) };
            var head = Ui.Stack(); var bell = Ui.Icon("\uEA8F", 28, Ui.Accent); bell.Margin = new Thickness(0, 0, 0, 13); head.Children.Add(bell);
            head.Children.Add(Ui.Label("Пора вспомнить", 25)); count = Ui.Label("", 12, Ui.Secondary); count.Margin = new Thickness(0, 6, 0, 17); head.Children.Add(count); DockPanel.SetDock(head, Dock.Top); body.Children.Add(head);
            var footer = Ui.Stack(); footer.Margin = new Thickness(0, 16, 0, 0);
            var buttons = Ui.Columns(new GridLength(1, GridUnitType.Star), new GridLength(10), new GridLength(1, GridUnitType.Star));
            Ui.Put(buttons, Ui.Button("Через 10 минут", () => Resolve(false)), 0); Ui.Put(buttons, Ui.Button("Помню", () => Resolve(true), "Primary"), 2); footer.Children.Add(buttons);
            var tomorrow = Ui.Button("Напомнить завтра", () => { if (!preview && !host.ResolveAll(false, DateTime.Now.AddDays(1))) return; resolved = true; Close(); }, "Quiet"); tomorrow.Margin = new Thickness(0, 7, 0, 0); footer.Children.Add(tomorrow);
            DockPanel.SetDock(footer, Dock.Bottom); body.Children.Add(footer);
            items = Ui.Stack(); body.Children.Add(new ScrollViewer { Content = items }); root.Children.Add(body); Content = root;
            Closing += (s, e) => { if (!resolved && !preview && !host.IsExiting && !host.ResolveAll(false, DateTime.Now.AddMinutes(10))) e.Cancel = true; };
            Closed += (s, e) => { if (!preview) host.NotificationClosed(); };
            Refresh();
        }
        void Resolve(bool acknowledge)
        {
            if (!preview && !host.ResolveAll(acknowledge, DateTime.Now.AddMinutes(10))) return;
            resolved = true; Close();
        }
        public void Refresh()
        {
            items.Children.Clear();
            var occurrences = preview ? new[] { Occurrence.For(new Reminder { Title = "Продлить подписку", Notes = "Так будет выглядеть ваше напоминание", Category = Category.Subscription }, DateTime.Today.AddDays(2)) } : host.ActiveNotifications.Values.ToArray();
            count.Text = preview ? "Пример · данные не изменяются" : "Важные даты: " + occurrences.Length;
            foreach (var o in occurrences)
            {
                var stack = Ui.Stack(); var title = Ui.Label(o.Reminder.Title, 16); title.TextWrapping = TextWrapping.Wrap; title.FontWeight = FontWeights.SemiBold; stack.Children.Add(title);
                var date = Ui.Label(o.Date.ToString("d MMMM yyyy", Ui.Ru) + "  ·  " + Ui.DayText(o.Date), 12, Ui.Color(o.Reminder.Category)); date.Margin = new Thickness(0, 6, 0, 0); stack.Children.Add(date);
                if (!string.IsNullOrWhiteSpace(o.Reminder.Notes)) { var note = Ui.Label(o.Reminder.Notes, 12, Ui.Secondary); note.TextWrapping = TextWrapping.Wrap; note.Margin = new Thickness(0, 8, 0, 0); stack.Children.Add(note); }
                var card = Ui.Panel(stack, 14); card.Margin = new Thickness(0, 0, 0, 8); items.Children.Add(card);
            }
        }
    }
}
