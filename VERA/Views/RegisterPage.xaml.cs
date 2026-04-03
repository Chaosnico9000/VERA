using VERA.Services;

namespace VERA.Views
{
    public partial class RegisterPage : ContentPage
    {
        private readonly AccountService _account;

        public RegisterPage(AccountService account)
        {
            InitializeComponent();
            _account = account;
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

            var username = UsernameEntry.Text?.Trim() ?? string.Empty;
            var password = PasswordEntry.Text ?? string.Empty;
            var confirm  = ConfirmEntry.Text  ?? string.Empty;

            // Validierung
            if (username.Length < 2)
            {
                ShowError("Benutzername muss mindestens 2 Zeichen haben.");
                return;
            }
            if (password.Length < 8)
            {
                ShowError("Passwort muss mindestens 8 Zeichen lang sein.");
                return;
            }
            if (password != confirm)
            {
                ShowError("Die Passwörter stimmen nicht überein.");
                return;
            }
            var (score, _, _) = EvaluateStrength(password);
            if (score < 0.4)
            {
                ShowError("Passwort ist zu schwach. Verwende Groß-/Kleinbuchstaben, Zahlen und Sonderzeichen.");
                return;
            }

            RegisterButton.IsEnabled = false;

            await RegisterButton.ScaleTo(0.96, 80);
            await RegisterButton.ScaleTo(1.0,  80);

            var ok = await _account.RegisterAsync(username, password);
            if (!ok)
            {
                ShowError("Account konnte nicht erstellt werden.");
                RegisterButton.IsEnabled = true;
                return;
            }

            // Zur Login-Seite navigieren (ohne zurück-Möglichkeit)
            Application.Current!.Windows[0].Page = new NavigationPage(
                new LoginPage(_account, MauiProgram.Services.GetRequiredService<IAuthService>()));
        }

        private void ShowError(string msg)
        {
            ErrorLabel.Text      = msg;
            ErrorLabel.IsVisible = true;
        }

        // ── Passwort-Stärke ───────────────────────────────────────────────────
        private static (double score, string label, string color) EvaluateStrength(string pw)
        {
            if (pw.Length == 0) return (0, string.Empty, "#8FA0DC");

            int points = 0;
            if (pw.Length >= 8)  points++;
            if (pw.Length >= 12) points++;
            if (pw.Any(char.IsUpper))                                           points++;
            if (pw.Any(char.IsLower))                                           points++;
            if (pw.Any(char.IsDigit))                                           points++;
            if (pw.Any(c => !char.IsLetterOrDigit(c)))                          points++;

            return points switch
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
