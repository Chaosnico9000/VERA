using VERA.Services;

namespace VERA.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly AccountService _account;
        private readonly IAuthService   _auth;
        private CancellationTokenSource? _lockoutCts;

        public LoginPage(AccountService account, IAuthService auth)
        {
            InitializeComponent();
            _account = account;
            _auth    = auth;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Benutzernamen anzeigen
            var name = await _account.GetUsernameAsync();
            if (!string.IsNullOrEmpty(name))
                WelcomeLabel.Text = $"Willkommen zurück, {name} 👋";

            // Biometrie-Button nur zeigen wenn verfügbar
            BiometricButton.IsVisible = _auth.IsAvailable;

            // Lockout prüfen
            await UpdateLockoutUiAsync();

            // Biometrie automatisch starten wenn kein Lockout
            if (_auth.IsAvailable)
            {
                var remaining = await _account.GetLockoutSecondsRemainingAsync();
                if (remaining == 0)
                    await TryBiometricAsync();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _lockoutCts?.Cancel();
            _lockoutCts = null;
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            var password = PasswordEntry.Text ?? string.Empty;
            if (string.IsNullOrEmpty(password)) return;

            LoginButton.IsEnabled = false;
            ErrorLabel.IsVisible  = false;

            await LoginButton.ScaleTo(0.96, 80);
            await LoginButton.ScaleTo(1.0,  80);

            var (result, _) = await _account.LoginAsync(password);

            switch (result)
            {
                case LoginResult.Success:
                    PasswordEntry.Text = string.Empty;
                    await NavigateToAppAsync();
                    break;

                case LoginResult.InvalidPassword:
                    PasswordEntry.Text = string.Empty;
                    ShowError("Falsches Passwort.");
                    await UpdateLockoutUiAsync();
                    LoginButton.IsEnabled = true;
                    break;

                case LoginResult.AccountLocked:
                    await UpdateLockoutUiAsync();
                    LoginButton.IsEnabled = true;
                    break;

                case LoginResult.NoAccountFound:
                    ShowError("Kein Account gefunden.");
                    LoginButton.IsEnabled = true;
                    break;
            }
        }

        private async void OnBiometricClicked(object? sender, EventArgs e)
            => await TryBiometricAsync();

        private async Task TryBiometricAsync()
        {
            if (!_auth.IsAvailable) return;

            var remaining = await _account.GetLockoutSecondsRemainingAsync();
            if (remaining > 0) return;

            var result = await _auth.AuthenticateAsync("Zugriff auf VERA");
            if (result == AuthResult.Success)
                await NavigateToAppAsync();
        }

        private async Task UpdateLockoutUiAsync()
        {
            var remaining = await _account.GetLockoutSecondsRemainingAsync();
            if (remaining > 0)
            {
                LockoutBanner.IsVisible = true;
                LoginButton.IsEnabled   = false;
                BiometricButton.IsEnabled = false;
                StartLockoutCountdown(remaining);
            }
            else
            {
                LockoutBanner.IsVisible   = false;
                LoginButton.IsEnabled     = true;
                BiometricButton.IsEnabled = true;
            }
        }

        private void StartLockoutCountdown(int seconds)
        {
            _lockoutCts?.Cancel();
            _lockoutCts = new CancellationTokenSource();
            var token   = _lockoutCts.Token;

            Task.Run(async () =>
            {
                var end = DateTime.UtcNow.AddSeconds(seconds);
                while (!token.IsCancellationRequested)
                {
                    var left = (int)(end - DateTime.UtcNow).TotalSeconds;
                    if (left <= 0)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            LockoutBanner.IsVisible   = false;
                            LoginButton.IsEnabled     = true;
                            BiometricButton.IsEnabled = true;
                        });
                        break;
                    }
                    var min = left / 60;
                    var sec = left % 60;
                    var txt = min > 0
                        ? $"Noch {min}:{sec:D2} Min gesperrt"
                        : $"Noch {sec} Sekunden gesperrt";

                    MainThread.BeginInvokeOnMainThread(() => LockoutLabel.Text = txt);
                    await Task.Delay(1000, token).ContinueWith(_ => { });
                }
            }, token);
        }

        private void ShowError(string msg)
        {
            ErrorLabel.Text      = msg;
            ErrorLabel.IsVisible = true;
        }

        private static Task NavigateToAppAsync()
        {
            Application.Current!.Windows[0].Page = new AppShell();
            return Task.CompletedTask;
        }
    }
}
