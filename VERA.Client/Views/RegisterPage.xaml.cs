using VERA.Services;
using VERA.Shared;

namespace VERA.Views
{
    public partial class RegisterPage : ContentPage
    {
        private readonly ApiClient _api;

        public RegisterPage(ApiClient api)
        {
            InitializeComponent();
            _api = api;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            var saved = Preferences.Default.Get("server_url", string.Empty);
            if (!string.IsNullOrEmpty(saved))
            {
                ServerUrlEntry.Text = saved;
                _api.SetBaseUrl(saved);
                _ = CheckConnectionAsync();
            }
        }

        private void OnPasswordChanged(object? sender, TextChangedEventArgs e)
        {
            var pw = e.NewTextValue ?? string.Empty;
            var (score, label, color) = EvaluateStrength(pw);
            StrengthBar.ProgressTo(score, 200, Easing.CubicOut);
            StrengthBar.ProgressColor = Color.FromArgb(color);
            StrengthLabel.Text        = label;
            StrengthLabel.TextColor   = Color.FromArgb(color);
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
                ServerCompatibility.Ok           => ("Verbunden ✓",                        "#4ECCA3"),
                ServerCompatibility.Unreachable  => ("Server nicht erreichbar",             "#E55353"),
                ServerCompatibility.ClientTooOld => ("App veraltet – bitte aktualisieren",  "#FFB347"),
                ServerCompatibility.ServerTooOld => ("Server veraltet – bitte aktualisieren", "#FFB347"),
                _                                => ("Unbekannter Fehler",                  "#E55353"),
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

        private async void OnRegisterClicked(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;

            var url      = ServerUrlEntry.Text?.Trim() ?? string.Empty;
            var username = UsernameEntry.Text?.Trim()  ?? string.Empty;
            var password = PasswordEntry.Text          ?? string.Empty;
            var confirm  = ConfirmEntry.Text           ?? string.Empty;

            if (string.IsNullOrEmpty(url))  { ShowError("Bitte die Server-URL eingeben.");             return; }
            if (username.Length < 2)         { ShowError("Benutzername muss mind. 2 Zeichen haben.");  return; }
            if (password.Length < 8)         { ShowError("Passwort muss mind. 8 Zeichen lang sein.");  return; }
            if (password != confirm)          { ShowError("Die Passwörter stimmen nicht überein.");     return; }

            var (score, _, _) = EvaluateStrength(password);
            if (score < 0.4) { ShowError("Passwort ist zu schwach."); return; }

            RegisterButton.IsEnabled = false;
            await RegisterButton.ScaleToAsync(0.96, 80);
            await RegisterButton.ScaleToAsync(1.0,  80);

            Preferences.Default.Set("server_url", url);
            _api.SetBaseUrl(url);

            var compat = await _api.CheckServerAsync();
            if (compat != ServerCompatibility.Ok)
            {
                ShowError(compat switch
                {
                    ServerCompatibility.Unreachable  => "Server nicht erreichbar.",
                    ServerCompatibility.ClientTooOld => "App veraltet – bitte aktualisieren.",
                    ServerCompatibility.ServerTooOld => "Server veraltet – bitte aktualisieren.",
                    _                                => "Verbindungsfehler."
                });
                RegisterButton.IsEnabled = true;
                return;
            }

            var (ok, error) = await _api.RegisterAsync(username, password);
            if (!ok) { ShowError(error); RegisterButton.IsEnabled = true; return; }

            // Registrierung erfolgreich → automatisch einloggen
            var (result, loginError) = await _api.LoginAsync(username, password);
            if (result == LoginResult.Success)
            {
                Application.Current!.Windows[0].Page = new AppShell();
                return;
            }

            // Auto-Login fehlgeschlagen (sollte nicht passieren) → zur LoginPage mit prefill
            var auth = MauiProgram.Services.GetRequiredService<IAuthService>();
            Application.Current!.Windows[0].Page = new NavigationPage(
                new LoginPage(_api, auth, prefillUsername: username));
        }

        private void ShowError(string msg) { ErrorLabel.Text = msg; ErrorLabel.IsVisible = true; }

        private static (double score, string label, string color) EvaluateStrength(string pw)
        {
            if (pw.Length == 0) return (0, string.Empty, "#8FA0DC");
            int p = 0;
            if (pw.Length >= 8)                        p++;
            if (pw.Length >= 12)                       p++;
            if (pw.Any(char.IsUpper))                  p++;
            if (pw.Any(char.IsLower))                  p++;
            if (pw.Any(char.IsDigit))                  p++;
            if (pw.Any(c => !char.IsLetterOrDigit(c))) p++;
            return p switch
            {
                <= 2 => (0.2, "Sehr schwach",  "#E55353"),
                3    => (0.4, "Schwach",        "#FFB347"),
                4    => (0.6, "Mittel",         "#F5E642"),
                5    => (0.8, "Stark",          "#4ECCA3"),
                _    => (1.0, "Sehr stark 💪",  "#3566E5"),
            };
        }
    }
}
