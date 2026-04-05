using System.Collections.ObjectModel;
using System.Windows.Input;
using VERA.Models;

namespace VERA.ViewModels
{
    public class DartsViewModel : BaseViewModel
    {
        // ── State ─────────────────────────────────────────────────────────────
        private DartsGameState _gameState = DartsGameState.Setup;
        private DartsMode      _selectedMode = DartsMode.X01_501;
        private int            _legsToWin = 2;
        private int            _currentPlayerIndex;
        private string         _currentInput     = string.Empty;
        private int            _currentDartsThrown;
        private string         _checkoutHint     = string.Empty;
        private string         _winnerName       = string.Empty;
        private string         _turnMessage      = string.Empty;
        private int            _selectedMultiplier = 1;

        // ── Collections ───────────────────────────────────────────────────────
        public ObservableCollection<DartsPlayerSetup> SetupPlayers { get; } = new()
        {
            new DartsPlayerSetup("Spieler 1"),
            new DartsPlayerSetup("Spieler 2")
        };

        public ObservableCollection<DartsPlayerVm>  Players     { get; } = new();
        public ObservableCollection<CricketRowVm>   CricketBoard { get; } = new();

        // ── Checkout hints (170 → 2) ──────────────────────────────────────────
        private static readonly Dictionary<int, string> CheckoutTable = new()
        {
            [170] = "T20 T20 Bull",    [167] = "T20 T19 Bull",    [164] = "T20 T18 Bull",
            [161] = "T20 T17 Bull",    [160] = "T20 T20 D20",     [158] = "T20 T20 D19",
            [157] = "T20 T19 D20",     [156] = "T20 T20 D18",     [155] = "T20 T19 D19",
            [154] = "T20 T18 D20",     [153] = "T20 T19 D18",     [152] = "T20 T20 D16",
            [151] = "T20 T17 D20",     [150] = "T20 T18 D18",     [149] = "T20 T19 D16",
            [148] = "T20 T20 D14",     [147] = "T20 T17 D18",     [146] = "T20 T18 D16",
            [145] = "T20 T19 D14",     [144] = "T20 T20 D12",     [143] = "T20 T17 D16",
            [142] = "T20 T14 D20",     [141] = "T20 T19 D12",     [140] = "T20 T20 D10",
            [139] = "T20 T13 D20",     [138] = "T20 T18 D12",     [137] = "T20 T19 D10",
            [136] = "T20 T20 D8",      [135] = "T20 T17 D12",     [134] = "T20 T14 D16",
            [133] = "T20 T19 D8",      [132] = "T20 T16 D12",     [131] = "T20 T13 D16",
            [130] = "T20 T20 D5",      [129] = "T19 T16 D12",     [128] = "T20 T20 D4",
            [127] = "T20 T17 D8",      [126] = "T19 T19 D6",      [125] = "T20 T19 D4",
            [124] = "T20 T16 D8",      [123] = "T19 T16 D9",      [122] = "T18 T18 D7",
            [121] = "T20 T11 D14",     [120] = "T20 S20 D20",     [119] = "T19 T12 D13",
            [118] = "T20 S18 D20",     [117] = "T20 S17 D20",     [116] = "T20 S16 D20",
            [115] = "T20 S15 D20",     [114] = "T20 S14 D20",     [113] = "T20 S13 D20",
            [112] = "T20 S12 D20",     [111] = "T20 S11 D20",     [110] = "T20 S10 D20",
            [109] = "T20 S9 D20",      [108] = "T20 S8 D20",      [107] = "T20 S7 D20",
            [106] = "T20 S6 D20",      [105] = "T20 S5 D20",      [104] = "T20 S4 D20",
            [103] = "T20 S3 D20",      [102] = "T20 S2 D20",      [101] = "T20 S1 D20",
            [100] = "T20 D20",         [99]  = "T19 S10 D16",     [98]  = "T20 D19",
            [97]  = "T19 D20",         [96]  = "T20 D18",         [95]  = "T19 D19",
            [94]  = "T18 D20",         [93]  = "T19 D18",         [92]  = "T20 D16",
            [91]  = "T17 D20",         [90]  = "T18 D18",         [89]  = "T19 D16",
            [88]  = "T20 D14",         [87]  = "T17 D18",         [86]  = "T18 D16",
            [85]  = "T15 D20",         [84]  = "T20 D12",         [83]  = "T17 D16",
            [82]  = "T14 D20",         [81]  = "T19 D12",         [80]  = "T20 D10",
            [79]  = "T13 D20",         [78]  = "T18 D12",         [77]  = "T19 D10",
            [76]  = "T20 D8",          [75]  = "T17 D12",         [74]  = "T14 D16",
            [73]  = "T19 D8",          [72]  = "T16 D12",         [71]  = "T13 D16",
            [70]  = "T18 D8",          [69]  = "T19 D6",          [68]  = "T20 D4",
            [67]  = "T17 D8",          [66]  = "T10 D18",         [65]  = "T19 D4",
            [64]  = "T16 D8",          [63]  = "T13 D12",         [62]  = "T10 D16",
            [61]  = "T15 D8",          [60]  = "S20 D20",         [59]  = "S19 D20",
            [58]  = "S18 D20",         [57]  = "S17 D20",         [56]  = "T16 D4",
            [55]  = "S15 D20",         [54]  = "S14 D20",         [53]  = "S13 D20",
            [52]  = "S12 D20",         [51]  = "S11 D20",         [50]  = "S10 D20",
            [49]  = "S9 D20",          [48]  = "S8 D20",          [47]  = "S15 D16",
            [46]  = "S6 D20",          [45]  = "S5 D20",          [44]  = "S4 D20",
            [43]  = "S3 D20",          [42]  = "S10 D16",         [41]  = "S9 D16",
            [40]  = "D20",             [39]  = "S7 D16",          [38]  = "D19",
            [37]  = "S5 D16",          [36]  = "D18",             [35]  = "S3 D16",
            [34]  = "D17",             [33]  = "S1 D16",          [32]  = "D16",
            [31]  = "S15 D8",          [30]  = "D15",             [29]  = "S13 D8",
            [28]  = "D14",             [27]  = "S11 D8",          [26]  = "D13",
            [25]  = "S9 D8",           [24]  = "D12",             [23]  = "S7 D8",
            [22]  = "D11",             [21]  = "S5 D8",           [20]  = "D10",
            [19]  = "S3 D8",           [18]  = "D9",              [17]  = "S1 D8",
            [16]  = "D8",              [15]  = "S7 D4",           [14]  = "D7",
            [13]  = "S5 D4",           [12]  = "D6",              [11]  = "S3 D4",
            [10]  = "D5",              [9]   = "S1 D4",           [8]   = "D4",
            [7]   = "S3 D2",           [6]   = "D3",              [5]   = "S1 D2",
            [4]   = "D2",              [3]   = "S1 D1",           [2]   = "D1",
        };

