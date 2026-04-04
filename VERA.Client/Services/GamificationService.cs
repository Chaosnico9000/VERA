using VERA.Models;

namespace VERA.Services
{
    // ── Rang: je 6 Level eine Stufe ────────────────────────────────────────────
    public record Rang(string Titel, string Farbe, string Emoji);

    // ── Achievement ────────────────────────────────────────────────────────────
    public record Achievement(string Id, string Emoji, string Titel, string Beschreibung, bool Erreicht);

    // ── Ergebnis-DTO ───────────────────────────────────────────────────────────
    public class GamificationResult
    {
        public long   TotalXP               { get; set; }
        public int    Level                 { get; set; }
        public long   XPInLevel             { get; set; }
        public long   XPForNextLevel        { get; set; }
        public double LevelProgress         { get; set; }
        public int    Streak                { get; set; }
        public int    BesterStreak          { get; set; }
        public int    GesamtArbeitstage     { get; set; }
        public int    GesamtUeberstundenMin { get; set; }
        public string LevelTitle            { get; set; } = string.Empty;
        public string LevelEmoji            { get; set; } = string.Empty;
        public Rang   AktuellerRang         { get; set; } = null!;
        public bool   NewLevelUp            { get; set; }
        public bool   NewStreakMilestone    { get; set; }
        public bool   NewAchievement        { get; set; }
        public List<Achievement> Achievements { get; set; } = [];
        public long XpAusArbeit             { get; set; }
        public long XpAusSonderTage         { get; set; }
        public long XpAusStreak             { get; set; }
        public long XpAusAchievements       { get; set; }
        public long XpAusJubilaeum          { get; set; }
    }

    public class GamificationService
    {
        // ── 30 Level ───────────────────────────────────────────────────────────
        private static readonly (string Titel, string Emoji)[] Levels =
        [
            /* 00 */ ("Frischling",       "🌱"),
            /* 01 */ ("Einsteiger",       "🚪"),
            /* 02 */ ("Azubi",            "📋"),
            /* 03 */ ("Praktikant",       "☕"),
            /* 04 */ ("Werkstudie",       "📚"),
            /* 05 */ ("Junior",           "💡"),
            /* 06 */ ("Kollegin",         "💼"),
            /* 07 */ ("Fachkraft",        "🔧"),
            /* 08 */ ("Spezialist",       "🔬"),
            /* 09 */ ("Profi",            "⚡"),
            /* 10 */ ("Senior",           "🎯"),
            /* 11 */ ("Teamplayer",       "🤝"),
            /* 12 */ ("Koordinator",      "📊"),
            /* 13 */ ("Stratege",         "♟️"),
            /* 14 */ ("Analyst",          "🧮"),
            /* 15 */ ("Experte",          "🏅"),
            /* 16 */ ("Veteran",          "🎖️"),
            /* 17 */ ("Mentor",           "🎓"),
            /* 18 */ ("Architekt",        "🏗️"),
            /* 19 */ ("Innovator",        "💎"),
            /* 20 */ ("Meister",          "🏆"),
            /* 21 */ ("Hauptmeister",     "⚜️"),
            /* 22 */ ("Großmeister",      "🌟"),
            /* 23 */ ("Champion",         "🥇"),
            /* 24 */ ("Elite",            "👑"),
            /* 25 */ ("Legende",          "🔥"),
            /* 26 */ ("Ikone",            "✨"),
            /* 27 */ ("Mythisch",         "🌌"),
            /* 28 */ ("Transcendent",     "⚡🔥"),
            /* 29 */ ("VERA Pro",         "🚀"),
        ];

        // ── 5 Ränge (je 6 Level) ───────────────────────────────────────────────
        private static readonly Rang[] Raenge =
        [
            new("Einsteiger",        "#8FA0DC", "🌱"),   // Level 0–5
            new("Fortgeschrittener", "#3566E5", "💼"),   // Level 6–11
            new("Experte",           "#4ECCA3", "🎯"),   // Level 12–17
            new("Meister",           "#FFB347", "🏆"),   // Level 18–23
            new("Legende",           "#8240CE", "🔥"),   // Level 24–29
        ];

