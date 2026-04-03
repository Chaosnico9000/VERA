using VERA.ViewModels;

namespace VERA.Views
{
    public partial class StatistikPage : ContentPage
    {
        private readonly StatistikViewModel _viewModel;

        public StatistikPage()
        {
            InitializeComponent();
            _viewModel = MauiProgram.Services.GetRequiredService<StatistikViewModel>();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }

        private void OnMenuClicked(object? sender, EventArgs e)
            => Shell.Current.FlyoutIsPresented = true;
    }
}
