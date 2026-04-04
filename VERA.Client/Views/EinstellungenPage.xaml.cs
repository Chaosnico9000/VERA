using VERA.Services;
using VERA.Shared;
using VERA.ViewModels;

namespace VERA.Views
{
    public partial class EinstellungenPage : ContentPage
    {
        public EinstellungenPage()
        {
            InitializeComponent();
            BindingContext = MauiProgram.Services.GetRequiredService<EinstellungenViewModel>();
            ServerUrlEntry.Text = Preferences.Default.Get("server_url", string.Empty);
        }

        private void OnMenuClicked(object? sender, EventArgs e)
            => Shell.Current.FlyoutIsPresented = true;

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
                await DisplayAlert("Fehler", "Bitte zuerst eine Server-URL eingeben.", "OK");
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
            await DisplayAlert("Verbindungstest", msg, "OK");
        }

        private async void OnChangePasswordClicked(object? sender, EventArgs e)
        {
            var api = MauiProgram.Services.GetRequiredService<ApiClient>();
            if (!api.HasSession)
            {
                await DisplayAlert("Nicht verbunden", "Bitte zuerst mit dem Server verbinden und anmelden.", "OK");
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
                await DisplayAlert("Fehler", "Das neue Passwort muss mindestens 8 Zeichen lang sein.", "OK");
                return;
            }

            var confirm = await DisplayPromptAsync(
                "Passwort ändern", "Neues Passwort bestätigen:",
                keyboard: Keyboard.Default, maxLength: 100);
            if (confirm != newPw)
            {
                await DisplayAlert("Fehler", "Die Passwörter stimmen nicht überein.", "OK");
                return;
            }

            var ok = await api.ChangePasswordAsync(oldPw, newPw);
            if (ok)
                await DisplayAlert("Erfolg", "Passwort wurde erfolgreich geändert. ✓", "OK");
            else
                await DisplayAlert("Fehler", "Altes Passwort falsch oder Verbindungsfehler.", "OK");
        }
    }
}
