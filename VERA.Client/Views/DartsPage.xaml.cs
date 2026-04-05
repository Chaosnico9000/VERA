using VERA.Models;
using VERA.ViewModels;

namespace VERA.Views
{
    public partial class DartsPage : ContentPage
    {
        private readonly DartsViewModel _vm;

        public DartsPage()
        {
            InitializeComponent();
            _vm = MauiProgram.Services.GetRequiredService<DartsViewModel>();
            BindingContext = _vm;
        }

        private void OnMenuClicked(object sender, EventArgs e)
            => Shell.Current.FlyoutIsPresented = true;

        private void OnRemovePlayer(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is DartsPlayerSetup player)
                _vm.SetupPlayers.Remove(player);
        }
    }
}
