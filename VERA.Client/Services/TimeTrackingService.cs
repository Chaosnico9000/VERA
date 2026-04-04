using System.Text.Json;
using System.Text.Json.Serialization;
using VERA.Models;

namespace VERA.Services
{
    public class TimeTrackingService : ITimeTrackingService
    {
        private readonly string _filePath;
        private List<TimeEntry>? _cache;

        // Sichere Serialisierungsoptionen: kein polymorphes Type-Handling, kein Kommentar-Support
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement
        };

        public TimeTrackingService()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "vera_entries.json");
        }

        private async Task<List<TimeEntry>> LoadAsync()
        {
            if (_cache != null) return _cache;
            if (!File.Exists(_filePath))
            {
                _cache = [];
                return _cache;
            }
            var json = await File.ReadAllTextAsync(_filePath);
            _cache = JsonSerializer.Deserialize<List<TimeEntry>>(json, _jsonOptions) ?? [];
            return _cache;
        }

        private async Task PersistAsync()
        {
            var json = JsonSerializer.Serialize(_cache ?? [], _jsonOptions);
            // Atomar schreiben: erst in Temp-Datei, dann umbenennen (verhindert Datenverlust bei Absturz)
            var tmp = _filePath + ".tmp";
            await File.WriteAllTextAsync(tmp, json);
            File.Move(tmp, _filePath, overwrite: true);
        }

        public async Task<List<TimeEntry>> GetEntriesAsync()
        {
            var entries = await LoadAsync();
            return [.. entries.OrderByDescending(e => e.StartTime)];
        }

        public async Task<TimeEntry?> GetActiveEntryAsync()
        {
            var entries = await LoadAsync();
            return entries.FirstOrDefault(e => e.EndTime == null);
        }

        public async Task<TimeEntry> StartTimerAsync(string title, string category = "")
        {
            var entries = await LoadAsync();
            foreach (var running in entries.Where(e => e.EndTime == null))
                running.EndTime = DateTime.Now;

            var entry = new TimeEntry
            {
                Title = string.IsNullOrWhiteSpace(title) ? "Aufgabe" : title.Trim(),
                Category = category,
                StartTime = DateTime.Now
            };
            entries.Add(entry);
            await PersistAsync();
            return entry;
        }

        public async Task StopTimerAsync(Guid id)
        {
            var entries = await LoadAsync();
            var entry = entries.FirstOrDefault(e => e.Id == id);
            if (entry?.EndTime == null)
            {
                entry!.EndTime = DateTime.Now;
                await PersistAsync();
            }
        }

        public async Task DeleteEntryAsync(Guid id)
        {
            var entries = await LoadAsync();
            entries.RemoveAll(e => e.Id == id);
            _cache = entries;
            await PersistAsync();
        }

        public async Task<TimeEntry> AddSonderTagAsync(EntryType type, string title, DateTime date)
        {
            var entries = await LoadAsync();
            var sollStunden = Preferences.Default.Get("sollzeit_stunden", 8.0);
            var dauer = type == EntryType.UrlaubHalbtag ? sollStunden / 2 : sollStunden;
            var entry = new TimeEntry
            {
                Title = title,
                Type = type,
                StartTime = date.Date.AddHours(8),
                EndTime = date.Date.AddHours(8 + dauer)
            };
            entries.Add(entry);
            await PersistAsync();
            return entry;
        }

        public async Task UpdateEntryAsync(TimeEntry entry)
        {
            var entries = await LoadAsync();
            var idx = entries.FindIndex(e => e.Id == entry.Id);
            if (idx >= 0)
            {
                entries[idx] = entry;
                await PersistAsync();
            }
        }

        public async Task AddManualEntryAsync(TimeEntry entry)
        {
            var entries = await LoadAsync();
            entries.Add(entry);
            await PersistAsync();
        }
    }
}
