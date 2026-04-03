using VERA.Views;

namespace VERA
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
            Routing.RegisterRoute(nameof(ZeitTrackerPage), typeof(ZeitTrackerPage));
            Routing.RegisterRoute(nameof(StatistikPage), typeof(StatistikPage));
            Routing.RegisterRoute(nameof(EinstellungenPage), typeof(EinstellungenPage));
            Routing.RegisterRoute(nameof(EditEntryPage), typeof(EditEntryPage));
            Routing.RegisterRoute(nameof(LevelingPage), typeof(LevelingPage));
        }
    }
}
