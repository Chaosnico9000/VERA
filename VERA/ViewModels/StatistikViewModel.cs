using System.Collections.ObjectModel;
using System.Windows.Input;
using VERA.Models;
using VERA.Services;

namespace VERA.ViewModels
{
    public class TagData : BaseViewModel
    {
        public string Name { get; set; } = string.Empty;
        public double GearbeiteteMinuten { get; set; }
        public double SollMinuten { get; set; }
        public bool IstHeute { get; set; }
        public bool IstZukunft { get; set; }
        public string? SonderLabel { get; set; }

        public bool IstSonderTag => !string.IsNullOrEmpty(SonderLabel);
        public double Fortschritt => SollMinuten > 0 ? Math.Min(1.0, GearbeiteteMinuten / SollMinuten) : 0;

        public string GearbeiteteAnzeige
        {
            get
            {
                if (IstZukunft) return "\u2013";
                if (SonderLabel != null) return SonderLabel;
                if (GearbeiteteMinuten < 1) return "0m";
                var h = (int)(GearbeiteteMinuten / 60);
                var m = (int)(GearbeiteteMinuten % 60);
                return h > 0 ? $"{h}h {m:D2}m" : $"{m}m";
            }
        }

        public Color FortschrittFarbe
        {
            get
            {
                if (IstZukunft) return Color.FromArgb("#1A2460");
                if (IstSonderTag) return Color.FromArgb("#8240CE");
                if (Fortschritt >= 1.0) return Color.FromArgb("#4ECCA3");
                if (Fortschritt >= 0.8) return Color.FromArgb("#3566E5");
                if (Fortschritt >= 0.5) return Color.FromArgb("#FFB347");
                if (GearbeiteteMinuten < 1) return Color.FromArgb("#1A2460");
                return Color.FromArgb("#8240CE");
            }
        }
    }

    public class WochenKachel
    {
        public string Label { get; set; } = string.Empty;
        public string Gesamt { get; set; } = string.Empty;
        public string Puffer { get; set; } = string.Empty;
        public Color PufferFarbe { get; set; } = Color.FromArgb("#8FA0DC");
        public double Fortschritt { get; set; }
        public Color FortschrittFarbe { get; set; } = Color.FromArgb("#3566E5");
        public bool IstAktuelleWoche { get; set; }
    }

    public class KalenderTagData
    {
        public int Tag { get; set; }
        public bool IstImMonat { get; set; }
        public bool IstHeute { get; set; }
        public bool IstVorErstemArbeitstag { get; set; }
        public bool IstWochenende { get; set; }
        public bool HatEintrag { get; set; }
        public bool SollErreicht { get; set; }
        public bool IstSonderTag { get; set; }
        public bool IstZukunft { get; set; }
        public string TagText => IstImMonat ? Tag.ToString() : string.Empty;

        // Hintergrundfarbe der Zelle
        public Color HintergrundFarbe
        {
            get
            {
                if (!IstImMonat || IstVorErstemArbeitstag) return Color.FromArgb("#0A0F2A");
                if (IstHeute) return Color.FromArgb("#1A3580");
                if (IstWochenende) return Color.FromArgb("#0D1232");
                if (IstZukunft) return Color.FromArgb("#0D1232");
                if (IstSonderTag) return Color.FromArgb("#2A1555");
                if (SollErreicht) return Color.FromArgb("#0D2B22");
                if (HatEintrag) return Color.FromArgb("#1A1A40");
                return Color.FromArgb("#0D1232");
            }
        }

        public Color TextFarbe
        {
            get
            {
                if (!IstImMonat) return Colors.Transparent;
                if (IstHeute) return Color.FromArgb("#FFFFFF");
                if (IstVorErstemArbeitstag || IstWochenende) return Color.FromArgb("#2A3260");
                if (IstZukunft) return Color.FromArgb("#424F8A");
                if (IstSonderTag) return Color.FromArgb("#AF75EF");
                if (SollErreicht) return Color.FromArgb("#4ECCA3");
                if (HatEintrag) return Color.FromArgb("#7AA5FF");
                return Color.FromArgb("#424F8A");
            }
        }

