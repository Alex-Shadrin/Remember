using System;
using System.IO;
using System.Linq;
using Remember;

static class Tests
{
    static int count;
    static void Equal<T>(T expected, T actual, string name)
    {
        if (!Equals(expected, actual)) throw new Exception(name + ": expected " + expected + ", got " + actual);
        count++; Console.WriteLine("PASS " + name);
    }
    static Reminder Monthly() { return new Reminder { Title = "Subscription", Date = new DateTime(2026, 1, 31), Repeat = Repeat.Monthly, LeadDays = 2, AcknowledgedThrough = "2026-01-31" }; }
    static int Main()
    {
        string temp = Path.Combine(Path.GetTempPath(), "Remember-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var monthly = Monthly();
            Equal(new DateTime(2026, 2, 28), Schedule.OnOrAfter(monthly, new DateTime(2026, 2, 1)).Value, "Jan 31 clamps to Feb 28");
            Equal(new DateTime(2026, 3, 31), Schedule.OnOrAfter(monthly, new DateTime(2026, 3, 1)).Value, "March restores anchor day 31");
            Equal(new DateTime(2028, 2, 29), Schedule.OnOrAfter(monthly, new DateTime(2028, 2, 1)).Value, "monthly leap year");
            var birthday = new Reminder { Title = "Birthday", Date = new DateTime(2000, 2, 29), Repeat = Repeat.Yearly, LeadDays = 3 };
            Equal(new DateTime(2027, 2, 28), Schedule.OnOrAfter(birthday, new DateTime(2027, 1, 1)).Value, "leap birthday in ordinary year");
            Equal(new DateTime(2028, 2, 29), Schedule.OnOrAfter(birthday, new DateTime(2027, 3, 1)).Value, "leap birthday restored");
            var p = Schedule.Pending(monthly, new DateTime(2026, 2, 26, 8, 59, 0));
            Equal(new DateTime(2026, 2, 26, 9, 0, 0), p.NotifyAt, "exact lead day and local time");
            Equal(false, p.NotifyAt <= new DateTime(2026, 2, 26, 8, 59, 0), "does not notify a minute early");
            Equal(true, Schedule.Pending(monthly, new DateTime(2026, 2, 26, 9, 0, 0)).NotifyAt <= new DateTime(2026, 2, 26, 9, 0, 0), "notifies on boundary");
            monthly.AcknowledgedThrough = "2026-02-28";
            Equal(new DateTime(2026, 3, 31), Schedule.Pending(monthly, new DateTime(2026, 2, 26, 9, 1, 0)).Date, "acknowledged monthly does not repeat");
            Equal(new DateTime(2031, 6, 30), Schedule.Pending(monthly, new DateTime(2031, 7, 2)).Date, "long downtime catches latest only");
            var once = new Reminder { Title = "Appointment", Date = new DateTime(2026, 4, 12), LeadDays = 0 };
            Equal(once.Date, Schedule.Pending(once, new DateTime(2026, 4, 20)).Date, "one-time overdue survives shutdown");
            once.AcknowledgedThrough = "2026-04-12";
            Equal<Occurrence>(null, Schedule.Pending(once, new DateTime(2026, 4, 20)), "one-time acknowledgement is final");
            once.AcknowledgedThrough = null; once.Enabled = false;
            Equal<Occurrence>(null, Schedule.Pending(once, new DateTime(2026, 4, 20)), "paused reminders never fire");
            monthly.SnoozedOccurrence = "2026-03-31"; monthly.SnoozedUntil = new DateTime(2026, 3, 29, 9, 10, 0);
            Equal(monthly.SnoozedUntil.Value, Schedule.Pending(monthly, new DateTime(2026, 3, 29, 9, 5, 0)).NotifyAt, "snooze delays delivery");
            Equal(new DateTime(2026, 3, 31), Schedule.Pending(monthly, new DateTime(2026, 5, 1)).Date, "snoozed occurrence survives a later restart");
            var weekly = new Reminder { Title = "Weekly", Date = new DateTime(2026, 10, 21), Repeat = Repeat.Weekly };
            Equal(new DateTime(2026, 11, 4), Schedule.OnOrAfter(weekly, new DateTime(2026, 10, 29)).Value, "weekly anchor across month boundary");
            Equal(0, Schedule.Between(once, new DateTime(2026, 4, 13), new DateTime(2027, 1, 1)).Count(), "one-time occurrence not repeated");
            Equal(12, Schedule.Between(Monthly(), new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)).Count(), "twelve calendar marks per year");
            Equal(new DateTime(2026, 1, 31), Schedule.OnOrAfter(Monthly(), new DateTime(2020, 1, 1)).Value, "never emit before anchor");
            var yearCross = new Reminder { Title = "New Year", Date = new DateTime(2027, 1, 1), LeadDays = 3 };
            Equal(new DateTime(2026, 12, 29, 9, 0, 0), Schedule.Pending(yearCross, new DateTime(2026, 12, 20)).NotifyAt, "lead days cross year boundary");
            var storage = new Storage(temp); var data = new AppData(); data.Reminders.Add(Monthly()); storage.Save(data);
            Equal("Subscription", storage.Load().Reminders[0].Title, "JSON round-trip");
            Equal(true, File.ReadAllText(storage.FilePath).Contains("\"Date\":\"2026-01-31\""), "calendar date stored without a time-zone offset");
            data.Reminders[0].Title = "Подписка — русский текст"; storage.Save(data);
            Equal("Подписка — русский текст", storage.Load().Reminders[0].Title, "Unicode round-trip");
            Equal(true, File.Exists(storage.FilePath + ".bak"), "atomic save keeps backup");
            File.WriteAllText(storage.FilePath, "not JSON");
            Equal("Subscription", storage.Load().Reminders[0].Title, "corrupt primary recovers backup");
            Equal(true, Directory.GetFiles(temp, "*.damaged-*").Length > 0, "damaged input preserved");
            var noBackup = new Storage(Path.Combine(temp, "broken")); Directory.CreateDirectory(noBackup.DirectoryPath); File.WriteAllText(noBackup.FilePath, "{}");
            bool rejected = false; try { noBackup.Load(); } catch (IOException) { rejected = true; }
            Equal(true, rejected, "unrecoverable store blocks startup instead of erasing data");
            bool exportRejected = false; try { storage.Export(data, storage.FilePath); } catch (IOException) { exportRejected = true; }
            Equal(true, exportRejected, "export cannot overwrite active store");
            Console.WriteLine("All " + count + " checks passed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
    }
}
