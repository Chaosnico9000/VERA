using System.Collections.ObjectModel;
using System.Windows.Input;
using VERA.Models;
using VERA.Services;

namespace VERA.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly ITimeTrackingService _service;
        private readonly INotificationService _notification;
        private TimeEntry? _activeEntry;
        private string _timerDisplay = "00:00:00";
        private bool _isRunning;
        private string _todayTotal = "0m";
        private int _todayCount;
        private double _todayProgress;
        private Color _progressColor = Color.FromArgb("#1A2460");
        private string _pufferDisplay = "\u00b10m";
        private Color _pufferColor = Color.FromArgb("#8FA0DC");
        private string _startZeit = string.Empty;
        private bool _sondertagHeuteVorhanden;
        private IDispatcherTimer? _ticker;
        private GamificationViewModel? _gamification;

        public DashboardViewModel(ITimeTrackingService service, INotificationService notification)
        {
            _service = service;
            _notification = notification;
            ToggleTimerCommand = new Command(async () => await ToggleTimerAsync());
            UrlaubGanztagCommand = new Command(async () => await AddSonderTagAsync(EntryType.UrlaubGanztag));
            UrlaubHalbtagCommand = new Command(async () => await AddSonderTagAsync(EntryType.UrlaubHalbtag));
            FeiertagCommand = new Command(async () => await AddSonderTagAsync(EntryType.Feiertag));
        }

        // Wird von ZeitTrackerPage gesetzt (vermeidet zirkuläre DI)
        public GamificationViewModel? Gamification
        {
            get => _gamification;
            set => SetProperty(ref _gamification, value);
        }

        public string TimerDisplay
        {
            get => _timerDisplay;
            private set => SetProperty(ref _timerDisplay, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    OnPropertyChanged(nameof(IsNotRunning));
                    OnPropertyChanged(nameof(ButtonLabel));
                    OnPropertyChanged(nameof(ButtonColor));
                }
            }
        }

        public bool IsNotRunning => !_isRunning;

        public string TodayTotal
        {
            get => _todayTotal;
            private set => SetProperty(ref _todayTotal, value);
        }

        public int TodayCount
        {
            get => _todayCount;
            private set => SetProperty(ref _todayCount, value);
        }

        public double TodayProgress
        {
            get => _todayProgress;
            private set => SetProperty(ref _todayProgress, value);
        }

        public Color ProgressColor
        {
            get => _progressColor;
            private set => SetProperty(ref _progressColor, value);
        }

        public string PufferDisplay
        {
            get => _pufferDisplay;
            private set => SetProperty(ref _pufferDisplay, value);
        }

        public Color PufferColor
        {
            get => _pufferColor;
            private set => SetProperty(ref _pufferColor, value);
        }

        public string StartZeit
        {
            get => _startZeit;
            private set => SetProperty(ref _startZeit, value);
        }

        // true wenn heute Urlaub oder Feiertag eingetragen → Timer sperren
        public bool SondertagHeuteVorhanden
        {
            get => _sondertagHeuteVorhanden;
            private set
            {
                if (SetProperty(ref _sondertagHeuteVorhanden, value))
                    OnPropertyChanged(nameof(TimerGesperrt));
            }
        }

        // Timer-Start gesperrt wenn Sondertyp heute oder bereits läuft (außer zum Stoppen)
        public bool TimerGesperrt => !IsRunning && SondertagHeuteVorhanden;

        public string ButtonLabel => IsRunning ? "\u23f9  Stopp" : "\u25b6  Start";

        public Color ButtonColor => IsRunning
            ? Color.FromArgb("#8240CE")
            : Color.FromArgb("#3566E5");

        public ObservableCollection<TimeEntry> RecentEntries { get; } = [];

        public ICommand ToggleTimerCommand { get; }
        public ICommand UrlaubGanztagCommand { get; }
        public ICommand UrlaubHalbtagCommand { get; }
        public ICommand FeiertagCommand { get; }

        public async Task InitializeAsync()
        {
            var active = await _service.GetActiveEntryAsync();
            if (active != null)
            {
                _activeEntry = active;
                IsRunning = true;
                StartZeit = $"Start: {active.StartTime:HH:mm} Uhr";
                StartTicker();
            }
            await RefreshStatsAsync();
        }

        private async Task ToggleTimerAsync()
        {
            if (IsRunning)
            {
                if (_activeEntry == null) return;
                StopTicker();
                await _service.StopTimerAsync(_activeEntry.Id);
                _notification.StopTimerNotification();
                _activeEntry = null;
                IsRunning = false;
                TimerDisplay = "00:00:00";
                StartZeit = string.Empty;
            }
            else
            {
                // Sperren wenn heute Sondertyp vorhanden
                if (SondertagHeuteVorhanden)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Nicht m\u00f6glich",
                        "F\u00fcr heute ist bereits ein Urlaubs- oder Feiertagseintrag vorhanden.",
                        "OK");
                    return;
                }
                _activeEntry = await _service.StartTimerAsync(GenerateTitle(DateTime.Today));
                IsRunning = true;
                StartZeit = $"Start: {_activeEntry.StartTime:HH:mm} Uhr";
                _notification.StartTimerNotification("Arbeit");
                StartTicker();
            }
            await RefreshStatsAsync();
        }

        private async Task AddSonderTagAsync(EntryType type)
        {
            // Timer l\u00e4uft → erst stoppen
            if (IsRunning)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Timer aktiv",
                    "Bitte stoppe zuerst den laufenden Timer, bevor du einen Sondertag eintr\u00e4gst.",
                    "OK");
                return;
            }

            var today = (await _service.GetEntriesAsync())
                .Where(e => e.StartTime.Date == DateTime.Today)
                .ToList();

            // Bereits irgendein Sondertyp heute vorhanden
            var vorhandenTyp = today.FirstOrDefault(e => e.IsSonderTag);
            if (vorhandenTyp != null)
            {
                var name = vorhandenTyp.TypeBadge;
                await Shell.Current.DisplayAlertAsync(
                    "Bereits eingetragen",
                    $"F\u00fcr heute ist bereits \u201e{name}\u201c eingetragen.",
                    "OK");
                return;
            }

            // Bereits Arbeitszeiteintr\u00e4ge heute
            if (today.Any(e => !e.IsSonderTag))
            {
                var confirm = await Shell.Current.DisplayAlertAsync(
                    "Arbeitszeit vorhanden",
                    "Heute sind bereits Arbeitszeiteintr\u00e4ge vorhanden. Trotzdem eintragen?",
                    "Ja", "Abbrechen");
                if (!confirm) return;
            }

            string bezeichnung = type switch
            {
                EntryType.UrlaubGanztag => "Urlaub (ganztags)",
                EntryType.UrlaubHalbtag => "Urlaub (halbtags)",
                EntryType.Feiertag => "Feiertag",
                _ => ""
            };
            await _service.AddSonderTagAsync(type, bezeichnung, DateTime.Today);
            await RefreshStatsAsync();
        }

        private void StartTicker()
        {
            _ticker = Application.Current!.Dispatcher.CreateTimer();
            _ticker.Interval = TimeSpan.FromSeconds(1);
            _ticker.Tick += OnTick;
            _ticker.Start();
        }

        private void StopTicker()
        {
            if (_ticker == null) return;
            _ticker.Tick -= OnTick;
            _ticker.Stop();
            _ticker = null;
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (_activeEntry == null) return;
            var elapsed = DateTime.Now - _activeEntry.StartTime;
            TimerDisplay = elapsed.ToString(@"hh\:mm\:ss");
            _notification.UpdateTimerNotification(TimerDisplay, _activeEntry.Title);
        }

        private async Task RefreshStatsAsync()
        {
            var all = await _service.GetEntriesAsync();
            var today = all.Where(e => e.StartTime.Date == DateTime.Today).ToList();
            TodayCount = today.Count;
            SondertagHeuteVorhanden = today.Any(e => e.IsSonderTag);

            var sollStunden = Preferences.Default.Get("sollzeit_stunden", 8.0);
            var sollMinuten = sollStunden * 60;

            double totalMinuten = 0;
            foreach (var e in today)
            {
                if (e.Type == EntryType.UrlaubGanztag || e.Type == EntryType.Feiertag)
                    totalMinuten += sollMinuten;
                else if (e.Type == EntryType.UrlaubHalbtag)
                    totalMinuten += sollMinuten / 2;
                else if (e.EndTime != null)
                    totalMinuten += (e.EndTime.Value - e.StartTime).TotalMinutes;
            }

            var h = (int)(totalMinuten / 60);
            var m = (int)(totalMinuten % 60);
            TodayTotal = h > 0 ? $"{h}h {m:D2}m" : $"{m}m";

            TodayProgress = sollMinuten > 0 ? Math.Min(1.0, totalMinuten / sollMinuten) : 0;

            ProgressColor = TodayProgress >= 1.0 ? Color.FromArgb("#4ECCA3") :
                            TodayProgress >= 0.8 ? Color.FromArgb("#3566E5") :
                            TodayProgress >= 0.5 ? Color.FromArgb("#FFB347") :
                            totalMinuten < 1 ? Color.FromArgb("#1A2460") :
                            Color.FromArgb("#8240CE");

            var puffer = totalMinuten - sollMinuten;
            var ph = (int)(Math.Abs(puffer) / 60);
            var pm2 = (int)(Math.Abs(puffer) % 60);
            PufferDisplay = puffer >= 0 ? $"+{ph}h {pm2:D2}m" : $"-{ph}h {pm2:D2}m";
            PufferColor = puffer >= 0 ? Color.FromArgb("#4ECCA3") : Color.FromArgb("#8240CE");

            RecentEntries.Clear();
            foreach (var entry in all.Take(5))
                RecentEntries.Add(entry);
        }

        private static string GenerateTitle(DateTime date)
        {
            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            return $"Arbeitstag \u2013 {date.ToString("ddd, dd. MMM", de)}";
        }
    }
}