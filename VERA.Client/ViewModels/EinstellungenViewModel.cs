using System.Windows.Input;
using VERA.Services;

namespace VERA.ViewModels
{
    public class EinstellungenViewModel : BaseViewModel
    {
        private readonly INotificationService _notification;
        private readonly ThemeService _theme;
        private double _sollzeitStunden;
        private bool _erinnerungAktiv;
        private TimeSpan _erinnerungsZeit;
        private DateTime _ersterArbeitstag;
        private string _gewaehltesTheme;

        public EinstellungenViewModel(INotificationService notification, ThemeService theme)
        {
            _notification = notification;
            _theme        = theme;
            _sollzeitStunden  = Preferences.Default.Get("sollzeit_stunden", 8.0);
            _erinnerungAktiv  = Preferences.Default.Get("erinnerung_aktiv", false);
            var h = Preferences.Default.Get("erinnerung_stunde", 9);
            var m = Preferences.Default.Get("erinnerung_minute", 0);
            _erinnerungsZeit  = new TimeSpan(h, m, 0);
            var raw = Preferences.Default.Get("erster_arbeitstag", "2026-04-01");
            _ersterArbeitstag = DateTime.TryParse(raw, out var d) ? d : new DateTime(2026, 4, 1);
            _gewaehltesTheme  = _theme.SavedTheme;

            ErhoehenCommand       = new Command(() => SollzeitStunden = Math.Min(12, SollzeitStunden + 0.5));
            VerringernCommand     = new Command(() => SollzeitStunden = Math.Max(1,  SollzeitStunden - 0.5));
            TestNachrichtCommand  = new Command(TestNachricht);
            ResetCommand          = new Command(async () => await ResetAsync());
            ThemeDarkCommand      = new Command(() => GewaehltesTheme = "dark");
            ThemeLightCommand     = new Command(() => GewaehltesTheme = "light");
            ThemeSystemCommand    = new Command(() => GewaehltesTheme = "system");
            ExportWhatsAppCommand = new Command(async () => await ExportWhatsAppAsync());
        }

        // ── Sollzeit ──
        public double SollzeitStunden
        {
            get => _sollzeitStunden;
            set
            {
                if (SetProperty(ref _sollzeitStunden, value))
                {
                    Preferences.Default.Set("sollzeit_stunden", value);
                    OnPropertyChanged(nameof(SollzeitAnzeige));
                    OnPropertyChanged(nameof(WochenzielAnzeige));
                }
            }
        }

        public string SollzeitAnzeige => SollzeitStunden % 1 == 0
            ? $"{(int)SollzeitStunden}h" : $"{SollzeitStunden:F1}h";

        public string WochenzielAnzeige
        {
            get
            {
                var wh = SollzeitStunden * 5;
                return wh % 1 == 0 ? $"{(int)wh}h" : $"{wh:F1}h";
            }
        }

        // ── Erster Arbeitstag ──
        public DateTime ErsterArbeitstag
        {
            get => _ersterArbeitstag;
            set
            {
                if (SetProperty(ref _ersterArbeitstag, value))
                    Preferences.Default.Set("erster_arbeitstag", value.ToString("yyyy-MM-dd"));
            }
        }

        // ── Benachrichtigungen ──
        public bool ErinnerungAktiv
        {
            get => _erinnerungAktiv;
            set
            {
                if (SetProperty(ref _erinnerungAktiv, value))
                    Preferences.Default.Set("erinnerung_aktiv", value);
            }
        }

        public TimeSpan ErinnerungsZeit
        {
            get => _erinnerungsZeit;
            set
            {
                if (SetProperty(ref _erinnerungsZeit, value))
                {
                    Preferences.Default.Set("erinnerung_stunde", value.Hours);
                    Preferences.Default.Set("erinnerung_minute", value.Minutes);
                }
            }
        }

        public ICommand ErhoehenCommand      { get; }
        public ICommand VerringernCommand    { get; }
        public ICommand TestNachrichtCommand { get; }
        public ICommand ResetCommand         { get; }
        public ICommand ThemeDarkCommand     { get; }
        public ICommand ThemeLightCommand    { get; }
        public ICommand ThemeSystemCommand   { get; }
        public ICommand ExportWhatsAppCommand { get; }

        // ── Theme ──
        public string GewaehltesTheme
        {
            get => _gewaehltesTheme;
            set
            {
                if (SetProperty(ref _gewaehltesTheme, value))
                {
                    _theme.Apply(value);
                    OnPropertyChanged(nameof(IsDark));
                    OnPropertyChanged(nameof(IsLight));
                    OnPropertyChanged(nameof(IsSystem));
                }
            }
        }

        public bool IsDark   => _gewaehltesTheme == "dark";
        public bool IsLight  => _gewaehltesTheme == "light";
        public bool IsSystem => _gewaehltesTheme == "system";

        private void TestNachricht()
            => _notification.SendReminderNotification(
                "VERA \u23f0 Erinnerung",
                "Vergiss nicht, deinen Timer zu starten!");

        private async Task ResetAsync()
        {
            var ok = await Shell.Current.DisplayAlertAsync(
                "Daten l\u00f6schen",
                "Alle Zeiteintr\u00e4ge werden unwiderruflich gel\u00f6scht. Fortfahren?",
                "Ja, l\u00f6schen", "Abbrechen");
            if (!ok) return;
            var service = MauiProgram.Services.GetRequiredService<ITimeTrackingService>();
            foreach (var e in await service.GetEntriesAsync())
                await service.DeleteEntryAsync(e.Id);
            await Shell.Current.DisplayAlertAsync("Erledigt", "Alle Daten wurden gel\u00f6scht.", "OK");
        }

        private async Task ExportWhatsAppAsync()
        {
            var service = MauiProgram.Services.GetRequiredService<ITimeTrackingService>();
            var entries = await service.GetEntriesAsync().ConfigureAwait(false);
            var heute   = DateTime.Today;
            var heutige = entries
                .Where(e => e.StartTime.Date == heute && e.EndTime.HasValue)
                .OrderBy(e => e.StartTime)
                .ToList();

            if (heutige.Count == 0)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Shell.Current.DisplayAlertAsync("Export", "Keine abgeschlossenen Eintr\u00e4ge f\u00fcr heute.", "OK"));
                return;
            }

            var gesamtMin = heutige.Sum(e => (int)(e.EndTime!.Value - e.StartTime).TotalMinutes);
            var sb        = new System.Text.StringBuilder();
            sb.AppendLine($"\u23f1\ufe0f *VERA \u2014 {heute:dd.MM.yyyy}*");
            sb.AppendLine();
            foreach (var e in heutige)
            {
                var dauer = e.EndTime!.Value - e.StartTime;
                sb.AppendLine($"\u2022 {e.Title}");
                sb.AppendLine($"  {e.StartTime:HH:mm} \u2013 {e.EndTime.Value:HH:mm} ({(int)dauer.TotalHours}h {dauer.Minutes:D2}m)");
            }
            sb.AppendLine();
            sb.AppendLine($"*Gesamt: {gesamtMin / 60}h {gesamtMin % 60:D2}m*");

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Text    = sb.ToString(),
                    Title   = "Tagesübersicht teilen",
                });
            });
        }
    }
}