        // ── XP-Kurve: exponentiell ─────────────────────────────────────────────
        // Level  0→1:   2.000 XP
        // Level  9→10: ~35.000 XP
        // Level 19→20: ~320.000 XP
        // Level 28→29: ~2.800.000 XP
        // Gesamt bis Level 29: ~8 Mio XP  ≈ ~4 Jahre tägliche Arbeit
        private static long XPForLevel(int toLevel)
        {
            if (toLevel <= 0) return 0;
            return (long)(2000.0 * Math.Pow(1.55, toLevel - 1));
        }

        private static long CumulativeXP(int level)
        {
            long sum = 0;
            for (int i = 1; i <= level; i++) sum += XPForLevel(i);
            return sum;
        }

        // ── Hauptberechnung ────────────────────────────────────────────────────
        public GamificationResult Calculate(List<TimeEntry> entries)
        {
            var sollStunden = Preferences.Default.Get("sollzeit_stunden", 8.0);
            var sollMin     = sollStunden * 60;
            var rawStart    = Preferences.Default.Get("erster_arbeitstag", "2026-04-01");
            var ersterTag   = DateTime.TryParse(rawStart, out var ed) ? ed.Date : new DateTime(2026, 4, 1);
            var heute       = DateTime.Today;

            var relevant = entries.Where(e => e.StartTime.Date >= ersterTag && e.StartTime.Date <= heute).ToList();

            // ── 1. Tageweise XP ───────────────────────────────────────────────
            long xpArbeit = 0, xpSonder = 0;
            int arbeitstage = 0, ueberstundenMinGesamt = 0;

            var byDay = relevant
                .GroupBy(e => e.StartTime.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (day, tagesEintraege) in byDay)
            {
                var istSonder = tagesEintraege.All(e => e.IsSonderTag);
                var minuten   = tagesEintraege.Sum(e => e.Duration.TotalMinutes);

                if (istSonder)
                {
                    xpSonder += 150;
                    continue;
                }

                arbeitstage++;
                var effMin = Math.Min(minuten, sollMin);

                // Basis: 10 XP pro gearbeiteter Minute (bis Sollzeit)
                xpArbeit += (long)(effMin * 10);

                // Bonus: Sollzeit exakt erreicht
                if (minuten >= sollMin) xpArbeit += 500;

                // Überstunden: 4 XP/Min extra, max 480 Bonus
                var extra = minuten - sollMin;
                if (extra > 0)
                {
                    xpArbeit += (long)Math.Min(extra * 4, 480);
                    ueberstundenMinGesamt += (int)extra;
                }

                // Pünktlichkeits-Bonus: 200 XP wenn >= 95% der Sollzeit
                if (minuten >= sollMin * 0.95 && minuten < sollMin)
                    xpArbeit += 200;
            }

            // ── 2. Wochenstreak ───────────────────────────────────────────────
            int streak       = CalculateStreak(relevant, sollMin, ersterTag);
            int besterStreak = Preferences.Default.Get("vera_best_streak", 0);
            if (streak > besterStreak)
            {
                besterStreak = streak;
                Preferences.Default.Set("vera_best_streak", besterStreak);
            }

            long xpStreak = streak * 1000L;
            if (streak >= 52)      xpStreak += 50_000;
            else if (streak >= 26) xpStreak += 15_000;
            else if (streak >= 13) xpStreak += 5_000;
            else if (streak >= 4)  xpStreak += 1_000;

            // ── 3. Jahres-Jubiläum-Bonus ─────────────────────────────────────
            long xpJubilaeum = 0;
            for (int jahr = 1; ersterTag.AddYears(jahr) <= heute; jahr++)
                xpJubilaeum += 25_000L * jahr;

            // ── 4. Achievements ───────────────────────────────────────────────
            var (achievements, xpAchiev) = CalculateAchievements(
                arbeitstage, streak, besterStreak, ueberstundenMinGesamt,
                byDay, sollMin, ersterTag, heute);

            // ── 5. Gesamt-XP ──────────────────────────────────────────────────
            long totalXP = xpArbeit + xpSonder + xpStreak + xpJubilaeum + xpAchiev;

            // ── 6. Level bestimmen ────────────────────────────────────────────
            int maxLevel = Levels.Length - 1;
            int level    = 0;
            while (level < maxLevel && totalXP >= CumulativeXP(level + 1))
                level++;

            long xpBisHierher     = CumulativeXP(level);
            long xpInLevel        = totalXP - xpBisHierher;
            long xpFuerNaechstes  = level < maxLevel ? XPForLevel(level + 1) : 1;
            double progress       = level >= maxLevel ? 1.0 : Math.Min(1.0, (double)xpInLevel / xpFuerNaechstes);

            // ── 7. Level-Up / Milestone erkennen ─────────────────────────────
            var prevLevel     = Preferences.Default.Get("vera_level_last",  0);
            var prevAchiev    = Preferences.Default.Get("vera_achiev_count", 0);
            int newAchievCount = achievements.Count(a => a.Erreicht);

            Preferences.Default.Set("vera_level_last",   level);
            Preferences.Default.Set("vera_xp_last",      (int)Math.Min(totalXP, int.MaxValue));
            Preferences.Default.Set("vera_achiev_count", newAchievCount);

            int rangIndex = Math.Clamp(level / 6, 0, Raenge.Length - 1);

            return new GamificationResult
            {
                TotalXP               = totalXP,
                Level                 = level,
                XPInLevel             = xpInLevel,
                XPForNextLevel        = xpFuerNaechstes,
                LevelProgress         = progress,
                Streak                = streak,
                BesterStreak          = besterStreak,
                GesamtArbeitstage     = arbeitstage,
                GesamtUeberstundenMin = ueberstundenMinGesamt,
                LevelTitle            = Levels[level].Titel,
                LevelEmoji            = Levels[level].Emoji,
                AktuellerRang         = Raenge[rangIndex],
                NewLevelUp            = level > prevLevel,
                NewStreakMilestone    = streak > 0 && (streak % 4 == 0 || streak == 52),
                NewAchievement        = newAchievCount > prevAchiev,
                Achievements          = achievements,
                XpAusArbeit           = xpArbeit,
                XpAusSonderTage       = xpSonder,
                XpAusStreak           = xpStreak,
                XpAusAchievements     = xpAchiev,
                XpAusJubilaeum        = xpJubilaeum,
            };
        }

