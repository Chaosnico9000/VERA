using VERA.ViewModels;

namespace VERA.Views
{
    public partial class HistoryPage : ContentPage
    {
        private readonly HistoryViewModel _viewModel;
        private CancellationTokenSource? _loadCts;

        public HistoryPage()
        {
            InitializeComponent();
            _viewModel = MauiProgram.Services.GetRequiredService<HistoryViewModel>();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            try
            {
                await _viewModel.LoadAsync(_loadCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Navigation away cancelled the load — expected, ignore.
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _loadCts?.Cancel();
        }
    }
}
