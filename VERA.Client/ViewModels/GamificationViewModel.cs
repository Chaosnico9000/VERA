using System.Collections.ObjectModel;
using VERA.Services;

namespace VERA.ViewModels
{
    // ── Milestone-Eintrag für die LevelingPage ────────────────────────────────
    public class MilestoneEntry
    {
        public string Emoji        { get; init; } = string.Empty;
        public string Titel        { get; init; } = string.Empty;
        public string Beschreibung { get; init; } = string.Empty;
        public long   XpFehlt      { get; init; }
        public string XpFehltLabel => XpFehlt <= 0 ? "✓" : $"-{XpFehlt:N0} XP";
    }

    public class GamificationViewModel : BaseViewModel
    {
        private readonly GamificationService _service;
        private readonly ITimeTrackingService _trackingService;

        private string _levelEmoji        = "🌱";
        private string _levelTitle        = "Frischling";
        private string _rangTitel         = "Einsteiger";
        private string _rangEmoji         = "🌱";
        private Color  _rangFarbe         = Color.FromArgb("#8FA0DC");
        private int    _level;
        private long   _totalXP;
        private long   _xpInLevel;
        private long   _xpForNextLevel    = 2000;
        private double _levelProgress;
        private int    _streak;
        private int    _besterStreak;
        private int    _arbeitstage;
        private string _arbeitszeitGesamt = "0h";
        private bool   _newLevelUp;
        private bool   _newStreakMilestone;
        private bool   _newAchievement;
        private Color  _progressColor     = Color.FromArgb("#3566E5");
        private string _levelUpText       = string.Empty;
        private long   _xpAusArbeit;
        private long   _xpAusSonder;
        private long   _xpAusStreak;
        private long   _xpAusAchievements;
        private long   _xpAusJubilaeum;

        public GamificationViewModel(GamificationService service, ITimeTrackingService trackingService)
        {
            _service = service;
            _trackingService = trackingService;
        }

        public string LevelEmoji        { get => _levelEmoji;        private set => SetProperty(ref _levelEmoji,        value); }
        public string LevelTitle        { get => _levelTitle;        private set => SetProperty(ref _levelTitle,        value); }
        public string RangTitel         { get => _rangTitel;         private set => SetProperty(ref _rangTitel,         value); }
        public string RangEmoji         { get => _rangEmoji;         private set => SetProperty(ref _rangEmoji,         value); }
        public Color  RangFarbe         { get => _rangFarbe;         private set => SetProperty(ref _rangFarbe,         value); }
        public int    Level             { get => _level;             private set => SetProperty(ref _level,             value); }
        public long   TotalXP           { get => _totalXP;           private set => SetProperty(ref _totalXP,           value); }
        public long   XPInLevel         { get => _xpInLevel;         private set => SetProperty(ref _xpInLevel,         value); }
        public long   XPForNextLevel    { get => _xpForNextLevel;    private set => SetProperty(ref _xpForNextLevel,    value); }
        public double LevelProgress     { get => _levelProgress;     private set => SetProperty(ref _levelProgress,     value); }
        public int    Streak            { get => _streak;            private set => SetProperty(ref _streak,            value); }
        public int    BesterStreak      { get => _besterStreak;      private set => SetProperty(ref _besterStreak,      value); }
        public int    Arbeitstage       { get => _arbeitstage;       private set => SetProperty(ref _arbeitstage,       value); }
        public string ArbeitszeitGesamt { get => _arbeitszeitGesamt; private set => SetProperty(ref _arbeitszeitGesamt, value); }
        public bool   NewLevelUp        { get => _newLevelUp;        private set => SetProperty(ref _newLevelUp,        value); }
        public bool   NewStreakMilestone{ get => _newStreakMilestone; private set => SetProperty(ref _newStreakMilestone,value); }
        public bool   NewAchievement    { get => _newAchievement;    private set => SetProperty(ref _newAchievement,    value); }
        public Color  ProgressColor     { get => _progressColor;     private set => SetProperty(ref _progressColor,     value); }
        public string LevelUpText       { get => _levelUpText;       private set => SetProperty(ref _levelUpText,       value); }
        public long   XpAusArbeit       { get => _xpAusArbeit;       private set => SetProperty(ref _xpAusArbeit,       value); }
        public long   XpAusSonder       { get => _xpAusSonder;       private set => SetProperty(ref _xpAusSonder,       value); }
        public long   XpAusStreak       { get => _xpAusStreak;       private set => SetProperty(ref _xpAusStreak,       value); }
        public long   XpAusAchievements { get => _xpAusAchievements; private set => SetProperty(ref _xpAusAchievements, value); }
        public long   XpAusJubilaeum    { get => _xpAusJubilaeum;    private set => SetProperty(ref _xpAusJubilaeum,    value); }

        public ObservableCollection<Achievement>    Achievements   { get; } = [];
        public ObservableCollection<MilestoneEntry> NextMilestones { get; } = [];

