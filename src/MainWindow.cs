using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Remember
{
    public sealed class MainWindow : Window
    {
        readonly AppHost host;
        DateTime month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTime selected = DateTime.Today;
        bool selectedOnly;
        readonly Button monthTitle, upcomingTab, dayTab;
        readonly UniformGrid calendar;
        readonly StackPanel events;
        readonly TextBlock dateHeading, listHeading, countLabel, status;

        public MainWindow(AppHost host)
        {
            this.host = host;
            Title = "Remember"; Width = 810; Height = 680; MinWidth = 770; MinHeight = 660;
            WindowStartupLocation = WindowStartupLocation.CenterScreen; Ui.Chrome(this);
            var root = new DockPanel();
            var chrome = Ui.TitleBar(this, "Remember", true); DockPanel.SetDock(chrome, Dock.Top); root.Children.Add(chrome);
            var footer = Ui.Columns(new GridLength(1, GridUnitType.Star), new GridLength(36)); footer.Margin = new Thickness(25, 8, 20, 13);
            status = Ui.Label("●  Работает в фоне", 11, Ui.Brush("#6FD08C")); Ui.Put(footer, status, 0);
            Ui.Put(footer, Ui.IconButton("\uE713", "Настройки", () => new SettingsDialog(this, host).ShowDialog()), 1);
            DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
            var body = new DockPanel { Margin = new Thickness(24, 10, 24, 0) };
            var header = Ui.Columns(new GridLength(46), new GridLength(1, GridUnitType.Star), new GridLength(158)); header.Margin = new Thickness(0, 0, 0, 24);
            var mark = new Border { Width = 36, Height = 36, CornerRadius = new CornerRadius(11), Background = Ui.Brush("#24354B"), Child = Ui.Icon("\uE787", 20, Ui.Accent), HorizontalAlignment = HorizontalAlignment.Left };
            ((TextBlock)mark.Child).HorizontalAlignment = HorizontalAlignment.Center; Ui.Put(header, mark, 0);
            var brand = Ui.Stack(); var name = Ui.Label("Remember", 25); name.FontWeight = FontWeights.SemiBold; brand.Children.Add(name);
            dateHeading = Ui.Label("", 12, Ui.Secondary); dateHeading.Margin = new Thickness(0, 4, 0, 0); brand.Children.Add(dateHeading); Ui.Put(header, brand, 1);
            var add = Ui.Button("+  Напоминание", () => host.Edit(null, selected), "Primary"); add.VerticalAlignment = VerticalAlignment.Center; Ui.Put(header, add, 2);
            DockPanel.SetDock(header, Dock.Top); body.Children.Add(header);

            var content = Ui.Columns(new GridLength(338), new GridLength(22), new GridLength(1, GridUnitType.Star));
            var left = new DockPanel();
            var leftFoot = Ui.Stack(); leftFoot.Margin = new Thickness(0, 15, 0, 0);
            var quickAdd = Ui.Button("Добавить на выбранный день", () => host.Edit(null, selected)); leftFoot.Children.Add(quickAdd);
            var backToToday = Ui.Button("Сегодня", () => { selected = DateTime.Today; month = new DateTime(selected.Year, selected.Month, 1); selectedOnly = false; Refresh(); }, "Quiet"); backToToday.Foreground = Ui.Accent; backToToday.Margin = new Thickness(0, 5, 0, 0); leftFoot.Children.Add(backToToday);
            DockPanel.SetDock(leftFoot, Dock.Bottom); left.Children.Add(leftFoot);
            var calendarBody = Ui.Stack();
            var navigation = Ui.Columns(new GridLength(1, GridUnitType.Star), new GridLength(32), new GridLength(32)); navigation.Margin = new Thickness(0, 0, 0, 14);
            monthTitle = Ui.Button("", Jump, "Quiet"); monthTitle.HorizontalContentAlignment = HorizontalAlignment.Left; monthTitle.Padding = new Thickness(5, 7, 0, 7); monthTitle.FontWeight = FontWeights.SemiBold; monthTitle.ToolTip = "Выбрать месяц и год";
            Ui.Put(navigation, monthTitle, 0); Ui.Put(navigation, Ui.IconButton("\uE70E", "Предыдущий месяц", () => MoveMonth(-1)), 1); Ui.Put(navigation, Ui.IconButton("\uE70D", "Следующий месяц", () => MoveMonth(1)), 2); calendarBody.Children.Add(navigation);
            var weekdays = new UniformGrid { Columns = 7, Margin = new Thickness(0, 0, 0, 6) };
            foreach (var day in new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" }) { var t = Ui.Label(day, 11, Ui.Secondary); t.HorizontalAlignment = HorizontalAlignment.Center; t.Height = 22; weekdays.Children.Add(t); }
            calendarBody.Children.Add(weekdays);
            calendar = new UniformGrid { Columns = 7, Rows = 6, Height = 252 }; calendarBody.Children.Add(calendar);
            var legend = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            foreach (Category c in Enum.GetValues(typeof(Category))) { var t = Ui.Label("● " + (c == Category.Birthday ? "Дни рождения" : c == Category.Subscription ? "Подписки" : "Личное"), 9, Ui.Color(c)); t.Margin = new Thickness(5, 0, 5, 0); legend.Children.Add(t); }
            calendarBody.Children.Add(legend); left.Children.Add(Ui.Panel(calendarBody, 14)); Ui.Put(content, left, 0);

            var right = new DockPanel(); var rightTop = Ui.Stack();
            var tabs = Ui.Columns(new GridLength(1, GridUnitType.Star), new GridLength(5), new GridLength(1, GridUnitType.Star)); tabs.Margin = new Thickness(0, 0, 0, 21);
            upcomingTab = Ui.Button("Ближайшие", () => { selectedOnly = false; Refresh(); }, "Quiet"); dayTab = Ui.Button("Выбранный день", () => { selectedOnly = true; Refresh(); }, "Quiet");
            upcomingTab.FontSize = dayTab.FontSize = 12; upcomingTab.Padding = dayTab.Padding = new Thickness(8, 9, 8, 9);
            Ui.Put(tabs, upcomingTab, 0); Ui.Put(tabs, dayTab, 2); rightTop.Children.Add(Ui.Panel(tabs, 4, Ui.Brush("#17191B"))); tabs.Margin = new Thickness(0);
            var listHeader = Ui.Columns(new GridLength(1, GridUnitType.Star), new GridLength(35)); listHeader.Margin = new Thickness(0, 20, 0, 13);
            listHeading = Ui.Label("", 16); listHeading.FontWeight = FontWeights.SemiBold; countLabel = Ui.Label("", 12, Ui.Muted); countLabel.HorizontalAlignment = HorizontalAlignment.Right; Ui.Put(listHeader, listHeading, 0); Ui.Put(listHeader, countLabel, 1); rightTop.Children.Add(listHeader);
            DockPanel.SetDock(rightTop, Dock.Top); right.Children.Add(rightTop);
            events = Ui.Stack(); right.Children.Add(new ScrollViewer { Content = events }); Ui.Put(content, right, 2);
            body.Children.Add(content); root.Children.Add(body); Content = root;
            Closed += (s, e) => { host.Changed -= Refresh; host.MainClosed(this); };
            host.Changed += Refresh;
            PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control) { host.Edit(null, selected); e.Handled = true; } };
            Refresh();
        }
        void MoveMonth(int by)
        {
            if ((month.Year <= 1900 && month.Month == 1 && by < 0) || (month.Year >= 9998 && month.Month == 12 && by > 0)) return;
            month = month.AddMonths(by); Refresh();
        }
        void Jump()
        { var jump = new JumpDialog(this, month); if (jump.ShowDialog() == true) { month = jump.Selected; Refresh(); } }

        public void Refresh()
        {
            if (calendar == null) return;
            dateHeading.Text = "Всё важное — вовремя";
            monthTitle.Content = Ui.Ru.TextInfo.ToTitleCase(month.ToString("MMMM yyyy", Ui.Ru)) + "  ⌄";
            calendar.Children.Clear();
            int offset = ((int)month.DayOfWeek + 6) % 7;
            var start = month.AddDays(-offset);
            var marks = host.Data.Reminders.Where(r => r.Enabled).SelectMany(r => Schedule.Between(r, start, start.AddDays(41))).Where(o => !o.Acknowledged).GroupBy(o => o.Date).ToDictionary(g => g.Key, g => g.Select(o => o.Reminder.Category).Distinct().Take(3).ToArray());
            for (int i = 0; i < 42; i++)
            {
                var date = start.AddDays(i); bool chosen = date == selected, today = date == DateTime.Today;
                var day = Ui.Button("", () => { selected = date; month = new DateTime(date.Year, date.Month, 1); selectedOnly = true; Refresh(); }, "Quiet");
                day.Padding = new Thickness(0); day.ToolTip = date.ToString("D", Ui.Ru); day.Margin = new Thickness(2);
                System.Windows.Automation.AutomationProperties.SetName(day, date.ToString("D", Ui.Ru));
                var number = Ui.Label(date.Day.ToString(), 13, chosen ? Ui.Brush("#111923") : date.Month != month.Month ? Ui.Muted : Ui.Text); number.HorizontalAlignment = HorizontalAlignment.Center;
                var circle = new Border { Width = 32, Height = 32, CornerRadius = new CornerRadius(16), Background = chosen ? Ui.Accent : Brushes.Transparent, BorderThickness = new Thickness(today && !chosen ? 1 : 0), BorderBrush = Ui.Accent, Child = number, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top };
                var dayContent = new Grid { Width = 34, Height = 38 }; dayContent.Children.Add(circle);
                Category[] categories;
                if (marks.TryGetValue(date, out categories))
                {
                    var dots = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom };
                    foreach (var c in categories) dots.Children.Add(new Ellipse { Width = 4, Height = 4, Fill = Ui.Color(c), Margin = new Thickness(1, 0, 1, 0) });
                    dayContent.Children.Add(dots);
                }
                day.Content = dayContent; calendar.Children.Add(day);
            }
            upcomingTab.Background = !selectedOnly ? Ui.Raised : Brushes.Transparent; upcomingTab.Foreground = !selectedOnly ? Ui.Text : Ui.Secondary;
            dayTab.Background = selectedOnly ? Ui.Raised : Brushes.Transparent; dayTab.Foreground = selectedOnly ? Ui.Text : Ui.Secondary;
            listHeading.Text = selectedOnly ? selected.ToString("d MMMM", Ui.Ru) : "На очереди";
            Occurrence[] list;
            if (selectedOnly)
                list = host.Data.Reminders.SelectMany(r => Schedule.Between(r, selected, selected)).OrderBy(o => o.Reminder.MinuteOfDay).ToArray();
            else
                list = host.Data.Reminders.Select(r => Schedule.Pending(r, DateTime.Now)).Where(o => o != null).OrderBy(o => o.Date).ThenBy(o => o.Reminder.MinuteOfDay).ToArray();
            countLabel.Text = list.Length.ToString(); events.Children.Clear();
            if (list.Length == 0)
            {
                var empty = Ui.Stack(); empty.Margin = new Thickness(12, 48, 12, 0);
                var icon = Ui.Icon("\uE73E", 34, Ui.Muted); icon.HorizontalAlignment = HorizontalAlignment.Center; empty.Children.Add(icon);
                var title = Ui.Label(selectedOnly ? "Этот день свободен" : "Пока всё спокойно", 17); title.HorizontalAlignment = HorizontalAlignment.Center; title.Margin = new Thickness(0, 18, 0, 9); empty.Children.Add(title);
                var help = Ui.Label("Подписки, дни рождения и планы —\nRemember напомнит о них заранее.", 12, Ui.Secondary); help.TextAlignment = TextAlignment.Center; help.LineHeight = 20; help.HorizontalAlignment = HorizontalAlignment.Center; empty.Children.Add(help);
                var add = Ui.Button("+  Добавить напоминание", () => host.Edit(null, selected), "Quiet"); add.Foreground = Ui.Accent; add.Margin = new Thickness(0, 18, 0, 0); empty.Children.Add(add); events.Children.Add(empty);
            }
            else foreach (var o in list) events.Children.Add(EventCard(o));
            // Keep paused/completed records reachable, even outside their calendar month.
            if (!selectedOnly)
            {
                var other = host.Data.Reminders.Where(r => !r.Enabled || (r.Repeat == Repeat.Once && Occurrence.For(r, r.Date).Acknowledged)).ToArray();
                if (other.Length > 0)
                {
                    var expand = new Expander { Header = "Завершённые и на паузе · " + other.Length, Foreground = Ui.Secondary, Margin = new Thickness(0, 18, 0, 0) };
                    var past = Ui.Stack(); foreach (var r in other) past.Children.Add(EventCard(Occurrence.For(r, r.Date))); expand.Content = past; events.Children.Add(expand);
                }
            }
            status.Text = "●  Работает в фоне  ·  " + host.Data.Reminders.Count(r => r.Enabled && (r.Repeat != Repeat.Once || !Occurrence.For(r, r.Date).Acknowledged)) + " активных";
        }

        FrameworkElement EventCard(Occurrence o)
        {
            var r = o.Reminder;
            var content = Ui.Stack();
            var top = Ui.Columns(new GridLength(1, GridUnitType.Star), new GridLength(29));
            var tag = Ui.Label(Schedule.CategoryText(r.Category).ToUpper(Ui.Ru), 9, Ui.Color(r.Category)); tag.FontWeight = FontWeights.SemiBold; Ui.Put(top, tag, 0);
            var more = Ui.IconButton("\uE712", "Действия: " + r.Title, () => { }); more.Width = 27; more.Height = 22;
            var menu = new ContextMenu();
            Action<string, Action> item = (name, action) => { var mi = new MenuItem { Header = name }; mi.Click += (s, e) => action(); menu.Items.Add(mi); };
            item("Изменить", () => host.Edit(r, o.Date));
            if (r.Enabled && !o.Acknowledged) item("Помню — отметить выполненным", () => host.Acknowledge(o));
            item(r.Enabled ? "Поставить на паузу" : "Включить", () => host.Toggle(r));
            menu.Items.Add(new Separator()); item("Удалить", () => host.Delete(r));
            more.Click += (s, e) => { menu.PlacementTarget = more; menu.IsOpen = true; e.Handled = true; }; Ui.Put(top, more, 1); content.Children.Add(top);
            var title = Ui.Label(r.Title, 15); title.FontWeight = FontWeights.SemiBold; title.TextWrapping = TextWrapping.Wrap; title.Margin = new Thickness(0, 5, 0, 8); content.Children.Add(title);
            var when = Ui.Label(o.Date.ToString("d MMM", Ui.Ru) + (o.Date.Year != DateTime.Today.Year ? " " + o.Date.Year : "") + "  ·  " + (!r.Enabled ? "На паузе" : o.Acknowledged ? "Выполнено" : Ui.DayText(o.Date)), 12, Ui.Secondary); content.Children.Add(when);
            var rule = Ui.Label(Schedule.RepeatText(r.Repeat) + "  ·  " + (r.LeadDays == 0 ? "В этот день" : "За " + r.LeadDays + " дн.") + ", " + TimeSpan.FromMinutes(r.MinuteOfDay).ToString(@"hh\:mm"), 10, Ui.Muted); rule.Margin = new Thickness(0, 7, 0, 0); content.Children.Add(rule);
            var border = Ui.Panel(content, 14); border.Margin = new Thickness(0, 0, 3, 10);
            if (!r.Enabled || o.Acknowledged) border.Opacity = 0.58;
            border.MouseLeftButtonUp += (s, e) => { if (!(e.OriginalSource is Button)) host.Edit(r, o.Date); };
            border.Cursor = Cursors.Hand; border.ToolTip = "Нажмите, чтобы изменить";
            return border;
        }
    }
}