        public Color RandFarbe => IstHeute ? Color.FromArgb("#3566E5") : Colors.Transparent;
        public string IndikatorEmoji
        {
            get
            {
                if (!IstImMonat || IstVorErstemArbeitstag || IstZukunft || IstWochenende) return string.Empty;
                if (IstSonderTag) return "✦";
                if (SollErreicht) return "✓";
                if (HatEintrag) return "·";
                return string.Empty;
            }
        }
        public Color IndikatorFarbe
        {
            get
            {
                if (IstSonderTag) return Color.FromArgb("#8240CE");
                if (SollErreicht) return Color.FromArgb("#4ECCA3");
                return Color.FromArgb("#3566E5");
            }
        }
    }

    public class StatistikViewModel : BaseViewModel
    {
        private readonly ITimeTrackingService _service;
        private string _wocheGesamt = "0m";
        private string _wocheSoll = "40h";
        private string _wochePuffer = "\u00b10m";
        private Color _pufferFarbe = Color.FromArgb("#8FA0DC");
        private string _monatGesamt = "0m";
        private string _monatSoll = "0h";
        private string _monatPuffer = "\u00b10m";
        private Color _monatPufferFarbe = Color.FromArgb("#8FA0DC");
        private string _monatLabel = string.Empty;
        private bool _zeigeWoche = true;
        private int _kalenderMonatOffset = 0;

        public StatistikViewModel(ITimeTrackingService service)
        {
            _service = service;
            ZeigeWocheCommand  = new Command(() => ZeigeWoche = true);
            ZeigeMonatCommand  = new Command(() => ZeigeWoche = false);
            VorigerMonatCommand = new Command(() => { _kalenderMonatOffset--; _ = LoadAsync(); });
            NaechsterMonatCommand = new Command(() => { _kalenderMonatOffset++; if (_kalenderMonatOffset > 0) _kalenderMonatOffset = 0; _ = LoadAsync(); });
        }

        public ICommand ZeigeWocheCommand { get; }
        public ICommand ZeigeMonatCommand { get; }
        public ICommand VorigerMonatCommand { get; }
        public ICommand NaechsterMonatCommand { get; }

        public bool ZeigeWoche
        {
            get => _zeigeWoche;
            set { if (SetProperty(ref _zeigeWoche, value)) OnPropertyChanged(nameof(ZeigeMonat)); }
        }
        public bool ZeigeMonat => !_zeigeWoche;

        public ObservableCollection<TagData> WochenDaten { get; } = [];
        public ObservableCollection<KalenderTagData> KalenderTage { get; } = [];
        public IReadOnlyList<string> WochentagKopf { get; } = ["Mo","Di","Mi","Do","Fr","Sa","So"];

        public string WocheGesamt { get => _wocheGesamt; private set => SetProperty(ref _wocheGesamt, value); }
        public string WocheSoll   { get => _wocheSoll;   private set => SetProperty(ref _wocheSoll,   value); }
        public string WochePuffer { get => _wochePuffer; private set => SetProperty(ref _wochePuffer, value); }
        public Color  PufferFarbe { get => _pufferFarbe; private set => SetProperty(ref _pufferFarbe, value); }

        public ObservableCollection<WochenKachel> MonatsWochen { get; } = [];

        public string MonatGesamt     { get => _monatGesamt;     private set => SetProperty(ref _monatGesamt,     value); }
        public string MonatSoll       { get => _monatSoll;       private set => SetProperty(ref _monatSoll,       value); }
        public string MonatPuffer     { get => _monatPuffer;     private set => SetProperty(ref _monatPuffer,     value); }
        public Color  MonatPufferFarbe{ get => _monatPufferFarbe;private set => SetProperty(ref _monatPufferFarbe,value); }
        public string MonatLabel      { get => _monatLabel;      private set => SetProperty(ref _monatLabel,      value); }

