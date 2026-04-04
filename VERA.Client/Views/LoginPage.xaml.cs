using VERA.Services;
using VERA.Shared;

namespace VERA.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly ApiClient    _api;
        private readonly IAuthService _auth;

        public LoginPage(ApiClient api, IAuthService auth)
        {
            InitializeComponent();
            _api  = api;
            _auth = auth;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Gespeicherte URL laden und anzeigen
            var saved = Preferences.Default.Get("server_url", string.Empty);
            if (!string.IsNullOrEmpty(saved))
            {
                ServerUrlEntry.Text = saved;
                _api.SetBaseUrl(saved);
                _ = CheckConnectionAsync();
            }

            var name = _api.Username;
            if (!string.IsNullOrEmpty(name))
                WelcomeLabel.Text = $"Willkommen zurück, {name} 👋";

            BiometricButton.IsVisible = _auth.IsAvailable;

            if (_auth.IsAvailable && _api.HasSession)
                await TryBiometricAsync();
        }

        private void OnServerUrlCompleted(object? sender, EventArgs e) => ApplyAndCheckUrl();
        private void OnServerUrlUnfocused(object? sender, FocusEventArgs e) => ApplyAndCheckUrl();

        private void ApplyAndCheckUrl()
        {
            var url = ServerUrlEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(url)) return;
            Preferences.Default.Set("server_url", url);
            _api.SetBaseUrl(url);
            _ = CheckConnectionAsync();
        }

        private async Task CheckConnectionAsync()
        {
            SetStatus("Prüfe Verbindung...", "#424F8A");
            var result = await _api.CheckServerAsync();
            SetStatus(result switch
            {
                ServerCompatibility.Ok           => ("Verbunden ✓",                          "#4ECCA3"),
                ServerCompatibility.Unreachable  => ("Server nicht erreichbar",               "#E55353"),
                ServerCompatibility.ClientTooOld => ("App veraltet – bitte aktualisieren",    "#FFB347"),
                ServerCompatibility.ServerTooOld => ("Server veraltet – bitte aktualisieren", "#FFB347"),
                _                                => ("Unbekannter Fehler",                    "#E55353"),
            });
        }

        private void SetStatus(string text, string hex) =>
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var c = Color.FromArgb(hex);
                StatusDot.TextColor   = c;
                StatusLabel.Text      = text;
                StatusLabel.TextColor = c;
            });

        private void SetStatus((string text, string hex) t) => SetStatus(t.text, t.hex);

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            var username = UsernameEntry.Text?.Trim() ?? string.Empty;
            var password = PasswordEntry.Text         ?? string.Empty;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) return;

            LoginButton.IsEnabled = false;
            ErrorLabel.IsVisible  = false;
            await LoginButton.ScaleTo(0.96, 80);
            await LoginButton.ScaleTo(1.0,  80);

            var compat = await _api.CheckServerAsync();
            if (compat != ServerCompatibility.Ok)
            {
                ShowError(compat switch
                {
                    ServerCompatibility.Unreachable  => "Server nicht erreichbar. Bitte URL oben prüfen.",
                    ServerCompatibility.ClientTooOld => "Diese App-Version wird nicht mehr unterstützt. Bitte aktualisieren.",
                    ServerCompatibility.ServerTooOld => "Der Server ist veraltet. Bitte den Server aktualisieren.",
                    _                                => "Verbindungsfehler."
                });
                LoginButton.IsEnabled = true;
                return;
            }

            var (result, error) = await _api.LoginAsync(username, password);
            switch (result)
            {
                case LoginResult.Success:
                    PasswordEntry.Text = string.Empty;
                    Application.Current!.Windows[0].Page = new AppShell();
                    break;
                case LoginResult.AccountLocked:
                    LockoutBanner.IsVisible = true;
                    LockoutLabel.Text       = "Account gesperrt. Bitte warte 5 Minuten.";
                    _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ =>
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            LockoutBanner.IsVisible = false;
                            LoginButton.IsEnabled   = true;
                        }));
                    break;
                default:
                    PasswordEntry.Text    = string.Empty;
                    ShowError(string.IsNullOrEmpty(error) ? "Falsches Passwort oder Benutzername." : error);
                    LoginButton.IsEnabled = true;
                    break;
            }
        }

        private async void OnBiometricClicked(object? sender, EventArgs e) => await TryBiometricAsync();

        private async Task TryBiometricAsync()
        {
            if (!_auth.IsAvailable || !_api.HasSession) return;
            var result = await _auth.AuthenticateAsync("Zugriff auf VERA");
            if (result == AuthResult.Success)
            {
                var ok = await _api.RefreshTokenAsync();
                if (ok) Application.Current!.Windows[0].Page = new AppShell();
                else ShowError("Sitzung abgelaufen. Bitte neu anmelden.");
            }
        }

        private void ShowError(string msg) { ErrorLabel.Text = msg; ErrorLabel.IsVisible = true; }
    }
}