        // ── Computed properties ───────────────────────────────────────────────
        public DartsGameState GameState
        {
            get => _gameState;
            set
            {
                if (!SetProperty(ref _gameState, value)) return;
                OnPropertyChanged(nameof(IsSetup));
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(IsFinished));
                OnPropertyChanged(nameof(IsX01Playing));
                OnPropertyChanged(nameof(IsCricketPlaying));
                OnPropertyChanged(nameof(IsAtcPlaying));
            }
        }

        public bool IsSetup    => GameState == DartsGameState.Setup;
        public bool IsPlaying  => GameState == DartsGameState.Playing;
        public bool IsFinished => GameState == DartsGameState.Finished;
        public bool IsX01Playing  => IsPlaying && (_selectedMode == DartsMode.X01_301 || _selectedMode == DartsMode.X01_501 || _selectedMode == DartsMode.X01_701);
        public bool IsCricketPlaying => IsPlaying && _selectedMode == DartsMode.Cricket;
        public bool IsAtcPlaying     => IsPlaying && _selectedMode == DartsMode.AroundTheClock;

        public DartsMode SelectedMode
        {
            get => _selectedMode;
            set
            {
                if (!SetProperty(ref _selectedMode, value)) return;
                OnPropertyChanged(nameof(IsMode301));
                OnPropertyChanged(nameof(IsMode501));
                OnPropertyChanged(nameof(IsMode701));
                OnPropertyChanged(nameof(IsModeCricket));
                OnPropertyChanged(nameof(IsModeAtc));
            }
        }

        public bool IsMode301    => _selectedMode == DartsMode.X01_301;
        public bool IsMode501    => _selectedMode == DartsMode.X01_501;
        public bool IsMode701    => _selectedMode == DartsMode.X01_701;
        public bool IsModeCricket => _selectedMode == DartsMode.Cricket;
        public bool IsModeAtc    => _selectedMode == DartsMode.AroundTheClock;

        public int LegsToWin
        {
            get => _legsToWin;
            set { SetProperty(ref _legsToWin, Math.Max(1, value)); OnPropertyChanged(nameof(LegsDisplay)); }
        }
        public string LegsDisplay => $"Legs: {_legsToWin}";

