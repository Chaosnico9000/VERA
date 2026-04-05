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

        // ── Timer ──────────────────────────────────────────────────────────
        private string _timerDisplay    = "00:00:00";
        private string _uhrzeitDisplay  = string.Empty;
        private bool   _isRunning;
        private string _startZeit       = string.Empty;

        // ── Tages-Stats ────────────────────────────────────────────────────
        private string _todayTotal      = "0m";
        private string _sollzeitDisplay = "0h";
        private int    _todayCount;
        private double _todayProgress;
        private Color  _progressColor   = Color.FromArgb("#1A2460");
        private string _pufferDisplay   = "\u00b10m";
        private Color  _pufferColor     = Color.FromArgb("#8FA0DC");
        private bool   _sondertagHeuteVorhanden;
        private string _sondertagTitel  = string.Empty;
        private string _tagesMotivation = string.Empty;

        // ── Pausen-Vorschlag ───────────────────────────────────────────────
        private bool   _pausenVorschlagSichtbar;
        private string _pausenVorschlagText = string.Empty;

        // ── Wochenübersicht ────────────────────────────────────────────────
        private string _wochenStundenDisplay  = "0h 00m";
        private string _wochenZielDisplay     = "0h";
        private string _wochenPufferDisplay   = "\u00b10h 00m";
        private Color  _wochenPufferFarbe     = Color.FromArgb("#8FA0DC");
        private double _wochenProgress;
        private string _wochenTageFortschritt = "0 / 0 Tage";

        // ── UI-State ───────────────────────────────────────────────────────
        private bool   _isLoading     = true;
        private bool   _isRefreshing;
        private string _lastRefreshed = string.Empty;

        // ── Interna ────────────────────────────────────────────────────────
        private IDispatcherTimer?      _ticker;
        private GamificationViewModel? _gamification;
        private double                 _lastPausenCheckMinuten = -1;

        // ── Entry-Cache ────────────────────────────────────────────────────
        private List<TimeEntry> _cachedEntries  = [];
        private DateTime        _cacheTimestamp = DateTime.MinValue;
        private const double    CacheTtlSeconds = 30;

        public DashboardViewModel(ITimeTrackingService service, INotificationService notification)
        {
            _service      = service;
            _notification = notification;

            ToggleTimerCommand     = new Command(async () => await ToggleTimerAsync(), () => !IsLoading);
            UrlaubGanztagCommand   = new Command(async () => await AddSonderTagAsync(EntryType.UrlaubGanztag));
            UrlaubHalbtagCommand   = new Command(async () => await AddSonderTagAsync(EntryType.UrlaubHalbtag));
            FeiertagCommand        = new Command(async () => await AddSonderTagAsync(EntryType.Feiertag));
            PauseAbweisenCommand   = new Command(() => PausenVorschlagSichtbar = false);
            RefreshCommand         = new Command(async () => await RefreshAsync());
        }

        // ── Gamification (wird von ZeitTrackerPage gesetzt) ────────────────
        public GamificationViewModel? Gamification
        {
            get => _gamification;
            set => SetProperty(ref _gamification, value);
        }

        // ── Timer-Properties ───────────────────────────────────────────────
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
                    OnPropertyChanged(nameof(TimerGesperrt));
                    OnPropertyChanged(nameof(TimerCardBorderColor));
                }
            }
        }

        public bool IsNotRunning => !_isRunning;

        public string StartZeit
        {
            get => _startZeit;
            private set => SetProperty(ref _startZeit, value);
        }

        // ── Tages-Properties ───────────────────────────────────────────────
        public string TodayTotal
        {
            get => _todayTotal;
            private set => SetProperty(ref _todayTotal, value);
        }

        public string SollzeitDisplay
        {
            get => _sollzeitDisplay;
            private set => SetProperty(ref _sollzeitDisplay, value);
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

        public bool SondertagHeuteVorhanden
        {
            get => _sondertagHeuteVorhanden;
            private set
            {
                if (SetProperty(ref _sondertagHeuteVorhanden, value))
                {
                    OnPropertyChanged(nameof(TimerGesperrt));
                    OnPropertyChanged(nameof(SondertagNichtVorhanden));
                }
            }
        }

        public bool SondertagNichtVorhanden => !_sondertagHeuteVorhanden;

        public string SondertagTitel
        {
            get => _sondertagTitel;
            private set => SetProperty(ref _sondertagTitel, value);
        }

        public string TagesMotivation
        {
            get => _tagesMotivation;
            private set => SetProperty(ref _tagesMotivation, value);
        }

        public bool TimerGesperrt       => !IsRunning && SondertagHeuteVorhanden;
        public Color TimerCardBorderColor => IsRunning ? Color.FromArgb("#00C8F0") : Color.FromArgb("#1A2460");
        public string ButtonLabel        => IsRunning ? "\u23f9  Stopp" : "\u25b6  Start";
        public Color ButtonColor         => IsRunning ? Color.FromArgb("#8240CE") : Color.FromArgb("#3566E5");

        // ── Pausen-Properties ──────────────────────────────────────────────
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

        // ── Wochen-Properties ──────────────────────────────────────────────
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

        public string WochenTageFortschritt
        {
            get => _wochenTageFortschritt;
            private set => SetProperty(ref _wochenTageFortschritt, value);
        }

        // ── UI-State Properties ────────────────────────────────────────────
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                    ((Command)ToggleTimerCommand).ChangeCanExecute();
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public string LastRefreshed
        {
            get => _lastRefreshed;
            private set => SetProperty(ref _lastRefreshed, value);
        }

        // ── Collections + Commands ─────────────────────────────────────────
        public ObservableCollection<TimeEntry> RecentEntries { get; } = [];

        public ICommand ToggleTimerCommand   { get; }
        public ICommand UrlaubGanztagCommand { get; }
        public ICommand UrlaubHalbtagCommand { get; }
        public ICommand FeiertagCommand      { get; }
        public ICommand PauseAbweisenCommand { get; }
        public ICommand RefreshCommand       { get; }

        // ── Öffentliche Methoden ───────────────────────────────────────────

        public async Task InitializeAsync()
        {
            IsLoading      = true;
            UhrzeitDisplay = DateTime.Now.ToString("HH:mm:ss");

            var active = await _service.GetActiveEntryAsync().ConfigureAwait(false);
            if (active != null)
            {
                _activeEntry = active;
                IsRunning    = true;
                StartZeit    = $"Start: {active.StartTime:HH:mm} Uhr";
                StartTicker();
            }

            await RefreshStatsAsync().ConfigureAwait(false);
            IsLoading = false;
        }

        // ── Private: Toggle ────────────────────────────────────────────────

        private async Task ToggleTimerAsync()
        {
            if (IsRunning)
            {
                if (_activeEntry == null) return;
                StopTicker();
                await _service.StopTimerAsync(_activeEntry.Id).ConfigureAwait(false);
                _notification.StopTimerNotification();
                _activeEntry            = null;
                IsRunning               = false;
                TimerDisplay            = "00:00:00";
                StartZeit               = string.Empty;
                PausenVorschlagSichtbar = false;
                _lastPausenCheckMinuten = -1;
            }
            else
            {
                if (SondertagHeuteVorhanden)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Nicht m\u00f6glich",
                        "F\u00fcr heute ist bereits ein Urlaubs- oder Feiertagseintrag vorhanden.",
                        "OK");
                    return;
                }
                _activeEntry = await _service.StartTimerAsync(GenerateTitle(DateTime.Today)).ConfigureAwait(false);
                IsRunning    = true;
                StartZeit    = $"Start: {_activeEntry.StartTime:HH:mm} Uhr";
                _notification.StartTimerNotification("Arbeit");
                StartTicker();
            }

            InvalidateCache();
            await RefreshStatsAsync().ConfigureAwait(false);
        }

        // ── Private: Sondertypen ───────────────────────────────────────────

        private async Task AddSonderTagAsync(EntryType type)
        {
            if (IsRunning)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Timer aktiv",
                    "Bitte stoppe zuerst den laufenden Timer, bevor du einen Sondertag eintr\u00e4gst.",
                    "OK");
                return;
            }

            var all   = await _service.GetEntriesAsync().ConfigureAwait(false);
            var today = all.Where(e => e.StartTime.Date == DateTime.Today).ToList();

            var vorhandenTyp = today.FirstOrDefault(e => e.IsSonderTag);
            if (vorhandenTyp != null)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Bereits eingetragen",
                    $"F\u00fcr heute ist bereits \u201e{vorhandenTyp.TypeBadge}\u201c eingetragen.",
                    "OK");
                return;
            }

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
                EntryType.Feiertag      => "Feiertag",
                _                       => ""
            };

            await _service.AddSonderTagAsync(type, bezeichnung, DateTime.Today).ConfigureAwait(false);
            InvalidateCache();
            await RefreshStatsAsync().ConfigureAwait(false);
        }

        // ── Private: Ticker ────────────────────────────────────────────────

        private void StartTicker()
        {
            _ticker          = Application.Current!.Dispatcher.CreateTimer();
            _ticker.Interval = TimeSpan.FromSeconds(1);
            _ticker.Tick    += OnTick;
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

            if (_activeEntry == null) return;

            var elapsed = now - _activeEntry.StartTime;
            TimerDisplay = elapsed.ToString(@"hh\:mm\:ss");
            _notification.UpdateTimerNotification(TimerDisplay, _activeEntry.Title);

            // Pausen-Check alle 60 Sekunden
            var elapsedMinutes = elapsed.TotalMinutes;
            if (elapsedMinutes - _lastPausenCheckMinuten >= 1.0)
            {
                _lastPausenCheckMinuten = elapsedMinutes;
                _ = CheckPausenVorschlagAsync();
            }
        }

        // ── Private: Pausen-Vorschlag ──────────────────────────────────────

        private async Task CheckPausenVorschlagAsync()
        {
            if (!IsRunning || PausenVorschlagSichtbar) return;

            var entries      = await GetCachedEntriesAsync().ConfigureAwait(false);
            var sollMinuten  = Preferences.Default.Get("sollzeit_stunden", 8.0) * 60;
            var totalMinuten = ComputeMinutes(
                entries.Where(e => e.StartTime.Date == DateTime.Today && !e.IsSonderTag),
                sollMinuten, includeRunning: true);

            const double ersteSchwelle = 330.0; // 5h 30min
            const double wiederholen   = 120.0; // alle 2h

            bool schwelleErreicht =
                totalMinuten >= ersteSchwelle &&
                (totalMinuten - ersteSchwelle) % wiederholen < 1.5;

            if (!schwelleErreicht) return;

            // Pause vorhanden? (Lücke > 5 min zwischen Einträgen)
            var abgeschlossene = entries
                .Where(e => e.StartTime.Date == DateTime.Today && !e.IsSonderTag && e.EndTime != null)
                .OrderBy(e => e.StartTime)
                .ToList();

            for (int i = 1; i < abgeschlossene.Count; i++)
            {
                var luecke = abgeschlossene[i].StartTime - abgeschlossene[i - 1].EndTime!.Value;
                if (luecke.TotalMinutes >= 5) return;
            }

            var h = (int)(totalMinuten / 60);
            var m = (int)(totalMinuten % 60);
            PausenVorschlagText     = $"\u2615 Du arbeitest seit {h}h {m:D2}m \u2013 Zeit f\u00fcr eine kurze Pause!";
            PausenVorschlagSichtbar = true;
        }

        // ── Private: RefreshStats ──────────────────────────────────────────

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            InvalidateCache();
            await RefreshStatsAsync().ConfigureAwait(false);
            IsRefreshing = false;
        }

        private async Task RefreshStatsAsync()
        {
            var all          = await GetCachedEntriesAsync().ConfigureAwait(false);
            var sollStunden  = Preferences.Default.Get("sollzeit_stunden", 8.0);
            var sollMinuten  = sollStunden * 60.0;
            SollzeitDisplay  = $"{sollStunden:0.#}h";

            // ── Tages-Stats ───────────────────────────────────────────────
            var today = all.Where(e => e.StartTime.Date == DateTime.Today).ToList();
            TodayCount              = today.Count(e => !e.IsSonderTag);
            SondertagHeuteVorhanden = today.Any(e => e.IsSonderTag);
            SondertagTitel          = today.FirstOrDefault(e => e.IsSonderTag)?.TypeBadge ?? string.Empty;

            var totalMinuten = ComputeMinutes(today, sollMinuten, includeRunning: false);

            var th = (int)(totalMinuten / 60);
            var tm = (int)(totalMinuten % 60);
            TodayTotal    = th > 0 ? $"{th}h {tm:D2}m" : $"{tm}m";
            TodayProgress = sollMinuten > 0 ? Math.Min(1.0, totalMinuten / sollMinuten) : 0;

            ProgressColor = TodayProgress >= 1.0 ? Color.FromArgb("#4ECCA3") :
                            TodayProgress >= 0.8 ? Color.FromArgb("#3566E5") :
                            TodayProgress >= 0.5 ? Color.FromArgb("#FFB347") :
                            totalMinuten   < 1.0 ? Color.FromArgb("#1A2460") :
                                                   Color.FromArgb("#8240CE");

            var puffer = totalMinuten - sollMinuten;
            var ph     = (int)(Math.Abs(puffer) / 60);
            var pm     = (int)(Math.Abs(puffer) % 60);
            PufferDisplay = puffer >= 0 ? $"+{ph}h {pm:D2}m" : $"-{ph}h {pm:D2}m";
            PufferColor   = puffer >= 0 ? Color.FromArgb("#4ECCA3") : Color.FromArgb("#8240CE");

            TagesMotivation = BuildMotivation(totalMinuten, sollMinuten, TodayProgress);

            // Letzte Einträge
            RecentEntries.Clear();
            foreach (var entry in all.Take(5))
                RecentEntries.Add(entry);

            // ── Wochenübersicht (korrekte Kalenderwoche) ──────────────────
            //
            // Ziel: nur die Werktage (Mo–Fr) der aktuellen Woche bis einschließlich
            // heute als "Pflicht" werten. Wer am Mittwoch anfängt, hat nur 3 Soll-Tage.
            //
            var montagDieserWoche = DateTime.Today.AddDays(
                -(int)DateTime.Today.DayOfWeek == 0
                    ? 6   // Sonntag → 6 Tage zurück
                    : (int)DateTime.Today.DayOfWeek - 1);  // Mo=1..Fr=5

            var weekEntries = all
                .Where(e => e.StartTime.Date >= montagDieserWoche
                         && e.StartTime.Date <= DateTime.Today)
                .ToList();

            // Vergangene Werktage dieser Woche (Mo–Fr bis heute)
            int vergangeneWerktage = 0;
            for (var d = montagDieserWoche; d <= DateTime.Today; d = d.AddDays(1))
            {
                if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                    vergangeneWerktage++;
            }
            // Maximal 5 (Wochentage Mo–Fr)
            vergangeneWerktage = Math.Min(vergangeneWerktage, 5);

            double wochenZielMinuten = vergangeneWerktage * sollMinuten;
            double wochenTotal       = ComputeMinutes(weekEntries, sollMinuten, includeRunning: false);

            var wh = (int)(wochenTotal / 60);
            var wm = (int)(wochenTotal % 60);
            WochenStundenDisplay = $"{wh}h {wm:D2}m";
            WochenZielDisplay    = $"{(int)(wochenZielMinuten / 60)}h";
            WochenProgress       = wochenZielMinuten > 0
                ? Math.Min(1.0, wochenTotal / wochenZielMinuten)
                : 0;

            var wochenPuffer = wochenTotal - wochenZielMinuten;
            var wph          = (int)(Math.Abs(wochenPuffer) / 60);
            var wpm          = (int)(Math.Abs(wochenPuffer) % 60);
            WochenPufferDisplay = wochenPuffer >= 0 ? $"+{wph}h {wpm:D2}m" : $"-{wph}h {wpm:D2}m";
            WochenPufferFarbe   = wochenPuffer >= 0 ? Color.FromArgb("#4ECCA3") : Color.FromArgb("#8240CE");

            // Anzahl Werktage in dieser Woche mit mindestens einem Eintrag
            var tage = weekEntries
                .Where(e => e.StartTime.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                .Select(e => e.StartTime.Date)
                .Distinct()
                .Count();
            WochenTageFortschritt = $"{Math.Min(tage, 5)} / {vergangeneWerktage} Tage";

            LastRefreshed = $"Aktualisiert {DateTime.Now:HH:mm:ss}";
        }

        // ── Private: Hilfsmethoden ─────────────────────────────────────────

        /// <summary>
        /// Summiert Arbeitsminuten. Sondertypen werden mit der vollen/halben Sollzeit gewertet.
        /// </summary>
        private static double ComputeMinutes(
            IEnumerable<TimeEntry> entries,
            double sollMinuten,
            bool includeRunning)
        {
            double total = 0;
            foreach (var e in entries)
            {
                if (e.Type is EntryType.UrlaubGanztag or EntryType.Feiertag)
                    total += sollMinuten;
                else if (e.Type == EntryType.UrlaubHalbtag)
                    total += sollMinuten / 2;
                else if (e.EndTime != null)
                    total += (e.EndTime.Value - e.StartTime).TotalMinutes;
                else if (includeRunning && e.IsRunning)
                    total += (DateTime.Now - e.StartTime).TotalMinutes;
            }
            return total;
        }

        /// <summary>Kontextsensitiver Motivationstext.</summary>
        private static string BuildMotivation(double totalMinuten, double sollMinuten, double progress)
        {
            var dow = DateTime.Today.DayOfWeek;
            if (totalMinuten < 1)
                return dow switch
                {
                    DayOfWeek.Monday    => "Guten Start in die neue Woche! \ud83d\udcaa",
                    DayOfWeek.Friday    => "Letzter Tag \u2014 dann Wochenende! \ud83c\udf89",
                    DayOfWeek.Wednesday => "Halbzeit der Woche \u2014 du schaffst das!",
                    _                   => "Viel Erfolg heute!"
                };

            if (progress >= 1.0) return "Sollzeit erreicht \u2014 top! \ud83c\udfaf";

            var rest     = sollMinuten - totalMinuten;
            var rh       = (int)(rest / 60);
            var rm       = (int)(rest % 60);
            var restText = rh > 0 ? $"{rh}h {rm:D2}m" : $"{rm}m";

            return progress switch
            {
                >= 0.9 => $"Fast geschafft! Noch {restText} \ud83c\udfc1",
                >= 0.7 => $"Nur noch {restText} bis zur Sollzeit \ud83d\udcaa",
                >= 0.5 => $"Halbzeit! Noch {restText} \u00fcbrig",
                >= 0.2 => $"Gut gestartet \u2014 noch {restText}",
                _      => "Los geht\u2019s! Du schaffst das \ud83d\ude80"
            };
        }

        private async Task<List<TimeEntry>> GetCachedEntriesAsync()
        {
            if (_cachedEntries.Count > 0 &&
                (DateTime.Now - _cacheTimestamp).TotalSeconds < CacheTtlSeconds)
                return _cachedEntries;

            _cachedEntries  = await _service.GetEntriesAsync().ConfigureAwait(false);
            _cacheTimestamp = DateTime.Now;
            return _cachedEntries;
        }

        private void InvalidateCache()
        {
            _cachedEntries  = [];
            _cacheTimestamp = DateTime.MinValue;
        }

        private static string GenerateTitle(DateTime date)
        {
            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            return $"Arbeitstag \u2013 {date.ToString("ddd, dd. MMM", de)}";
        }
    }
}