        // Computed Labels
        public string XPLabel                => $"{XPInLevel:N0} / {XPForNextLevel:N0} XP";
        public string TotalXPLabel           => $"∑ {TotalXP:N0} XP";
        public string StreakFlame            => Streak >= 52 ? "👑" : Streak >= 26 ? "🔥🔥" : Streak >= 13 ? "🔥" : Streak >= 4 ? "⚡" : "💤";
        public string StreakLabel            => Streak == 1 ? "1 Woche" : $"{Streak} Wochen";
        public string BesterStreakLabel      => $"Rekord: {BesterStreak}W";
        public int    AchievementCount       => Achievements.Count(a => a.Erreicht);
        public string AchievementLabel       => $"{AchievementCount} / {Achievements.Count}";
        public string XpAusArbeitLabel       => $"{XpAusArbeit:N0} XP";
        public string XpAusSonderLabel       => $"{XpAusSonder:N0} XP";
        public string XpAusStreakLabel       => $"{XpAusStreak:N0} XP";
        public string XpAusAchievementsLabel => $"{XpAusAchievements:N0} XP";
        public string XpAusJubilaeumLabel    => $"{XpAusJubilaeum:N0} XP";

        public async Task LoadAsync()
        {
            var entries = await _trackingService.GetEntriesAsync();
            var r = _service.Calculate(entries);

            LevelEmoji         = r.LevelEmoji;
            LevelTitle         = r.LevelTitle;
            RangTitel          = r.AktuellerRang.Titel;
            RangEmoji          = r.AktuellerRang.Emoji;
            RangFarbe          = Color.FromArgb(r.AktuellerRang.Farbe);
            Level              = r.Level;
            TotalXP            = r.TotalXP;
            XPInLevel          = r.XPInLevel;
            XPForNextLevel     = r.XPForNextLevel;
            LevelProgress      = r.LevelProgress;
            Streak             = r.Streak;
            BesterStreak       = r.BesterStreak;
            Arbeitstage        = r.GesamtArbeitstage;
            NewLevelUp         = r.NewLevelUp;
            NewStreakMilestone  = r.NewStreakMilestone;
            NewAchievement     = r.NewAchievement;
            XpAusArbeit        = r.XpAusArbeit;
            XpAusSonder        = r.XpAusSonderTage;
            XpAusStreak        = r.XpAusStreak;
            XpAusAchievements  = r.XpAusAchievements;
            XpAusJubilaeum     = r.XpAusJubilaeum;

            var gesamtStunden = r.GesamtArbeitstage * Preferences.Default.Get("sollzeit_stunden", 8.0);
            ArbeitszeitGesamt = gesamtStunden >= 1000
                ? $"{gesamtStunden / 1000:F1}k h"
                : $"{(int)gesamtStunden}h";

            if (r.NewLevelUp)
                LevelUpText = $"🎉 Level Up! Du bist jetzt {r.LevelEmoji} {r.LevelTitle}!";
            else if (r.NewAchievement)
                LevelUpText = $"🏅 Neues Achievement freigeschaltet!";
            else if (r.NewStreakMilestone)
                LevelUpText = $"🔥 {r.Streak} Wochen Streak! Weiter so!";

            Achievements.Clear();
            foreach (var a in r.Achievements)
                Achievements.Add(a);

            ProgressColor = r.LevelProgress >= 0.9
                ? Color.FromArgb("#4ECCA3")
                : r.LevelProgress >= 0.5
                    ? Color.FromArgb("#3566E5")
                    : Color.FromArgb("#8240CE");

            // Nächste 3 Achievement-Meilensteine + nächster Level
            NextMilestones.Clear();
            // Nächster Level
            if (r.Level < 29)
            {
                NextMilestones.Add(new MilestoneEntry
                {
                    Emoji        = "⬆️",
                    Titel        = $"Level {r.Level + 1}",
                    Beschreibung = "Nächste Stufe erreichen",
                    XpFehlt      = r.XPForNextLevel - r.XPInLevel
                });
            }
            // Nicht erreichte Achievements
            foreach (var a in r.Achievements.Where(x => !x.Erreicht).Take(4))
            {
                NextMilestones.Add(new MilestoneEntry
                {
                    Emoji        = a.Emoji,
                    Titel        = a.Titel,
                    Beschreibung = a.Beschreibung,
                    XpFehlt      = 0
                });
            }

            OnPropertyChanged(nameof(XPLabel));
            OnPropertyChanged(nameof(TotalXPLabel));
            OnPropertyChanged(nameof(StreakFlame));
            OnPropertyChanged(nameof(StreakLabel));
            OnPropertyChanged(nameof(BesterStreakLabel));
            OnPropertyChanged(nameof(AchievementCount));
            OnPropertyChanged(nameof(AchievementLabel));
            OnPropertyChanged(nameof(XpAusArbeitLabel));
            OnPropertyChanged(nameof(XpAusSonderLabel));
            OnPropertyChanged(nameof(XpAusStreakLabel));
            OnPropertyChanged(nameof(XpAusAchievementsLabel));
            OnPropertyChanged(nameof(XpAusJubilaeumLabel));
        }
    }
}
