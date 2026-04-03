using Microsoft.Extensions.DependencyInjection;
using VERA.Services;
using VERA.Views;

namespace VERA
{
    public partial class App : Application
    {
        private bool _lockOnResume;

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var account = MauiProgram.Services.GetRequiredService<AccountService>();
            var auth    = MauiProgram.Services.GetRequiredService<IAuthService>();

            // Kein Account? → Registrierung anzeigen
            if (!account.AccountExists())
                return new Window(new NavigationPage(new RegisterPage(account)));

            // Account vorhanden → Login anzeigen
            return new Window(new NavigationPage(new LoginPage(account, auth)));
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            _lockOnResume = true;
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (!_lockOnResume) return;
            _lockOnResume = false;

            var account = MauiProgram.Services.GetRequiredService<AccountService>();
            var auth    = MauiProgram.Services.GetRequiredService<IAuthService>();
            if (Windows.Count > 0)
                Windows[0].Page = new NavigationPage(new LoginPage(account, auth));
        }
    }
}
