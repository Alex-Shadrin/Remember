using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace Remember
{
    public enum Repeat { Once, Monthly, Yearly, Weekly }
    public enum Category { Personal, Subscription, Birthday }

    [DataContract]
    public sealed class Reminder
    {
        [DataMember] public string Id = Guid.NewGuid().ToString("N");
        [DataMember] public string Title = "";
        [DataMember] public string Notes = "";
        public DateTime Date = DateTime.Today;
        // A calendar date is not an instant: changing time zones must not move a birthday.
        [DataMember(Name = "Date")]
        public string StoredDate
        {
            get { return Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
            set { Date = DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture); }
        }
        [DataMember] public int MinuteOfDay = 9 * 60;
        [DataMember] public int LeadDays = 2;
        [DataMember] public Repeat Repeat;
        [DataMember] public Category Category;
        [DataMember] public bool Enabled = true;
        [DataMember] public string AcknowledgedThrough;
        [DataMember] public string SnoozedOccurrence;
        [DataMember] public DateTime? SnoozedUntil;

        public Reminder Copy() { return (Reminder)MemberwiseClone(); }
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Title) || Title.Length > 160)
                throw new ArgumentException("Название должно содержать от 1 до 160 символов.");
            if (Date.Year < 1900 || Date.Year > 9998) throw new ArgumentException("Укажите год от 1900 до 9998.");
            if (MinuteOfDay < 0 || MinuteOfDay > 1439 || LeadDays < 0 || LeadDays > 365)
                throw new ArgumentException("Проверьте время и число дней (от 0 до 365).");
            if (!Enum.IsDefined(typeof(Repeat), Repeat) || !Enum.IsDefined(typeof(Category), Category))
                throw new ArgumentException("Неизвестный тип напоминания.");
            if (Notes != null && Notes.Length > 2000) throw new ArgumentException("Заметка слишком длинная.");
        }
    }

    [DataContract]
    public sealed class AppData
    {
        [DataMember] public int Version = 1;
        [DataMember] public List<Reminder> Reminders = new List<Reminder>();
        [DataMember] public int StartupDelaySeconds = 120;
        [DataMember] public bool Sound = true;
    }

    public sealed class Occurrence
    {
        public Reminder Reminder;
        public DateTime Date;
        public DateTime NotifyAt;
        public string Key { get { return Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); } }
        public bool Acknowledged { get { return string.CompareOrdinal(Reminder.AcknowledgedThrough, Key) >= 0; } }
        public static Occurrence For(Reminder r, DateTime date)
        {
            return new Occurrence { Reminder = r, Date = date.Date, NotifyAt = date.Date.AddMinutes(r.MinuteOfDay).AddDays(-r.LeadDays) };
        }
    }

    public static class Schedule
    {
        // Always calculate from the original day. Jan 31 -> Feb 28 -> Mar 31.
        public static DateTime? OnOrAfter(Reminder r, DateTime from)
        {
            from = from.Date;
            var anchor = r.Date.Date;
            if (from <= anchor) return anchor;
            if (r.Repeat == Repeat.Once) return null;
            if (r.Repeat == Repeat.Weekly)
            {
                int days = (int)(from - anchor).TotalDays;
                return SafeAddDays(anchor, ((days + 6) / 7) * 7);
            }
            int year = from.Year, month = r.Repeat == Repeat.Monthly ? from.Month : anchor.Month;
            var candidate = Clamped(year, month, anchor.Day);
            if (candidate < from)
            {
                if (r.Repeat == Repeat.Monthly)
                {
                    if (month == 12) { year++; month = 1; } else month++;
                }
                else year++;
                if (year > 9999) return null;
                candidate = Clamped(year, month, anchor.Day);
            }
            return candidate;
        }

        static DateTime Clamped(int y, int m, int d) { return new DateTime(y, m, Math.Min(d, DateTime.DaysInMonth(y, m))); }
        static DateTime? SafeAddDays(DateTime d, int days) { return d.Ticks > DateTime.MaxValue.Ticks - (long)days * TimeSpan.TicksPerDay ? (DateTime?)null : d.AddDays(days); }

        public static IEnumerable<Occurrence> Between(Reminder r, DateTime start, DateTime end)
        {
            var next = OnOrAfter(r, start);
            while (next.HasValue && next.Value <= end.Date)
            {
                yield return Occurrence.For(r, next.Value);
                if (next.Value >= DateTime.MaxValue.Date) yield break;
                next = OnOrAfter(r, next.Value.AddDays(1));
            }
        }

        // One catch-up per reminder after downtime; never a cascade of old months.
        public static Occurrence Pending(Reminder r, DateTime now)
        {
            if (!r.Enabled) return null;
            if (r.SnoozedUntil.HasValue && !string.IsNullOrEmpty(r.SnoozedOccurrence))
            {
                DateTime snoozed;
                if (DateTime.TryParseExact(r.SnoozedOccurrence, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out snoozed))
                {
                    var saved = Occurrence.For(r, snoozed);
                    if (!saved.Acknowledged) { saved.NotifyAt = r.SnoozedUntil.Value; return saved; }
                }
            }
            if (r.Repeat == Repeat.Once)
            {
                var once = Occurrence.For(r, r.Date);
                return once.Acknowledged ? null : once;
            }
            // Include the most recent date whose warning time has passed.
            var horizon = now.AddDays(r.LeadDays).AddMinutes(-r.MinuteOfDay).Date;
            DateTime start = horizon;
            if (r.Repeat == Repeat.Monthly) start = horizon.AddMonths(-1);
            else if (r.Repeat == Repeat.Yearly) start = horizon.AddYears(-1);
            else start = horizon.AddDays(-7);
            var latest = Between(r, start, horizon).LastOrDefault();
            if (latest != null && !latest.Acknowledged && latest.NotifyAt <= now) return latest;
            var nextDate = OnOrAfter(r, latest == null ? r.Date : latest.Date.AddDays(1));
            if (!nextDate.HasValue) return null;
            var next = Occurrence.For(r, nextDate.Value);
            // Acknowledgement can be ahead of 'now' after a clock correction.
            while (next.Acknowledged)
            {
                nextDate = OnOrAfter(r, next.Date.AddDays(1));
                if (!nextDate.HasValue) return null;
                next = Occurrence.For(r, nextDate.Value);
            }
            return next;
        }

        public static string RepeatText(Repeat repeat)
        {
            switch (repeat) { case Repeat.Monthly: return "Каждый месяц"; case Repeat.Yearly: return "Каждый год"; case Repeat.Weekly: return "Каждую неделю"; default: return "Один раз"; }
        }
        public static string CategoryText(Category category)
        {
            switch (category) { case Category.Subscription: return "Подписка"; case Category.Birthday: return "День рождения"; default: return "Личное"; }
        }
    }
}
