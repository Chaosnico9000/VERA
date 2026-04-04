using System.Collections.ObjectModel;
using System.Windows.Input;
using VERA.Models;
using VERA.Services;

namespace VERA.ViewModels
{
    public class EditEntryViewModel : BaseViewModel
    {
        private readonly ITimeTrackingService _service;
        private TimeEntry? _entry;

        private DateTime _startDate;
        private TimeSpan _startTime;
        private DateTime _endDate;
        private TimeSpan _endTime;
        private bool _hasEndTime;
        private bool _isNew;
        private EntryType _entryType = EntryType.Arbeit;

        public EditEntryViewModel(ITimeTrackingService service)
        {
            _service = service;
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy);

            EntryTypes = new ObservableCollection<string>
            {
                "Arbeit", "Urlaub (ganztags)", "Urlaub (halbtags)", "Feiertag"
            };
        }

        public ObservableCollection<string> EntryTypes { get; }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public TimeSpan StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public TimeSpan EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public bool HasEndTime
        {
            get => _hasEndTime;
            set => SetProperty(ref _hasEndTime, value);
        }

        public bool IsNew
        {
            get => _isNew;
            private set
            {
                if (SetProperty(ref _isNew, value))
                    OnPropertyChanged(nameof(PageTitle));
            }
        }

        public string PageTitle => IsNew ? "Neuer Eintrag" : "Bearbeiten";

        public int SelectedTypeIndex
        {
            get => (int)_entryType;
            set
            {
                _entryType = (EntryType)value;
                OnPropertyChanged();
                // Für Sondertage kein Ende nötig
                HasEndTime = _entryType == EntryType.Arbeit;
                OnPropertyChanged(nameof(ShowTimePickers));
            }
        }

        // Zeitpicker nur bei Arbeit sinnvoll
        public bool ShowTimePickers => _entryType == EntryType.Arbeit;

        public ICommand SaveCommand { get; }

        public void Load(TimeEntry entry)
        {
            IsNew = false;
            _entry = entry;
            StartDate      = entry.StartTime.Date;
            StartTime      = entry.StartTime.TimeOfDay;
            HasEndTime     = entry.EndTime.HasValue;
            EndDate        = entry.EndTime?.Date ?? DateTime.Today;
            EndTime        = entry.EndTime?.TimeOfDay ?? TimeSpan.Zero;
            SelectedTypeIndex = (int)entry.Type;
        }

        public void LoadNew()
        {
            IsNew = true;
            _entry = null;
            StartDate      = DateTime.Today;
            StartTime      = TimeSpan.FromHours(8);
            EndDate        = DateTime.Today;
            EndTime        = TimeSpan.FromHours(16);
            HasEndTime     = true;
            SelectedTypeIndex = 0;
        }

        private async Task SaveAsync()
        {
            // Endzeit vor Startzeit?
            if (HasEndTime && _entryType == EntryType.Arbeit)
            {
                var start = StartDate.Date + StartTime;
                var end   = EndDate.Date + EndTime;
                if (end <= start)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Ung\u00fcltige Zeit",
                        "Die Endzeit muss nach der Startzeit liegen.",
                        "OK");
                    return;
                }
            }

            IsBusy = true;
            try
            {
                if (IsNew)
                {
                    if (_entryType != EntryType.Arbeit)
                    {
                        var sonderTitel = _entryType switch
                        {
                            EntryType.UrlaubGanztag => "Urlaub (ganztags)",
                            EntryType.UrlaubHalbtag => "Urlaub (halbtags)",
                            EntryType.Feiertag      => "Feiertag",
                            _                       => "Sondertag"
                        };
                        await _service.AddSonderTagAsync(_entryType, sonderTitel, StartDate.Date);
                    }
                    else
                    {
                        var entry = new TimeEntry
                        {
                            Title     = GenerateTitle(StartDate.Date),
                            Type      = EntryType.Arbeit,
                            StartTime = StartDate.Date + StartTime,
                            EndTime   = HasEndTime ? EndDate.Date + EndTime : null
                        };
                        await _service.AddManualEntryAsync(entry);
                    }
                }
                else
                {
                    if (_entry == null) return;
                    _entry.Title     = _entryType == EntryType.Arbeit
                        ? GenerateTitle(_entry.StartTime.Date)
                        : _entry.Title;
                    _entry.Type      = _entryType;
                    _entry.StartTime = StartDate.Date + (_entryType == EntryType.Arbeit ? StartTime : TimeSpan.FromHours(8));
                    _entry.EndTime   = (_entryType == EntryType.Arbeit && HasEndTime)
                        ? EndDate.Date + EndTime
                        : null;
                    await _service.UpdateEntryAsync(_entry);
                }
                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static string GenerateTitle(DateTime date)
        {
            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            return $"Arbeitstag \u2013 {date.ToString("ddd, dd. MMM", de)}";
        }
    }
}