        public async Task LoadAsync()
        {
            var all = await _service.GetEntriesAsync();
            var sollStunden = Preferences.Default.Get("sollzeit_stunden", 8.0);
            var sollProTag = sollStunden * 60;
            LoadWoche(all, sollProTag, sollStunden);
            LoadMonat(all, sollProTag);
            LoadKalender(all, sollProTag);
        }

        private void LoadWoche(List<TimeEntry> all, double sollProTag, double sollStunden)
        {
            var sollWoche = sollStunden * 5;
            WocheSoll = sollWoche % 1 == 0 ? $"{(int)sollWoche}h" : $"{sollWoche:F1}h";
            var heute = DateTime.Today;
            var raw = Preferences.Default.Get("erster_arbeitstag", "2026-04-01");
            var ersterArbeitstag = DateTime.TryParse(raw, out var d) ? d.Date : new DateTime(2026, 4, 1);
            var dow = (int)heute.DayOfWeek;
            var ws = heute.AddDays(dow == 0 ? -6 : -(dow - 1));
            WochenDaten.Clear();
            double wMin = 0;
            string[] namen = ["Mo","Di","Mi","Do","Fr"];
            for (int i = 0; i < 5; i++)
            {
                var tag = ws.AddDays(i);
                var (tMin, sl) = CalcTag(all, tag, sollProTag, heute);
                wMin += tMin;
                WochenDaten.Add(new TagData { Name=namen[i], GearbeiteteMinuten=tMin, SollMinuten=sollProTag, IstHeute=tag==heute, IstZukunft=tag>heute || tag<ersterArbeitstag, SonderLabel=sl });
            }
            WocheGesamt = Fmt(wMin);
            var dow2 = dow == 0 || dow == 6 ? 5 : dow;
            int arb = 0;
            for (int i = 0; i < dow2; i++)
            {
                var tag = ws.AddDays(i);
                if (tag >= ersterArbeitstag) arb++;
            }
            var puf = wMin - sollProTag * arb;
            WochePuffer = FmtPuf(puf);
            PufferFarbe = puf >= 0 ? Color.FromArgb("#4ECCA3") : Color.FromArgb("#8240CE");
        }

        private void LoadMonat(List<TimeEntry> all, double sollProTag)
        {
            var heute = DateTime.Today;
            var raw = Preferences.Default.Get("erster_arbeitstag", "2026-04-01");
            var ersterArbeitstag = DateTime.TryParse(raw, out var d) ? d.Date : new DateTime(2026, 4, 1);
            var ms = new DateTime(heute.Year, heute.Month, 1);
            var me = ms.AddMonths(1).AddDays(-1);
            MonatLabel = heute.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
            double monatMin = 0; int arbVorbei = 0;
            MonatsWochen.Clear();
            var cur = ms.AddDays(-(((int)ms.DayOfWeek + 6) % 7));
            while (cur <= me)
            {
                double wMin = 0; int wArb = 0;
                for (int i = 0; i < 5; i++)
                {
                    var tag = cur.AddDays(i);
                    if (tag.Month != heute.Month) continue;
                    var (tMin, _) = CalcTag(all, tag, sollProTag, heute);
                    wMin += tMin; monatMin += tMin;
                    if (tag <= heute && tag >= ersterArbeitstag) { arbVorbei++; wArb++; }
                }
                var kw = System.Globalization.ISOWeek.GetWeekOfYear(cur);
                var puf = wMin - sollProTag * wArb;
                MonatsWochen.Add(new WochenKachel
                {
                    Label = $"KW {kw}", Gesamt = Fmt(wMin), Puffer = FmtPuf(puf),
                    PufferFarbe = puf >= 0 ? Color.FromArgb("#4ECCA3") : Color.FromArgb("#8240CE"),
                    Fortschritt = sollProTag * 5 > 0 ? Math.Min(1.0, wMin / (sollProTag * 5)) : 0,
                    FortschrittFarbe = wMin >= sollProTag * 5 ? Color.FromArgb("#4ECCA3") : Color.FromArgb("#3566E5"),
                    IstAktuelleWoche = cur <= heute && cur.AddDays(6) >= heute
                });
                cur = cur.AddDays(7);
            }
            MonatGesamt = Fmt(monatMin);
            var monatSoll = sollProTag * arbVorbei; MonatSoll = Fmt(monatSoll);
            var mpuf = monatMin - monatSoll; MonatPuffer = FmtPuf(mpuf);
            MonatPufferFarbe = mpuf >= 0 ? Color.FromArgb("#4ECCA3") : Color.FromArgb("#8240CE");
        }