        public string CurrentInput
        {
            get => _currentInput;
            private set { SetProperty(ref _currentInput, value); OnPropertyChanged(nameof(InputDisplay)); }
        }
        public string InputDisplay => string.IsNullOrEmpty(_currentInput) ? "0" : _currentInput;

        public string CheckoutHint
        {
            get => _checkoutHint;
            private set { SetProperty(ref _checkoutHint, value); OnPropertyChanged(nameof(HasCheckoutHint)); }
        }
        public bool HasCheckoutHint => !string.IsNullOrEmpty(_checkoutHint);

        public string WinnerName
        {
            get => _winnerName;
            private set => SetProperty(ref _winnerName, value);
        }

        public string TurnMessage
        {
            get => _turnMessage;
            private set => SetProperty(ref _turnMessage, value);
        }

        public int SelectedMultiplier
        {
            get => _selectedMultiplier;
            private set
            {
                SetProperty(ref _selectedMultiplier, value);
                OnPropertyChanged(nameof(IsMult1));
                OnPropertyChanged(nameof(IsMult2));
                OnPropertyChanged(nameof(IsMult3));
            }
        }
        public bool IsMult1 => _selectedMultiplier == 1;
        public bool IsMult2 => _selectedMultiplier == 2;
        public bool IsMult3 => _selectedMultiplier == 3;

        public string DartCountDisplay => $"Dart {_currentDartsThrown + 1}/3";

        public DartsPlayerVm? CurrentPlayer =>
            Players.Count > 0 && _currentPlayerIndex < Players.Count
                ? Players[_currentPlayerIndex]
                : null;

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand SetModeCommand        { get; }
        public ICommand AddPlayerCommand      { get; }
        public ICommand IncrLegsCommand       { get; }
        public ICommand DecrLegsCommand       { get; }
        public ICommand StartGameCommand      { get; }
        public ICommand NumpadCommand         { get; }
        public ICommand QuickScoreCommand     { get; }
        public ICommand CricketHitCommand     { get; }
        public ICommand CricketMissCommand    { get; }
        public ICommand SelectMultiplierCommand { get; }
        public ICommand EndCricketTurnCommand { get; }
        public ICommand AtcHitCommand         { get; }
        public ICommand AtcMissCommand        { get; }
        public ICommand UndoCommand           { get; }
        public ICommand NewGameCommand        { get; }

        public DartsViewModel()
        {
            SetModeCommand         = new Command<string>(OnSetMode);
            AddPlayerCommand       = new Command(OnAddPlayer);
            IncrLegsCommand        = new Command(() => LegsToWin++);
            DecrLegsCommand        = new Command(() => LegsToWin--);
            StartGameCommand       = new Command(OnStartGame, () => SetupPlayers.Count >= 2);
            NumpadCommand          = new Command<string>(OnNumpad);
            QuickScoreCommand      = new Command<string>(OnQuickScore);
            CricketHitCommand      = new Command<string>(OnCricketHit);
            CricketMissCommand     = new Command(OnCricketMiss);
            SelectMultiplierCommand = new Command<string>(s => SelectedMultiplier = int.Parse(s));
            EndCricketTurnCommand  = new Command(OnEndCricketTurn);
            AtcHitCommand          = new Command(OnAtcHit);
            AtcMissCommand         = new Command(OnAtcMiss);
            UndoCommand            = new Command(OnUndo, () => IsX01Playing);
            NewGameCommand         = new Command(OnNewGame);
        }

        // ── Setup actions ─────────────────────────────────────────────────────
        private void OnSetMode(string mode)
        {
            if (Enum.TryParse<DartsMode>(mode, out var m))
                SelectedMode = m;
        }

        private void OnAddPlayer()
        {
            if (SetupPlayers.Count >= 8) return;
            SetupPlayers.Add(new DartsPlayerSetup($"Spieler {SetupPlayers.Count + 1}"));
            ((Command)StartGameCommand).ChangeCanExecute();
        }

        // ── Start game ────────────────────────────────────────────────────────
        private void OnStartGame()
        {
            Players.Clear();
            CricketBoard.Clear();
            _currentPlayerIndex = 0;
            _currentDartsThrown = 0;
            _currentInput       = string.Empty;
            _checkoutHint       = string.Empty;
            SelectedMultiplier  = 1;

            int startScore = _selectedMode switch
            {
                DartsMode.X01_301 => 301,
                DartsMode.X01_701 => 701,
                _                 => 501
            };

            foreach (var setup in SetupPlayers)
            {
                Players.Add(new DartsPlayerVm
                {
                    Name  = string.IsNullOrWhiteSpace(setup.Name) ? "Spieler" : setup.Name,
                    Score = startScore,
                    Legs  = 0
                });
            }

            if (_selectedMode == DartsMode.Cricket)
                BuildCricketBoard();

            Players[0].IsActive = true;
            UpdateTurnMessage();
            GameState = DartsGameState.Playing;
            OnPropertyChanged(nameof(CurrentPlayer));
            OnPropertyChanged(nameof(DartCountDisplay));
        }

