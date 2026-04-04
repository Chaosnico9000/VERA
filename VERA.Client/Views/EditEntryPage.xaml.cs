using VERA.Models;
using VERA.ViewModels;

namespace VERA.Views
{
    public partial class EditEntryPage : ContentPage
    {
        private readonly EditEntryViewModel _viewModel;

        public EditEntryPage()
        {
            InitializeComponent();
            _viewModel = MauiProgram.Services.GetRequiredService<EditEntryViewModel>();
            BindingContext = _viewModel;
        }

        public void LoadEntry(TimeEntry entry) => _viewModel.Load(entry);
        public void LoadNew() => _viewModel.LoadNew();

        private async void OnBackClicked(object? sender, EventArgs e)
            => await Shell.Current.GoToAsync("..");
    }
}