        private void LoadKalender(List<TimeEntry> all, double sollProTag)
        {
            var heute = DateTime.Today;
            var raw = Preferences.Default.Get("erster_arbeitstag", "2026-04-01");
            var ersterArbeitstag = DateTime.TryParse(raw, out var ead) ? ead.Date : new DateTime(2026, 4, 1);

            var refMonat = new DateTime(heute.Year, heute.Month, 1).AddMonths(_kalenderMonatOffset);
            MonatLabel = refMonat.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));

            // Ersten Wochentag des Monats bestimmen (Mo=0)
            var ersterWochentag = ((int)refMonat.DayOfWeek + 6) % 7;
            var tageImMonat = DateTime.DaysInMonth(refMonat.Year, refMonat.Month);

            KalenderTage.Clear();

            // Leere Zellen vor dem 1. des Monats
            for (int i = 0; i < ersterWochentag; i++)
                KalenderTage.Add(new KalenderTagData { IstImMonat = false });

            for (int t = 1; t <= tageImMonat; t++)
            {
                var datum = new DateTime(refMonat.Year, refMonat.Month, t);
                var dow = datum.DayOfWeek;
                var istWE = dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday;
                var tagesEintraege = all.Where(e => e.StartTime.Date == datum).ToList();
                var hatEintrag = tagesEintraege.Any();
                var istSonder = hatEintrag && tagesEintraege.All(e => e.IsSonderTag);
                var minuten = tagesEintraege.Sum(e => e.Duration.TotalMinutes);

                KalenderTage.Add(new KalenderTagData
                {
                    Tag = t,
                    IstImMonat = true,
                    IstHeute = datum == heute,
                    IstVorErstemArbeitstag = datum < ersterArbeitstag,
                    IstWochenende = istWE,
                    HatEintrag = hatEintrag,
                    SollErreicht = !istWE && hatEintrag && minuten >= sollProTag * 0.9,
                    IstSonderTag = istSonder,
                    IstZukunft = datum > heute
                });
            }

            // Restliche Zellen auffüllen damit Grid voll ist (immer 7er-Raster)
            while (KalenderTage.Count % 7 != 0)
                KalenderTage.Add(new KalenderTagData { IstImMonat = false });
        }

        private static (double, string?) CalcTag(List<TimeEntry> all, DateTime tag, double sollProTag, DateTime heute)
        {
            double min = 0; string? label = null;
            foreach (var e in all.Where(x => x.StartTime.Date == tag))
            {
                if (e.Type == EntryType.UrlaubGanztag || e.Type == EntryType.Feiertag)
                    { min += sollProTag; label = e.Type == EntryType.Feiertag ? "Feiertag" : "Urlaub"; }
                else if (e.Type == EntryType.UrlaubHalbtag)
                    { min += sollProTag / 2; label ??= "\u00bd Urlaub"; }
                else if (e.EndTime != null)
                    min += (e.EndTime.Value - e.StartTime).TotalMinutes;
            }
            if (tag == heute)
            {
                var a = all.FirstOrDefault(e => e.EndTime == null && e.Type == EntryType.Arbeit);
                if (a != null) min += (DateTime.Now - a.StartTime).TotalMinutes;
            }
            return (min, label);
        }

        private static string Fmt(double min) { var h=(int)(min/60); var m=(int)(min%60); return h>0?$"{h}h {m:D2}m":$"{m}m"; }
        private static string FmtPuf(double p) { var h=(int)(Math.Abs(p)/60); var m=(int)(Math.Abs(p)%60); return p>=0?$"+{h}h {m:D2}m":$"-{h}h {m:D2}m"; }
    }
}