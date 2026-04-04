using VERA.Services;
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