        // ── Achievements ──────────────────────────────────────────────────────
        private static (List<Achievement> list, long xp) CalculateAchievements(
            int arbeitstage, int streak, int besterStreak, int ueberstundenMin,
            Dictionary<DateTime, List<TimeEntry>> byDay, double sollMin,
            DateTime ersterTag, DateTime heute)
        {
            var list = new List<Achievement>();
            long xp = 0;

            Achievement Make(string id, string emoji, string titel, string beschr, bool err, long bonus)
            {
                if (err) xp += bonus;
                return new Achievement(id, emoji, titel, beschr, err);
            }

            // Arbeitstage-Meilensteine
            list.Add(Make("tage_10",    "📅", "Erste 10 Tage",    "10 Arbeitstage erfasst",        arbeitstage >=   10,   2_000));
            list.Add(Make("tage_50",    "📅", "50 Tage",          "50 Arbeitstage erfasst",        arbeitstage >=   50,   8_000));
            list.Add(Make("tage_100",   "🗓️", "100 Tage",         "100 Arbeitstage erfasst",       arbeitstage >=  100,  25_000));
            list.Add(Make("tage_250",   "🗓️", "Viertel Jahr+",    "250 Arbeitstage erfasst",       arbeitstage >=  250,  75_000));
            list.Add(Make("tage_500",   "🏅", "500 Tage",         "500 Arbeitstage – Halbzeit!",   arbeitstage >=  500, 200_000));
            list.Add(Make("tage_1000",  "🏆", "Tausend Tage",     "1.000 Arbeitstage – Legende!",  arbeitstage >= 1000, 500_000));

            // Streak-Meilensteine
            list.Add(Make("streak_4",   "⚡", "Erster Monat",     "4 Wochen Streak",               besterStreak >=  4,   3_000));
            list.Add(Make("streak_13",  "🔥", "Quartalsheld",     "13 Wochen ohne Lücke",          besterStreak >= 13,  20_000));
            list.Add(Make("streak_26",  "🔥🔥","Halbjahrsheld",   "26 Wochen Streak",              besterStreak >= 26,  60_000));
            list.Add(Make("streak_52",  "👑", "Jahreskönig",      "52 Wochen – ein ganzes Jahr!",  besterStreak >= 52, 200_000));

            // Überstunden-Meilensteine
            int ueberstundenStd = ueberstundenMin / 60;
            list.Add(Make("ue_10h",     "⏱️", "10h Bonus",        "10h Überstunden gesammelt",     ueberstundenStd >=   10,   1_500));
            list.Add(Make("ue_100h",    "⏰", "100h Bonus",       "100h Überstunden – Respekt!",   ueberstundenStd >=  100,  15_000));
            list.Add(Make("ue_500h",    "🕐", "500h Bonus",       "500h Überstunden – Wahnsinn!",  ueberstundenStd >=  500,  80_000));

            // Jubiläum
            bool ein  = ersterTag.AddYears(1) <= heute;
            bool zwei = ersterTag.AddYears(2) <= heute;
            bool drei = ersterTag.AddYears(3) <= heute;
            list.Add(Make("jubil_1",    "🎂",   "1 Jahr",          "Seit 1 Jahr dabei!",            ein,    25_000));
            list.Add(Make("jubil_2",    "🎂🎂", "2 Jahre",         "Seit 2 Jahren dabei!",          zwei,   75_000));
            list.Add(Make("jubil_3",    "🎂🎂🎂","3 Jahre",        "Seit 3 Jahren dabei!",          drei,  150_000));

            // Sonderleistungen
            bool vollMonat = CheckVollerMonat(byDay, sollMin, heute);
            list.Add(Make("voll_monat", "🌕", "Perfekter Monat",  "Jeden Werktag Sollzeit erreicht", vollMonat, 30_000));

            bool fruehaufsteher = byDay.Values.SelectMany(x => x)
                .Any(e => e.Type == EntryType.Arbeit && e.StartTime.Hour < 7);
            list.Add(Make("early",      "🌅", "Frühaufsteher",    "Vor 07:00 Uhr gestartet",       fruehaufsteher, 2_000));

            bool nachtschicht = byDay.Values.SelectMany(x => x)
                .Any(e => e.Type == EntryType.Arbeit && e.EndTime.HasValue && e.EndTime.Value.Hour >= 20);
            list.Add(Make("late",       "🌙", "Nachtschicht",     "Bis nach 20:00 Uhr gearbeitet", nachtschicht, 2_000));

            return (list, xp);
        }

