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

        private void OnPasswordChanged(object? sender, TextChangedEventArgs e)
        {
            var pw = e.NewTextValue ?? string.Empty;
            var (score, label, color) = EvaluateStrength(pw);
            StrengthBar.ProgressTo(score, 200, Easing.CubicOut);
            StrengthBar.ProgressColor = Color.FromArgb(color);
            StrengthLabel.Text        = label;
            StrengthLabel.TextColor   = Color.FromArgb(color);
        }

        private async void OnRegisterClicked(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;

            var url      = ServerUrlEntry.Text?.Trim() ?? string.Empty;
            var username = UsernameEntry.Text?.Trim()  ?? string.Empty;
            var password = PasswordEntry.Text          ?? string.Empty;
            var confirm  = ConfirmEntry.Text           ?? string.Empty;

            if (string.IsNullOrEmpty(url))     { ShowError("Bitte die Server-URL eingeben."); return; }
            if (username.Length < 2)            { ShowError("Benutzername muss mind. 2 Zeichen haben."); return; }
            if (password.Length < 8)            { ShowError("Passwort muss mind. 8 Zeichen lang sein."); return; }
            if (password != confirm)            { ShowError("Die Passwörter stimmen nicht überein."); return; }

            var (score, _, _) = EvaluateStrength(password);
            if (score < 0.4) { ShowError("Passwort ist zu schwach."); return; }

            RegisterButton.IsEnabled = false;
            await RegisterButton.ScaleTo(0.96, 80);
            await RegisterButton.ScaleTo(1.0,  80);

            Preferences.Default.Set("server_url", url);
            _api.SetBaseUrl(url);

            var (ok, error) = await _api.RegisterAsync(username, password);
            if (!ok) { ShowError(error); RegisterButton.IsEnabled = true; return; }

            var (result, _) = await _api.LoginAsync(username, password);
            if (result == LoginResult.Success)
                Application.Current!.Windows[0].Page = new AppShell();
            else
                Application.Current!.Windows[0].Page = new NavigationPage(
                    new LoginPage(_api, MauiProgram.Services.GetRequiredService<IAuthService>()));
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
