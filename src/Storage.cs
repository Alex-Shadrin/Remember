using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Remember
{
    public sealed class Storage
    {
        public string DirectoryPath { get; private set; }
        public string FilePath { get { return Path.Combine(DirectoryPath, "reminders.json"); } }
        public string Warning { get; private set; }
        readonly DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppData));
        public Storage(string directory) { DirectoryPath = directory; }

        public AppData Load()
        {
            if (!File.Exists(FilePath)) return new AppData();
            try { return Read(FilePath); }
            catch (Exception ex) when (ex is IOException || ex is System.Runtime.Serialization.SerializationException || ex is ArgumentException)
            {
                // Preserve the damaged file before any later save.
                var preserved = FilePath + ".damaged-" + DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
                File.Copy(FilePath, preserved, false);
                if (File.Exists(FilePath + ".bak"))
                {
                    try { var data = Read(FilePath + ".bak"); Warning = "Данные восстановлены из резервной копии. Исходный файл сохранён рядом."; return data; }
                    catch (Exception backupError) when (backupError is IOException || backupError is System.Runtime.Serialization.SerializationException || backupError is ArgumentException) { }
                }
                throw new IOException("Файл напоминаний повреждён. Он сохранён по адресу " + preserved + ". Восстановите reminders.json из своей копии.", ex);
            }
        }

        public AppData Read(string path)
        {
            AppData data;
            using (var stream = File.OpenRead(path)) data = (AppData)serializer.ReadObject(stream);
            if (data == null || data.Version != 1 || data.Reminders == null || data.StartupDelaySeconds < 0 || data.StartupDelaySeconds > 3600)
                throw new ArgumentException("Неподдерживаемый формат данных.");
            foreach (var r in data.Reminders) { if (r == null) throw new ArgumentException("Повреждённая запись."); r.Validate(); }
            if (data.Reminders.Select(r => r.Id).Distinct().Count() != data.Reminders.Count) throw new ArgumentException("Повторяющиеся идентификаторы.");
            return data;
        }

        public void Save(AppData data)
        {
            Directory.CreateDirectory(DirectoryPath);
            var temp = FilePath + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                serializer.WriteObject(stream, data);
                stream.Flush(true);
            }
            if (File.Exists(FilePath)) File.Replace(temp, FilePath, FilePath + ".bak");
            else File.Move(temp, FilePath);
        }

        public void Export(AppData data, string path)
        {
            // Do not allow exporting over the live store or backup.
            var full = Path.GetFullPath(path);
            if (full.Equals(Path.GetFullPath(FilePath), StringComparison.OrdinalIgnoreCase) || full.Equals(Path.GetFullPath(FilePath + ".bak"), StringComparison.OrdinalIgnoreCase))
                throw new IOException("Выберите файл вне папки данных Remember.");
            using (var stream = File.Create(path)) serializer.WriteObject(stream, data);
        }
    }
}
