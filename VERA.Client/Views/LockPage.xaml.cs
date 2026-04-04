using VERA.Services;

namespace VERA.Views
{
    public partial class LockPage : ContentPage
    {
        private readonly IAuthService _auth;
        private bool _unlocking;

        public LockPage(IAuthService auth)
        {
            InitializeComponent();
            _auth = auth;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Direkt beim Öffnen automatisch Biometrie starten
            await TryUnlockAsync();
        }

        private async void OnUnlockClicked(object? sender, EventArgs e)
            => await TryUnlockAsync();

        private async Task TryUnlockAsync()
        {
            if (_unlocking) return;
            _unlocking = true;

            UnlockButton.IsEnabled = false;
            ErrorLabel.IsVisible = false;

            if (!_auth.IsAvailable)
            {
                // Kein Biometrie/PIN auf dem Gerät eingerichtet → direkt öffnen
                await NavigateToMainAsync();
                return;
            }

            // Button-Animation
            await UnlockButton.ScaleTo(0.95, 80);
            await UnlockButton.ScaleTo(1.0, 80);

            var result = await _auth.AuthenticateAsync("Zugriff auf deine Zeiteinträge");

            switch (result)
            {
                case AuthResult.Success:
                    StatusLabel.Text = "✓ Authentifiziert";
                    StatusLabel.TextColor = Color.FromArgb("#4ECCA3");
                    await Task.Delay(200);
                    await NavigateToMainAsync();
                    break;

                case AuthResult.Cancelled:
                    StatusLabel.Text = "Entsperre mit deinem Fingerabdruck,\nGesicht oder Geräte-PIN";
                    StatusLabel.TextColor = Color.FromArgb("#8FA0DC");
                    ErrorLabel.Text = "Abgebrochen – tippe auf Entsperren um es erneut zu versuchen.";
                    ErrorLabel.IsVisible = true;
                    UnlockButton.IsEnabled = true;
                    break;

                case AuthResult.Failure:
                case AuthResult.NotAvailable:
                    ErrorLabel.Text = "Authentifizierung fehlgeschlagen. Bitte erneut versuchen.";
                    ErrorLabel.IsVisible = true;
                    await ErrorLabel.FadeTo(0, 3000);
                    ErrorLabel.IsVisible = false;
                    ErrorLabel.Opacity = 1;
                    UnlockButton.IsEnabled = true;
                    break;
            }

            _unlocking = false;
        }

        private static async Task NavigateToMainAsync()
        {
            // Zum Haupt-Shell navigieren ohne Back-Stack
            Application.Current!.Windows[0].Page = new AppShell();
            await Task.CompletedTask;
        }
    }
}
