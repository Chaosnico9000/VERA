using Microsoft.Extensions.DependencyInjection;
using VERA.Services;
using VERA.Shared;
using VERA.Views;

namespace VERA
{
    public partial class App : Application
    {
        private bool _lockOnResume;

        public App()
        {
            InitializeComponent();
            MauiProgram.Services.GetRequiredService<ThemeService>().ApplySaved();
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
            var window = new Window(new AppShell());
            _ = CheckForUpdateAsync();
            return window;
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            // Nur sperren wenn Nutzer bereits in der App ist (nicht wenn er gerade einloggt)
            var currentPage = Windows.FirstOrDefault()?.Page;
            bool onAuthPage = currentPage is NavigationPage nav &&
                              (nav.CurrentPage is LoginPage || nav.CurrentPage is RegisterPage);
            if (!onAuthPage)
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

        private static async Task CheckForUpdateAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            try
            {
                var svc  = MauiProgram.Services.GetRequiredService<UpdateService>();
                var info = await svc.CheckAsync();
                if (info is not { IsNewer: true }) return;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                    if (page is null) return;

                    var ok = await page.DisplayAlertAsync(
                        "Update verfügbar 🆕",
                        $"Version {info.LatestVersion} ist verfügbar (du hast {AppVersion.Current}).\nJetzt herunterladen und installieren?",
                        "Installieren", "Später");

                    if (!ok) return;

                    await svc.DownloadAndInstallAsync(info.DownloadUrl);
                });
            }
            catch { /* Update-Check ist nicht kritisch */ }
        }
    }
}
