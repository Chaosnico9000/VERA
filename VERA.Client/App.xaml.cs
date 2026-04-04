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
            var api  = MauiProgram.Services.GetRequiredService<ApiClient>();
            var auth = MauiProgram.Services.GetRequiredService<IAuthService>();

            // Kein Server konfiguriert oder keine Sitzung → Registrierung/Login
            var serverUrl = Preferences.Default.Get("server_url", string.Empty);
            if (string.IsNullOrEmpty(serverUrl))
                return new Window(new NavigationPage(new RegisterPage(api)));

            if (!api.HasSession)
                return new Window(new NavigationPage(new LoginPage(api, auth)));

            // Sitzung vorhanden → direkt zur App (Token wird beim ersten API-Call erneuert)
            return new Window(new AppShell());
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

            var api  = MauiProgram.Services.GetRequiredService<ApiClient>();
            var auth = MauiProgram.Services.GetRequiredService<IAuthService>();
            if (Windows.Count > 0)
                Windows[0].Page = new NavigationPage(new LoginPage(api, auth));
        }
    }
}
