using VERA.ViewModels;

namespace VERA
{
    public partial class MainPage : ContentPage
    {
        private readonly DashboardViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();
            _viewModel = MauiProgram.Services.GetRequiredService<DashboardViewModel>();
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            DateLabel.Text = DateTime.Now.ToString("ddd, dd. MMM",
                System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
            _ = _viewModel.InitializeAsync();
        }
    }
}
