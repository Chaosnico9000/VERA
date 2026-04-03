namespace VERA.Models
{
    public enum EntryType
    {
        Arbeit,
        UrlaubGanztag,
        UrlaubHalbtag,
        Feiertag
    }

    public class TimeEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public EntryType Type { get; set; } = EntryType.Arbeit;

        public bool IsRunning => EndTime == null && Type == EntryType.Arbeit;

        public bool IsSonderTag => Type != EntryType.Arbeit;

        public TimeSpan Duration => IsRunning
            ? DateTime.Now - StartTime
            : Type == EntryType.UrlaubHalbtag
                ? TimeSpan.FromHours(Preferences.Default.Get("sollzeit_stunden", 8.0) / 2)
                : Type is EntryType.UrlaubGanztag or EntryType.Feiertag
                    ? TimeSpan.FromHours(Preferences.Default.Get("sollzeit_stunden", 8.0))
                    : EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;

        public string DurationDisplay
        {
            get
            {
                if (Type == EntryType.UrlaubGanztag) return "Ganztags";
                if (Type == EntryType.UrlaubHalbtag) return "Halbtags";
                if (Type == EntryType.Feiertag) return "Feiertag";
                if (IsRunning) return "Läuft…";
                if (!EndTime.HasValue) return "–";
                var d = EndTime.Value - StartTime;
                if (d.TotalHours >= 1)
                    return $"{(int)d.TotalHours}h {d.Minutes:D2}m";
                return $"{(int)d.TotalMinutes}m";
            }
        }

        // Für den Verlauf: Zeitspanne als lesbarer Text (z.B. "08:00 – 16:30")
        public string TimeRangeDisplay
        {
            get
            {
                if (IsSonderTag) return StartTime.ToString("dd.MM.yyyy");
                if (IsRunning) return $"{StartTime:HH:mm} – läuft";
                if (!EndTime.HasValue) return $"{StartTime:HH:mm}";
                return $"{StartTime:HH:mm} – {EndTime.Value:HH:mm}";
            }
        }

        public string StartTimeDisplay
        {
            get
            {
                if (IsSonderTag)
                {
                    if (StartTime.Date == DateTime.Today) return "Heute";
                    if (StartTime.Date == DateTime.Today.AddDays(-1)) return "Gestern";
                    return StartTime.ToString("dd.MM.yyyy");
                }
                if (StartTime.Date == DateTime.Today)
                    return $"Heute, {StartTime:HH:mm}";
                if (StartTime.Date == DateTime.Today.AddDays(-1))
                    return $"Gestern, {StartTime:HH:mm}";
                return StartTime.ToString("dd.MM.yyyy, HH:mm");
            }
        }

        // Badge-Text und Farbe je nach Typ
        public string TypeBadge => Type switch
        {
            EntryType.UrlaubGanztag => "🏖 Urlaub",
            EntryType.UrlaubHalbtag => "🌤 Urlaub ½",
            EntryType.Feiertag      => "🎉 Feiertag",
            _                       => string.Empty
        };

        public bool HasTypeBadge => Type != EntryType.Arbeit;
    }
}
