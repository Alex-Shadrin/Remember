using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Remember
{
    public static class L
    {
        public static string Language { get; private set; } = "ru";
        public static CultureInfo Culture { get { return CultureInfo.GetCultureInfo(Language == "en" ? "en-GB" : "ru-RU"); } }
        public static string DatePattern { get { return Language == "en" ? "dd/MM/yyyy" : "dd.MM.yyyy"; } }
        public static void Configure(string language) { Language = language == "en" ? "en" : "ru"; }
        public static string T(string text) { string translated; return Language == "en" && text != null && English.TryGetValue(text, out translated) ? translated : text; }
        public static string F(string text, params object[] args) { return string.Format(Culture, T(text), args); }
        static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            {"Свернуть", "Minimize"}, {"Скрыть в трей", "Hide to tray"}, {"Закрыть", "Close"}, {"Настройки", "Settings"},
            {"+  Напоминание", "+  Reminder"}, {"Добавить на выбранный день", "Add to selected day"}, {"Сегодня", "Today"}, {"Завтра", "Tomorrow"},
            {"Выбрать месяц и год", "Choose month and year"}, {"Предыдущий месяц", "Previous month"}, {"Следующий месяц", "Next month"},
            {"Пн", "Mon"}, {"Вт", "Tue"}, {"Ср", "Wed"}, {"Чт", "Thu"}, {"Пт", "Fri"}, {"Сб", "Sat"}, {"Вс", "Sun"},
            {"Личное", "Personal"}, {"Подписки", "Subscriptions"}, {"Дни рождения", "Birthdays"}, {"Подписка", "Subscription"}, {"День рождения", "Birthday"},
            {"Ближайшие", "Upcoming"}, {"Выбранный день", "Selected day"}, {"На очереди", "Coming up"}, {"Этот день свободен", "This day is free"},
            {"Пока всё спокойно", "All clear for now"}, {"Подписки, дни рождения и планы —\nRemember напомнит о них заранее.", "Subscriptions, birthdays and plans —\nRemember will remind you in time."},
            {"+  Добавить напоминание", "+  Add a reminder"}, {"Завершённые и на паузе · {0}", "Completed and paused · {0}"},
            {"Действия: {0}", "Actions: {0}"}, {"Изменить", "Edit"}, {"Помню — отметить выполненным", "Got it — mark as done"},
            {"Поставить на паузу", "Pause"}, {"Включить", "Enable"}, {"Удалить", "Delete"}, {"На паузе", "Paused"}, {"Выполнено", "Done"},
            {"Каждый месяц", "Monthly"}, {"Каждый год", "Yearly"}, {"Каждую неделю", "Weekly"}, {"Один раз", "Once"}, {"В этот день", "On the day"},
            {"За {0} дн.", "{0} days before"}, {"Через {0} дн.", "In {0} days"}, {"Просрочено на {0} дн.", "{0} days overdue"},
            {"Нажмите, чтобы изменить", "Click to edit"}, {"Не удалось выполнить действие", "Could not complete the action"}, {"Понятно", "OK"}, {"Отмена", "Cancel"},
            {"Перейти к месяцу", "Go to month"}, {"Месяц", "Month"}, {"Год", "Year"}, {"Год: от 1900 до 9998", "Year: 1900 to 9998"}, {"Перейти", "Go"}, {"Быстрый переход", "Quick navigation"},
            {"Новое напоминание", "New reminder"}, {"Изменить напоминание", "Edit reminder"}, {"Название", "Title"}, {"Категория", "Category"},
            {"Дата события", "Event date"}, {"Время сигнала", "Reminder time"}, {"дн. до события · 0 — в этот день", "days before · 0 means on the day"},
            {"Напомнить заранее", "Advance warning"}, {"Повторять", "Repeat"}, {"Заметка · необязательно", "Note · optional"}, {"Напоминание включено", "Reminder enabled"},
            {"Сохранить", "Save"}, {"Дата: ДД.ММ.ГГГГ, например 25.12.2026", "Date: DD/MM/YYYY, for example 25/12/2026"},
            {"Время: ЧЧ:ММ, например 09:00", "Time: HH:MM, for example 09:00"}, {"Укажите от 0 до 365 дней.", "Enter 0 to 365 days."},
            {"Название должно содержать от 1 до 160 символов.", "The title must contain 1 to 160 characters."}, {"Укажите год от 1900 до 9998.", "Enter a year from 1900 to 9998."},
            {"Проверьте время и число дней (от 0 до 365).", "Check the time and number of days (0 to 365)."}, {"Неизвестный тип напоминания.", "Unknown reminder type."}, {"Заметка слишком длинная.", "The note is too long."},
            {"Настройки Remember", "Remember settings"}, {"Оформление", "Appearance"}, {"Язык интерфейса", "Interface language"}, {"Цветовая тема", "Colour theme"},
            {"Графит", "Graphite"}, {"Шалфей", "Sage"}, {"Светлая", "Light"}, {"Запуск и напоминания", "Startup and reminders"},
            {"Закрытие окна скрывает календарь в трей.\nПолный выход — через меню значка Remember.", "Closing the calendar keeps Remember in the tray.\nTo quit, use the tray icon menu."},
            {"Запускать вместе с Windows", "Start with Windows"}, {"Задержка после входа в Windows, минут", "Delay after Windows sign-in, minutes"},
            {"Окно сигнала появится после этой задержки. От 0 до 60 минут.", "Show reminders after this delay. From 0 to 60 minutes."},
            {"Звук вместе с окном напоминания", "Play a sound with the reminder"}, {"Экспорт копии", "Export backup"}, {"Импорт копии", "Import backup"},
            {"Показать пробное напоминание", "Show a test reminder"},
            {"Сохранить настройки", "Save settings"}, {"Укажите от 0 до 60 минут.", "Enter 0 to 60 minutes."}, {"Не удалось сохранить: {0}", "Could not save: {0}"},
            {"Remember — пробное напоминание", "Remember — test reminder"}, {"Remember — пора вспомнить", "Remember — reminder"}, {"Пробное напоминание", "Test reminder"},
            {"Remember · напоминание", "Remember · reminder"}, {"Пора вспомнить", "Time to remember"}, {"Через 10 минут", "In 10 minutes"}, {"Помню", "Got it"},
            {"Напомнить завтра", "Remind me tomorrow"}, {"Продлить подписку", "Renew subscription"}, {"Так будет выглядеть ваше напоминание", "This is how your reminders will look"},
            {"Пример · данные не изменяются", "Preview · your data stays unchanged"}, {"Важные даты: {0}", "Important dates: {0}"},
            {"Открыть календарь", "Open calendar"}, {"Добавить напоминание", "Add reminder"}, {"Выйти из Remember", "Quit Remember"},
            {"Восстановление данных", "Data recovery"}, {"Изменения не сохранены.\n{0}", "Your changes were not saved.\n{0}"},
            {"Удалить напоминание?", "Delete reminder?"}, {"«{0}» будет удалено вместе с правилом повторения.", "“{0}” and its recurrence rule will be deleted."},
            {"Экспорт напоминаний", "Export reminders"}, {"Копия Remember (*.json)|*.json", "Remember backup (*.json)|*.json"}, {"Копия сохранена", "Backup saved"},
            {"Импорт напоминаний", "Import reminders"}, {"Импорт завершён", "Import complete"}, {"Добавлено напоминаний: {0}. Существующие записи сохранены.", "Added reminders: {0}. Existing entries were preserved."},
            {"Не удалось прочитать копию. {0}", "Could not read the backup. {0}"}, {"Remember — не удалось запустить", "Remember — could not start"},
            {"Данные восстановлены из резервной копии. Исходный файл сохранён рядом.", "Your data was restored from a backup. The original file was preserved alongside it."}
        };
    }

    public static class Appearance
    {
        public static string Theme { get; private set; } = "graphite";
        static readonly Dictionary<string, string> Base = new Dictionary<string, string>
        {
            {"Bg", "#121415"}, {"Card", "#1B1E20"}, {"Raised", "#222529"}, {"Line", "#272B2E"}, {"Text", "#F4F4F2"}, {"Secondary", "#A9AFB5"}, {"Muted", "#737B83"},
            {"Accent", "#79AFFF"}, {"AccentText", "#111923"}, {"Input", "#141618"}, {"InputBorder", "#30353A"}, {"Hover", "#303A49"}, {"Selection", "#2B3B50"},
            {"Sidebar", "#17191B"}, {"MarkBg", "#24354B"}, {"Success", "#6FD08C"}, {"Error", "#FF7373"}, {"Subscription", "#C4A1FF"}, {"Birthday", "#F1BC8B"}
        };
        static Dictionary<string, Brush> brushes;
        public static Brush Get(string name) { if (brushes == null) Apply("graphite"); return brushes[name]; }
        public static void Apply(string theme)
        {
            Theme = theme == "sage" || theme == "light" ? theme : "graphite";
            var colours = new Dictionary<string, string>(Base);
            if (Theme == "sage")
            {
                colours["Bg"] = "#121815"; colours["Card"] = "#1C2620"; colours["Raised"] = "#28342C"; colours["Line"] = "#314237";
                colours["Accent"] = "#8BD6B2"; colours["AccentText"] = "#10261B"; colours["Input"] = "#151E18"; colours["InputBorder"] = "#3B4E40";
                colours["Hover"] = "#344A3B"; colours["Selection"] = "#344A3B"; colours["Sidebar"] = "#17211B"; colours["MarkBg"] = "#2B4536";
            }
            else if (Theme == "light")
            {
                colours["Bg"] = "#F3F5F8"; colours["Card"] = "#FFFFFF"; colours["Raised"] = "#E5EAF1"; colours["Line"] = "#D8DFE9"; colours["Text"] = "#202B3A";
                colours["Secondary"] = "#4F6076"; colours["Muted"] = "#66758A"; colours["Accent"] = "#326BCA"; colours["AccentText"] = "#FFFFFF";
                colours["Input"] = "#FFFFFF"; colours["InputBorder"] = "#BCC8D9"; colours["Hover"] = "#E0EAF9"; colours["Selection"] = "#D6E4FB";
                colours["Sidebar"] = "#EAF0F7"; colours["MarkBg"] = "#DFE9FA"; colours["Success"] = "#287B4B"; colours["Error"] = "#B63136";
                colours["Subscription"] = "#7951B0"; colours["Birthday"] = "#97601F";
            }
            brushes = new Dictionary<string, Brush>();
            foreach (var pair in colours)
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(pair.Value); brush.Freeze(); brushes[pair.Key] = brush;
                if (Application.Current != null) Application.Current.Resources[pair.Key] = brush;
            }
        }
    }
}
