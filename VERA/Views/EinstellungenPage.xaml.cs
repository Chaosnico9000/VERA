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
        }

        private void OnMenuClicked(object? sender, EventArgs e)
            => Shell.Current.FlyoutIsPresented = true;

        private async void OnChangePasswordClicked(object? sender, EventArgs e)
        {
            var account = MauiProgram.Services.GetRequiredService<AccountService>();

            var oldPw = await DisplayPromptAsync(
                "Passwort ändern",
                "Aktuelles Passwort eingeben:",
                keyboard: Keyboard.Default,
                maxLength: 100);
            if (string.IsNullOrEmpty(oldPw)) return;

            var newPw = await DisplayPromptAsync(
                "Passwort ändern",
                "Neues Passwort (mind. 8 Zeichen):",
                keyboard: Keyboard.Default,
                maxLength: 100);
            if (string.IsNullOrEmpty(newPw)) return;

            if (newPw.Length < 8)
            {
                await DisplayAlert("Fehler", "Das neue Passwort muss mindestens 8 Zeichen lang sein.", "OK");
                return;
            }

            var confirm = await DisplayPromptAsync(
                "Passwort ändern",
                "Neues Passwort bestätigen:",
                keyboard: Keyboard.Default,
                maxLength: 100);
            if (confirm != newPw)
            {
                await DisplayAlert("Fehler", "Die Passwörter stimmen nicht überein.", "OK");
                return;
            }

            var ok = await account.ChangePasswordAsync(oldPw, newPw);
            if (ok)
                await DisplayAlert("Erfolg", "Passwort wurde erfolgreich geändert. ✓", "OK");
            else
                await DisplayAlert("Fehler", "Das aktuelle Passwort ist falsch oder der Account ist gesperrt.", "OK");
        }
    }
}
