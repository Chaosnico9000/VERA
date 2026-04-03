namespace VERA.Views
{
    public partial class LevelingPage : ContentPage
    {
        public LevelingPage(ViewModels.GamificationViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var vm = (ViewModels.GamificationViewModel)BindingContext;
            await vm.LoadAsync();

            // Hero-Emoji Pulse-Animation
            await HeroEmoji.ScaleTo(1.25, 180, Easing.SinOut);
            await HeroEmoji.ScaleTo(1.0,  140, Easing.SinIn);

            // XP-Balken animieren
            XpBar.Progress = 0;
            await XpBar.ProgressTo(vm.LevelProgress, 900, Easing.CubicOut);
        }

        private void OnMenuClicked(object sender, EventArgs e)
            => Shell.Current.FlyoutIsPresented = true;
    }
}
