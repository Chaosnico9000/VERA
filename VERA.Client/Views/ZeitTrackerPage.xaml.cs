using VERA.ViewModels;

namespace VERA.Views
{
    public partial class ZeitTrackerPage : ContentPage
    {
        private readonly DashboardViewModel _viewModel;
        private readonly GamificationViewModel _gamification;

        public ZeitTrackerPage()
        {
            InitializeComponent();
            _viewModel    = MauiProgram.Services.GetRequiredService<DashboardViewModel>();
            _gamification = MauiProgram.Services.GetRequiredService<GamificationViewModel>();

            // GamificationViewModel als Sub-Property verfügbar machen
            _viewModel.Gamification = _gamification;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            DateLabel.Text = DateTime.Now.ToString("ddd, dd. MMM",
                System.Globalization.CultureInfo.GetCultureInfo("de-DE"));

            await _viewModel.InitializeAsync();
            await _gamification.LoadAsync();

            // XP-Bar animiert einfahren
            XpProgressBar.Progress = 0;
            await XpProgressBar.ProgressTo(_gamification.LevelProgress, 900, Easing.CubicOut);

            // Level-Emoji Puls-Animation
            await LevelEmojiLabel.ScaleToAsync(1.35, 200, Easing.SpringOut);
            await LevelEmojiLabel.ScaleToAsync(1.0, 200, Easing.SpringIn);

            // Level-Up Banner anzeigen wenn neues Level
            if (_gamification.NewLevelUp)
                await ShowLevelUpBannerAsync();
            else if (_gamification.NewAchievement)
                await ShowLevelUpBannerAsync();
            else if (_gamification.NewStreakMilestone)
                await ShowStreakBannerAsync();
        }

        private async Task ShowLevelUpBannerAsync()
        {
            LevelUpLabel.Text = _gamification.LevelUpText;
            LevelUpBanner.IsVisible = true;
            await LevelUpBanner.FadeToAsync(1.0, 350);
            await LevelUpBanner.ScaleToAsync(1.04, 200, Easing.SpringOut);
            await LevelUpBanner.ScaleToAsync(1.0, 150);
            await Task.Delay(3500);
            await LevelUpBanner.FadeToAsync(0.0, 500);
            LevelUpBanner.IsVisible = false;
        }

        private async Task ShowStreakBannerAsync()
        {
            LevelUpLabel.Text = _gamification.LevelUpText;
            LevelUpBanner.IsVisible = true;
            await LevelUpBanner.FadeToAsync(1.0, 350);
            await Task.Delay(3000);
            await LevelUpBanner.FadeToAsync(0.0, 500);
            LevelUpBanner.IsVisible = false;
        }

        private void OnMenuClicked(object? sender, EventArgs e)
            => Shell.Current.FlyoutIsPresented = true;
    }
}

