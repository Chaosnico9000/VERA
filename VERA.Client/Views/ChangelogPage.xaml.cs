namespace VERA.Views
{
    public partial class ChangelogPage : ContentPage
    {
        public ChangelogPage(string content)
        {
            InitializeComponent();
            ChangelogLabel.Text = content;
        }

        private async void OnBackClicked(object? sender, EventArgs e)
            => await Navigation.PopAsync();
    }
}