        // ── X01 numpad ────────────────────────────────────────────────────────
        private void OnNumpad(string key)
        {
            if (!IsX01Playing) return;
            switch (key)
            {
                case "DEL":
                    if (_currentInput.Length > 0)
                        CurrentInput = _currentInput[..^1];
                    break;
                case "OK":
                    if (int.TryParse(_currentInput, out int score))
                        ConfirmX01(score);
                    else
                        CurrentInput = string.Empty;
                    break;
                default:
                    if (_currentInput.Length < 3)
                        CurrentInput = _currentInput + key;
                    break;
            }
            UpdateCheckoutHint();
        }

        private void OnQuickScore(string s)
        {
            if (!IsX01Playing) return;
            if (int.TryParse(s, out int score))
                ConfirmX01(score);
        }

        private void ConfirmX01(int thrown)
        {
            var p = CurrentPlayer;
            if (p == null) return;

            int remaining = p.Score - thrown;

            // Bust
            if (remaining < 0 || remaining == 1)
            {
                TurnMessage = $"Bust! {p.Name} bleibt bei {p.Score}";
                CurrentInput = string.Empty;
                UpdateCheckoutHint();
                AdvanceToNextPlayer();
                return;
            }

            p.PreviousScore = p.Score;
            p.Score         = remaining;
            p.TotalScored  += thrown;
            p.DartsThrown  += 3;

            if (remaining == 0)
            {
                p.Legs++;
                if (p.Legs >= _legsToWin)
                {
                    WinGame(p.Name);
                    return;
                }
                ResetScoresForNewLeg();
                return;
            }

            CurrentInput = string.Empty;
            UpdateCheckoutHint();
            AdvanceToNextPlayer();
        }

        private void OnUndo()
        {
            if (!IsX01Playing) return;
            if (!string.IsNullOrEmpty(_currentInput)) { CurrentInput = string.Empty; return; }

            var p = CurrentPlayer;
            if (p?.PreviousScore > 0)
            {
                int restored = p.Score;
                p.Score       = p.PreviousScore;
                p.TotalScored -= (p.PreviousScore - restored);
                p.DartsThrown = Math.Max(0, p.DartsThrown - 3);
                p.PreviousScore = 0;
                UpdateCheckoutHint();
                UpdateTurnMessage();
            }
        }

        // ── Cricket ───────────────────────────────────────────────────────────
        private void BuildCricketBoard()
        {
            foreach (var num in DartsConst.CricketNumbers)
            {
                var row = new CricketRowVm { Number = num };
                foreach (var _ in Players)
                    row.Cells.Add(new CricketCellVm());
                CricketBoard.Add(row);
            }
        }

        private void OnCricketHit(string s)
        {
            if (!IsCricketPlaying || !int.TryParse(s, out int num)) return;
            var p = CurrentPlayer;
            if (p == null || !p.CricketMarks.ContainsKey(num)) return;

            int prev = p.CricketMarks[num];
            int next = Math.Min(prev + _selectedMultiplier, 9);
            p.CricketMarks[num] = next;

            // Scoring: only if not already closed by this player (excess marks)
            if (prev < 3)
            {
                int excess = Math.Max(0, prev + _selectedMultiplier - 3);
                bool anyOpen = Players.Any(op => op != p && op.CricketMarks[num] < 3);
                if (excess > 0 && anyOpen)
                    p.CricketPoints += num * excess;
            }

            OnCricketMiss(); // advance dart count
            RefreshCricketBoard();

            if (CheckCricketWin())
            {
                WinGame(p.Name);
                return;
            }
        }

        private void OnCricketMiss()
        {
            if (!IsCricketPlaying) return;
            _currentDartsThrown++;
            OnPropertyChanged(nameof(DartCountDisplay));

            if (_currentDartsThrown >= 3)
                OnEndCricketTurn();
        }

        private void OnEndCricketTurn()
        {
            if (!IsCricketPlaying) return;
            SelectedMultiplier = 1;
            AdvanceToNextPlayer();
        }

