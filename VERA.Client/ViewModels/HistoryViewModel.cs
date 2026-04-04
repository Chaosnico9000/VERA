using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using VERA.Models;
using VERA.Services;

namespace VERA.ViewModels
{
    public class DayGroup : ObservableCollection<TimeEntry>
    {
        public string DayLabel { get; }
        public string TotalTime { get; }
        public string DeltaText { get; }
        public double DeltaMinutes { get; }
        public Color DeltaColor => DeltaMinutes >= 0 ? Color.FromArgb("#4ECCA3") : Color.FromArgb("#8240CE");

        public DayGroup(string dayLabel, string totalTime, string deltaText, double deltaMinutes, IEnumerable<TimeEntry> entries)
        {
            DayLabel = dayLabel;
            TotalTime = totalTime;
            DeltaText = deltaText;
            DeltaMinutes = deltaMinutes;
            foreach (var e in entries)
                Add(e);
        }
    }

    public class HistoryViewModel : BaseViewModel
    {
        private readonly ITimeTrackingService _service;
        private string _weekTotal = "0m";

        public HistoryViewModel(ITimeTrackingService service)
        {
            _service = service;
            DeleteCommand    = new Command<TimeEntry>(async (e) => await DeleteAsync(e));
            EditCommand      = new Command<TimeEntry>(async (e) => await EditAsync(e));
            AddManualCommand = new Command(async () => await AddManualAsync());
        }

        public ObservableCollection<DayGroup> GroupedEntries { get; } = [];

        public string WeekTotal
        {
            get => _weekTotal;
            private set => SetProperty(ref _weekTotal, value);
        }

        public ICommand DeleteCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand AddManualCommand { get; }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            var all = await _service.GetEntriesAsync();

            cancellationToken.ThrowIfCancellationRequested();

            var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            var weekMinutes = all
                .Where(e => e.StartTime.Date >= weekStart)
                .Sum(e => e.Duration.TotalMinutes);
            WeekTotal = FormatMinutes(weekMinutes);

            cancellationToken.ThrowIfCancellationRequested();

            var sollMin = Preferences.Default.Get("sollzeit_stunden", 8.0) * 60;

            GroupedEntries.Clear();
            var de = CultureInfo.GetCultureInfo("de-DE");
            var groups = all
                .GroupBy(e => e.StartTime.Date)
                .OrderByDescending(g => g.Key);

            foreach (var g in groups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string label;
                if (g.Key == DateTime.Today) label = "Heute";
                else if (g.Key == DateTime.Today.AddDays(-1)) label = "Gestern";
                else label = g.Key.ToString("dddd, dd. MMMM", de);

                var dayMinutes = g.Sum(e => e.Duration.TotalMinutes);
                var isSonderTag = g.All(e => e.IsSonderTag);
                var deltaMin = isSonderTag ? 0.0 : dayMinutes - sollMin;
                var deltaText = isSonderTag ? string.Empty : FormatDelta(deltaMin);
                GroupedEntries.Add(new DayGroup(label, FormatMinutes(dayMinutes), deltaText, deltaMin, g));
            }
        }

        private static string FormatMinutes(double totalMinutes)
        {
            var h = (int)(totalMinutes / 60);
            var m = (int)(totalMinutes % 60);
            return h > 0 ? $"{h}h {m:D2}m" : $"{m}m";
        }

        private static string FormatDelta(double delta)
        {
            var abs = Math.Abs(delta);
            var h = (int)(abs / 60);
            var m = (int)(abs % 60);
            var s = h > 0 ? $"{h}h {m:D2}m" : $"{m}m";
            return delta >= 0 ? $"+{s}" : $"-{s}";
        }

        private async Task DeleteAsync(TimeEntry entry)
        {
            var ok = await Shell.Current.DisplayAlertAsync(
                "Eintrag löschen",
                $"\"{entry.Title}\" wirklich l\u00f6schen?",
                "Löschen", "Abbrechen");
            if (!ok) return;
            await _service.DeleteEntryAsync(entry.Id);
            await LoadAsync();
        }

        private static async Task EditAsync(TimeEntry entry)
        {
            var page = MauiProgram.Services.GetRequiredService<Views.EditEntryPage>();
            page.LoadEntry(entry);
            await Shell.Current.Navigation.PushAsync(page);
        }

        private static async Task AddManualAsync()
        {
            var page = MauiProgram.Services.GetRequiredService<Views.EditEntryPage>();
            page.LoadNew();
            await Shell.Current.Navigation.PushAsync(page);
        }
    }
}
