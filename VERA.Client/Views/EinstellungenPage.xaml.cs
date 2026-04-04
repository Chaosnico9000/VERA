using VERA.Services;
using VERA.Shared;
using VERA.ViewModels;

namespace VERA.Views
{
    public partial class EinstellungenPage : ContentPage
    {
        private string? _updateUrl;

        public EinstellungenPage()
        {
            InitializeComponent();
            BindingContext = MauiProgram.Services.GetRequiredService<EinstellungenViewModel>();
            ServerUrlEntry.Text = Preferences.Default.Get("server_url", string.Empty);

            VersionLabel.Text = $"v{AppVersion.Current}";
            BuildLabel.Text   = AppInfo.BuildString;

            _ = CheckUpdateAsync();
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
            UpdateButton.Text      = "⬇️  Wird heruntergeladen...";

            var svc      = MauiProgram.Services.GetRequiredService<UpdateService>();
            var progress = new Progress<double>(p =>
                MainThread.BeginInvokeOnMainThread(() =>
                    UpdateButton.Text = $"⬇️  {p:P0} heruntergeladen..."));

            var ok = await svc.DownloadAndInstallAsync(_updateUrl, progress);
            if (!ok)
            {
                UpdateButton.Text      = "🆕  Update herunterladen";
                UpdateButton.IsEnabled = true;
                await DisplayAlertAsync("Fehler", "Download fehlgeschlagen. Bitte versuche es erneut.", "OK");
            }
            // Bei Erfolg startet der Android-Installer — App bleibt offen
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
