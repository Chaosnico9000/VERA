using VERA.Services;
using VERA.Shared;
using VERA.ViewModels;
#if ANDROID
using Android.Content;
using VERA.Platforms.Android.Services;
#endif

namespace VERA.Views
{
    public partial class EinstellungenPage : ContentPage
    {
        private string? _updateUrl;

#if ANDROID
        private UpdateProgressReceiver? _receiver;
#endif

        public EinstellungenPage()
        {
            InitializeComponent();
            BindingContext = MauiProgram.Services.GetRequiredService<EinstellungenViewModel>();
            ServerUrlEntry.Text = Preferences.Default.Get("server_url", string.Empty);

            VersionLabel.Text = $"v{AppVersion.Current}";
            BuildLabel.Text   = AppInfo.BuildString;

            _ = CheckUpdateAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
#if ANDROID
            _receiver = new UpdateProgressReceiver(OnUpdateProgress);
            var filter = new IntentFilter(UpdateDownloadService.BroadcastAction);
            Android.App.Application.Context.RegisterReceiver(_receiver, filter,
                ReceiverFlags.NotExported);
#endif
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
#if ANDROID
            if (_receiver != null)
            {
                try { Android.App.Application.Context.UnregisterReceiver(_receiver); }
                catch { /* ignorieren falls nicht registriert */ }
                _receiver = null;
            }
#endif
        }

        private void OnUpdateProgress(double progress, string statusText)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (progress < 0)
                {
                    // Fehler
                    UpdateButton.Text      = "🆕  Update herunterladen";
                    UpdateButton.IsEnabled = true;
                }
                else if (progress >= 2.0)
                {
                    // Fertig — Installer öffnet sich automatisch
                    UpdateButton.Text      = "✅  Installer wird geöffnet…";
                    UpdateButton.IsEnabled = false;
                }
                else
                {
                    UpdateButton.Text = statusText;
                }
            });
        }

        private async Task CheckUpdateAsync()
        {
            try
            {
                var svc  = MauiProgram.Services.GetRequiredService<UpdateService>();
                var info = await svc.CheckAsync();
                if (info is not { IsNewer: true }) return;

                _updateUrl = info.DownloadUrl;
                UpdateButton.Text       = $"🆕  Update auf v{info.LatestVersion} installieren";
                UpdateButton.IsVisible  = true;
                UpdateDivider.IsVisible = true;
            }
            catch { /* Update-Check ist nicht kritisch */ }
        }

        private async void OnUpdateClicked(object? sender, EventArgs e)
        {
            if (_updateUrl is null) return;

            UpdateButton.IsEnabled = false;
            UpdateButton.Text      = "⬇️  Wird gestartet…";

            var svc = MauiProgram.Services.GetRequiredService<UpdateService>();
            var ok  = await svc.DownloadAndInstallAsync(_updateUrl);
            if (!ok)
            {
                UpdateButton.Text      = "🆕  Update herunterladen";
                UpdateButton.IsEnabled = true;
                await DisplayAlertAsync("Fehler", "Download konnte nicht gestartet werden.", "OK");
            }
            // Fortschritt kommt via Broadcast → OnUpdateProgress
        }

        private void OnMenuClicked(object? sender, EventArgs e)
            => Shell.Current.FlyoutIsPresented = true;

        private async void OnChangelogClicked(object? sender, EventArgs e)
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("CHANGELOG.md");
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                await Navigation.PushAsync(new ChangelogPage(content));
            }
            catch
            {
                await DisplayAlertAsync("Fehler", "Changelog konnte nicht geladen werden.", "OK");
            }
        }

        private void OnServerUrlCompleted(object? sender, EventArgs e)
        {
            var url = ServerUrlEntry.Text?.Trim() ?? string.Empty;
            Preferences.Default.Set("server_url", url);
            var client = MauiProgram.Services.GetRequiredService<ApiClient>();
            if (!string.IsNullOrEmpty(url)) client.SetBaseUrl(url);
        }

        private async void OnTestConnectionClicked(object? sender, EventArgs e)
        {
            var url = ServerUrlEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(url))
            {
                await DisplayAlertAsync("Fehler", "Bitte zuerst eine Server-URL eingeben.", "OK");
                return;
            }

            Preferences.Default.Set("server_url", url);
            var client = MauiProgram.Services.GetRequiredService<ApiClient>();
            client.SetBaseUrl(url);

            var result = await client.CheckServerAsync();
            var msg = result switch
            {
                ServerCompatibility.Ok           => "✅ Verbindung erfolgreich!",
                ServerCompatibility.ServerTooOld => $"⚠️ Server-Version zu alt. Mindestens v{AppVersion.MinServerVersion} wird benötigt.",
                ServerCompatibility.ClientTooOld => "⚠️ Diese App-Version wird nicht mehr unterstützt. Bitte aktualisieren.",
                ServerCompatibility.Unreachable  => "❌ Server nicht erreichbar. URL oder Netzwerk prüfen.",
                _                                => "Unbekannter Fehler."
            };
            await DisplayAlertAsync("Verbindungstest", msg, "OK");
        }

        private async void OnChangePasswordClicked(object? sender, EventArgs e)
        {
            var api = MauiProgram.Services.GetRequiredService<ApiClient>();
            if (!api.HasSession)
            {
                await DisplayAlertAsync("Nicht verbunden", "Bitte zuerst mit dem Server verbinden und anmelden.", "OK");
                return;
            }

            var oldPw = await DisplayPromptAsync(
                "Passwort ändern", "Aktuelles Passwort:",
                keyboard: Keyboard.Default, maxLength: 100);
            if (string.IsNullOrEmpty(oldPw)) return;

            var newPw = await DisplayPromptAsync(
                "Passwort ändern", "Neues Passwort (mind. 8 Zeichen):",
                keyboard: Keyboard.Default, maxLength: 100);
            if (string.IsNullOrEmpty(newPw)) return;

            if (newPw.Length < 8)
            {
                await DisplayAlertAsync("Fehler", "Das neue Passwort muss mindestens 8 Zeichen lang sein.", "OK");
                return;
            }

            var confirm = await DisplayPromptAsync(
                "Passwort ändern", "Neues Passwort bestätigen:",
                keyboard: Keyboard.Default, maxLength: 100);
            if (confirm != newPw)
            {
                await DisplayAlertAsync("Fehler", "Die Passwörter stimmen nicht überein.", "OK");
                return;
            }

            var ok = await api.ChangePasswordAsync(oldPw, newPw);
            if (ok)
                await DisplayAlertAsync("Erfolg", "Passwort wurde erfolgreich geändert. ✓", "OK");
            else
                await DisplayAlertAsync("Fehler", "Altes Passwort falsch oder Verbindungsfehler.", "OK");
        }
    }
}
