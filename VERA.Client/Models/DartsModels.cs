using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VERA.Models
{
    public enum DartsMode { X01_301, X01_501, X01_701, Cricket, AroundTheClock }
    public enum DartsGameState { Setup, Playing, Finished }

    public static class DartsConst
    {
        // Cricket-Nummern in Anzeigereihenfolge (20 oben, Bull unten)
        public static readonly int[] CricketNumbers = { 20, 19, 18, 17, 16, 15, 25 };
    }

    // ── Setup-Spieler (Name editierbar) ──────────────────────────────────────
    public class DartsPlayerSetup : INotifyPropertyChanged
    {
        private string _name;
        public string Name { get => _name; set => Set(ref _name, value); }
        public DartsPlayerSetup(string name) => _name = name;
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
        {
            if (EqualityComparer<T>.Default.Equals(f, v)) return;
            f = v;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }
    }

    // ── In-Game-Spieler ───────────────────────────────────────────────────────
    public class DartsPlayerVm : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int    _score;
        private int    _legs;
        private bool   _isActive;
        private int    _atcTarget      = 1;
        private int    _cricketPoints;

        public string Name { get => _name; set => Set(ref _name, value); }
        public int Score
        {
            get => _score;
            set { Set(ref _score, value); OnPropertyChanged(nameof(ScoreDisplay)); }
        }
        public int Legs
        {
            get => _legs;
            set { Set(ref _legs, value); OnPropertyChanged(nameof(LegsDisplay)); OnPropertyChanged(nameof(HasLegs)); }
        }
        public bool IsActive { get => _isActive; set => Set(ref _isActive, value); }

        public int DartsThrown { get; set; }
        public int TotalScored  { get; set; }
        public int PreviousScore { get; set; }

        public string ScoreDisplay => Score.ToString();
        public string LegsDisplay  => $"Legs: {Legs}";
        public bool   HasLegs      => _legs > 0;

        // Around the Clock
        public int AtcTarget
        {
            get => _atcTarget;
            set { Set(ref _atcTarget, value); OnPropertyChanged(nameof(AtcTargetDisplay)); OnPropertyChanged(nameof(AtcFinished)); }
        }
        public string AtcTargetDisplay => AtcFinished ? "\u2713" : AtcTarget.ToString();
        public bool   AtcFinished      => AtcTarget > 20;
        public string AtcProgressDisplay => AtcFinished ? "Fertig!" : $"Ziel: {AtcTarget}";

        // Cricket
        public int CricketPoints
        {
            get => _cricketPoints;
            set { Set(ref _cricketPoints, value); OnPropertyChanged(nameof(CricketPointsDisplay)); }
        }
        public string CricketPointsDisplay => _cricketPoints.ToString();

        // Marks pro Nummer (0 = offen … 3 = geschlossen)
        public Dictionary<int, int> CricketMarks { get; } = new()
        {
            [20] = 0, [19] = 0, [18] = 0, [17] = 0, [16] = 0, [15] = 0, [25] = 0
        };

        public bool CricketAllClosed() =>
            DartsConst.CricketNumbers.All(n => CricketMarks[n] >= 3);

        // Statistik-Anzeige (für Ergebnis-Panel)
        public string AvgDisplay
        {
            get
            {
                if (DartsThrown == 0) return "-";
                double visits = DartsThrown / 3.0;
                return $"\u00D8 {TotalScored / visits:F1}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        private bool Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
        {
            if (EqualityComparer<T>.Default.Equals(f, v)) return false;
            f = v; OnPropertyChanged(n); return true;
        }
    }

    // ── Cricket-Board-Zelle (eine pro Spieler pro Nummer) ─────────────────────
    public class CricketCellVm : INotifyPropertyChanged
    {
        private string _marks    = string.Empty;
        private bool   _isClosed;

        public string Marks    { get => _marks;    set => Set(ref _marks,    value); }
        public bool   IsClosed { get => _isClosed; set => Set(ref _isClosed, value); }

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
        {
            if (EqualityComparer<T>.Default.Equals(f, v)) return false;
            f = v;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
            return true;
        }
    }

    // ── Cricket-Board-Zeile (eine pro Nummer) ─────────────────────────────────
    public class CricketRowVm
    {
        public int    Number        { get; set; }
        public string NumberDisplay => Number == 25 ? "Bull" : Number.ToString();
        public ObservableCollection<CricketCellVm> Cells { get; } = new();
    }
}
