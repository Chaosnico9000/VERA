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
        private string _uhrzeitDisplay = string.Empty;
        private bool _isRunning;
        private string _todayTotal = "0m";
        private int _todayCount;
        private double _todayProgress;
        private Color _progressColor = Color.FromArgb("#1A2460");
        private string _pufferDisplay = "\u00b10m";
        private Color _pufferColor = Color.FromArgb("#8FA0DC");
        private string _startZeit = string.Empty;
        private bool _sondertagHeuteVorhanden;

        // Pausen-Vorschlag
        private bool _pausenVorschlagSichtbar;
        private string _pausenVorschlagText = string.Empty;

        // Wochenübersicht
        private string _wochenStundenDisplay = "0h 00m";
        private string _wochenZielDisplay = "0h";
        private string _wochenPufferDisplay = "\u00b10h 00m";
        private Color _wochenPufferFarbe = Color.FromArgb("#8FA0DC");
        private double _wochenProgress;

        private IDispatcherTimer? _ticker;
        private GamificationViewModel? _gamification;

        // Zustand für Pausen-Erkennung: letzte Gesamtminuten beim letzten Vorschlags-Check
        private double _lastPausenCheckMinuten = -1;

        public DashboardViewModel(ITimeTrackingService service, INotificationService notification)
        {
            _service = service;
            _notification = notification;
            ToggleTimerCommand = new Command(async () => await ToggleTimerAsync());
            UrlaubGanztagCommand = new Command(async () => await AddSonderTagAsync(EntryType.UrlaubGanztag));
            UrlaubHalbtagCommand = new Command(async () => await AddSonderTagAsync(EntryType.UrlaubHalbtag));
            FeiertagCommand = new Command(async () => await AddSonderTagAsync(EntryType.Feiertag));
            PauseAbweisenCommand = new Command(() => PausenVorschlagSichtbar = false);
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

        public string UhrzeitDisplay
        {
            get => _uhrzeitDisplay;
            private set => SetProperty(ref _uhrzeitDisplay, value);
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

        // ── Pausen-Vorschlag ────────────────────────────────────────────────
        public bool PausenVorschlagSichtbar
        {
            get => _pausenVorschlagSichtbar;
            private set => SetProperty(ref _pausenVorschlagSichtbar, value);
        }

        public string PausenVorschlagText
        {
            get => _pausenVorschlagText;
            private set => SetProperty(ref _pausenVorschlagText, value);
        }

        // ── Wochenübersicht ────────────────────────────────────────────────
        public string WochenStundenDisplay
        {
            get => _wochenStundenDisplay;
            private set => SetProperty(ref _wochenStundenDisplay, value);
        }

        public string WochenZielDisplay
        {
            get => _wochenZielDisplay;
            private set => SetProperty(ref _wochenZielDisplay, value);
        }

        public string WochenPufferDisplay
        {
            get => _wochenPufferDisplay;
            private set => SetProperty(ref _wochenPufferDisplay, value);
        }

        public Color WochenPufferFarbe
        {
            get => _wochenPufferFarbe;
            private set => SetProperty(ref _wochenPufferFarbe, value);
        }

        public double WochenProgress
        {
            get => _wochenProgress;
            private set => SetProperty(ref _wochenProgress, value);
        }

        public ObservableCollection<TimeEntry> RecentEntries { get; } = [];

        public ICommand ToggleTimerCommand { get; }
        public ICommand UrlaubGanztagCommand { get; }
        public ICommand UrlaubHalbtagCommand { get; }
        public ICommand FeiertagCommand { get; }
        public ICommand PauseAbweisenCommand { get; }

        public async Task InitializeAsync()
        {
            UhrzeitDisplay = DateTime.Now.ToString("HH:mm:ss");
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
                PausenVorschlagSichtbar = false;
                _lastPausenCheckMinuten = -1;
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
            var now = DateTime.Now;
            UhrzeitDisplay = now.ToString("HH:mm:ss");

            if (_activeEntry != null)
            {
                var elapsed = now - _activeEntry.StartTime;
                TimerDisplay = elapsed.ToString(@"hh\:mm\:ss");
                _notification.UpdateTimerNotification(TimerDisplay, _activeEntry.Title);

                // Pausen-Vorschlag alle 60 Sekunden prüfen (nicht jede Sekunde neu laden)
                var elapsedMinutes = elapsed.TotalMinutes;
                if (elapsedMinutes - _lastPausenCheckMinuten >= 1.0)
                {
                    _lastPausenCheckMinuten = elapsedMinutes;
                    _ = CheckPausenVorschlagAsync();
                }
            }
        }

        private async Task CheckPausenVorschlagAsync()
        {
            if (!IsRunning || PausenVorschlagSichtbar) return;

            var all = await _service.GetEntriesAsync();
            var today = all.Where(e => e.StartTime.Date == DateTime.Today && !e.IsSonderTag).ToList();

            // Gesamte heute gearbeitete Minuten (inkl. laufendem Eintrag)
            double totalMinuten = 0;
            foreach (var entry in today)
            {
                if (entry.EndTime != null)
                    totalMinuten += (entry.EndTime.Value - entry.StartTime).TotalMinutes;
                else if (entry.IsRunning)
                    totalMinuten += (DateTime.Now - entry.StartTime).TotalMinutes;
            }

            // Schwellwerte: 5,5h → erste Erinnerung; danach alle 2h eine weitere
            const double ersteSchwelle = 330.0; // 5h 30min
            const double wiederholen = 120.0;   // alle 2h danach

            bool schwelleErreicht = totalMinuten >= ersteSchwelle &&
                (totalMinuten - ersteSchwelle) % wiederholen < 1.5;

            if (!schwelleErreicht) return;

            // Pause bereits vorhanden? (Lücke > 5 min zwischen zwei Einträgen)
            var abgeschlossene = today.Where(e => e.EndTime != null)
                                      .OrderBy(e => e.StartTime)
                                      .ToList();
            bool hatPause = false;
            for (int i = 1; i < abgeschlossene.Count; i++)
            {
                var luecke = abgeschlossene[i].StartTime - abgeschlossene[i - 1].EndTime!.Value;
                if (luecke.TotalMinutes >= 5)
                {
                    hatPause = true;
                    break;
                }
            }

            if (!hatPause)
            {
                var h = (int)(totalMinuten / 60);
                var m = (int)(totalMinuten % 60);
                PausenVorschlagText = $"\u2615 Du arbeitest seit {h}h {m:D2}m – Zeit f\u00fcr eine kurze Pause!";
                PausenVorschlagSichtbar = true;
            }
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

            // ── Wochenübersicht (letzte 7 Tage inkl. heute) ─────────────────
            var sevenDaysAgo = DateTime.Today.AddDays(-6);
            var weekEntries = all.Where(e => e.StartTime.Date >= sevenDaysAgo).ToList();

            // Ziel: 5 Werktage × Sollzeit (Mo–Fr; Wochenende zählt nicht als Pflicht)
            // Wir zählen einfach alle 7 Tage vs. 5 × Sollzeit – passt für Vertrauensarbeitszeit
            double wochenZielMinuten = 5.0 * sollMinuten;
            double wochenTotalMinuten = 0;
            foreach (var e in weekEntries)
            {
                if (e.Type == EntryType.UrlaubGanztag || e.Type == EntryType.Feiertag)
                    wochenTotalMinuten += sollMinuten;
                else if (e.Type == EntryType.UrlaubHalbtag)
                    wochenTotalMinuten += sollMinuten / 2;
                else if (e.EndTime != null)
                    wochenTotalMinuten += (e.EndTime.Value - e.StartTime).TotalMinutes;
            }

            var wh = (int)(wochenTotalMinuten / 60);
            var wm = (int)(wochenTotalMinuten % 60);
            WochenStundenDisplay = $"{wh}h {wm:D2}m";

            var wzh = (int)(wochenZielMinuten / 60);
            WochenZielDisplay = $"{wzh}h";

            WochenProgress = wochenZielMinuten > 0
                ? Math.Min(1.0, wochenTotalMinuten / wochenZielMinuten)
                : 0;

            var wochenPuffer = wochenTotalMinuten - wochenZielMinuten;
            var wph = (int)(Math.Abs(wochenPuffer) / 60);
            var wpm = (int)(Math.Abs(wochenPuffer) % 60);
            WochenPufferDisplay = wochenPuffer >= 0
                ? $"+{wph}h {wpm:D2}m"
                : $"-{wph}h {wpm:D2}m";
            WochenPufferFarbe = wochenPuffer >= 0
                ? Color.FromArgb("#4ECCA3")
                : Color.FromArgb("#8240CE");
        }

        private static string GenerateTitle(DateTime date)
        {
            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            return $"Arbeitstag \u2013 {date.ToString("ddd, dd. MMM", de)}";
        }
    }
}