        private bool CheckCricketWin()
        {
            var p = CurrentPlayer;
            if (p == null || !p.CricketAllClosed()) return false;
            int maxOther = Players.Where(x => x != p).Select(x => x.CricketPoints).DefaultIfEmpty(0).Max();
            return p.CricketPoints >= maxOther;
        }

        private void RefreshCricketBoard()
        {
            foreach (var row in CricketBoard)
            {
                for (int i = 0; i < Players.Count && i < row.Cells.Count; i++)
                {
                    int marks    = Players[i].CricketMarks[row.Number];
                    bool closed  = marks >= 3;
                    row.Cells[i].IsClosed = closed;
                    row.Cells[i].Marks    = MarksDisplay(marks);
                }
            }
        }

        private static string MarksDisplay(int marks) => marks switch
        {
            0 => string.Empty,
            1 => "/",
            2 => "X",
            _ => "\u25CB"  // ○
        };

        // ── Around the Clock ──────────────────────────────────────────────────
        private void OnAtcHit()
        {
            if (!IsAtcPlaying) return;
            var p = CurrentPlayer;
            if (p == null) return;

            p.AtcTarget++;
            p.DartsThrown++;
            p.TotalScored++;
            OnPropertyChanged(nameof(CurrentPlayer));

            if (p.AtcFinished)
            {
                WinGame(p.Name);
                return;
            }

            _currentDartsThrown++;
            OnPropertyChanged(nameof(DartCountDisplay));
            if (_currentDartsThrown >= 3)
                AdvanceToNextPlayer();
            UpdateTurnMessage();
        }

        private void OnAtcMiss()
        {
            if (!IsAtcPlaying) return;
            _currentDartsThrown++;
            OnPropertyChanged(nameof(DartCountDisplay));
            if (_currentDartsThrown >= 3)
                AdvanceToNextPlayer();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void AdvanceToNextPlayer()
        {
            if (Players.Count == 0) return;
            Players[_currentPlayerIndex].IsActive = false;
            _currentPlayerIndex = (_currentPlayerIndex + 1) % Players.Count;
            Players[_currentPlayerIndex].IsActive = true;
            _currentDartsThrown = 0;
            CurrentInput = string.Empty;
            SelectedMultiplier = 1;
            UpdateCheckoutHint();
            UpdateTurnMessage();
            OnPropertyChanged(nameof(CurrentPlayer));
            OnPropertyChanged(nameof(DartCountDisplay));
        }

        private void UpdateTurnMessage()
        {
            var p = CurrentPlayer;
            if (p == null) return;
            TurnMessage = _selectedMode == DartsMode.AroundTheClock
                ? $"{p.Name} — Ziel: {p.AtcProgressDisplay}"
                : $"{p.Name} ist dran";
        }

        private void UpdateCheckoutHint()
        {
            if (!IsX01Playing) { CheckoutHint = string.Empty; return; }
            var p = CurrentPlayer;
            if (p == null) { CheckoutHint = string.Empty; return; }
            int remaining = p.Score;
            if (!string.IsNullOrEmpty(_currentInput) && int.TryParse(_currentInput, out int entered))
                remaining -= entered;
            CheckoutHint = (remaining >= 2 && remaining <= 170 && CheckoutTable.TryGetValue(remaining, out var hint))
                ? hint
                : string.Empty;
        }

        private void ResetScoresForNewLeg()
        {
            int startScore = _selectedMode switch
            {
                DartsMode.X01_301 => 301,
                DartsMode.X01_701 => 701,
                _                 => 501
            };
            foreach (var p in Players)
            {
                p.Score         = startScore;
                p.PreviousScore = 0;
                p.TotalScored   = 0;
                p.DartsThrown   = 0;
            }
            CurrentInput       = string.Empty;
            _currentDartsThrown = 0;
            SelectedMultiplier = 1;
            UpdateCheckoutHint();
            UpdateTurnMessage();
            OnPropertyChanged(nameof(DartCountDisplay));
        }

        private void WinGame(string winnerName)
        {
            WinnerName = winnerName;
            GameState  = DartsGameState.Finished;
        }

        private void OnNewGame()
        {
            Players.Clear();
            CricketBoard.Clear();
            _currentPlayerIndex = 0;
            _currentDartsThrown = 0;
            CurrentInput        = string.Empty;
            CheckoutHint        = string.Empty;
            WinnerName          = string.Empty;
            TurnMessage         = string.Empty;
            SelectedMultiplier  = 1;
            GameState           = DartsGameState.Setup;
            ((Command)UndoCommand).ChangeCanExecute();
        }
    }
}