        private static bool CheckVollerMonat(Dictionary<DateTime, List<TimeEntry>> byDay, double sollMin, DateTime heute)
        {
            var ms = new DateTime(heute.Year, heute.Month, 1).AddMonths(-1);
            var me = ms.AddMonths(1).AddDays(-1);
            for (var d = ms; d <= me; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday) continue;
                if (!byDay.TryGetValue(d, out var eintraege)) return false;
                var min = eintraege.Sum(e => e.Duration.TotalMinutes);
                if (min < sollMin * 0.95) return false;
            }
            return true;
        }

        // ── Wochenstreak ──────────────────────────────────────────────────────
        private static int CalculateStreak(List<TimeEntry> entries, double sollMin, DateTime ersterTag)
        {
            var heute  = DateTime.Today;
            int streak = 0;

            for (int w = 0; w < 260; w++) // max 5 Jahre zurück
            {
                var refDay    = heute.AddDays(-w * 7);
                var dow       = (int)refDay.DayOfWeek;
                var ws        = refDay.AddDays(dow == 0 ? -6 : -(dow - 1));
                var we        = ws.AddDays(4);

                if (we < ersterTag) break;

                var weekEnd   = w == 0 ? heute : we;
                var weekStart = ws < ersterTag ? ersterTag : ws;

                var weekMin = entries
                    .Where(e => e.StartTime.Date >= weekStart && e.StartTime.Date <= weekEnd)
                    .Sum(e => e.Duration.TotalMinutes);

                int sollTage = 0;
                for (var d = weekStart; d <= weekEnd; d = d.AddDays(1))
                    if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                        sollTage++;

                var wochensoll = sollTage * sollMin;
                if (wochensoll <= 0) { streak++; continue; }

                if (weekMin >= wochensoll * 0.9)
                    streak++;
                else
                    break;
            }

            return streak;
        }
    }
}
