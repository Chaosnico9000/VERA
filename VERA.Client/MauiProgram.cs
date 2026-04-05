using Microsoft.Extensions.Logging;
using VERA.Services;
using VERA.ViewModels;

namespace VERA
{
    public static class MauiProgram
    {
        public static IServiceProvider Services { get; private set; } = null!;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<ITimeTrackingService, TimeTrackingService>();
            builder.Services.AddSingleton<GamificationService>();
            builder.Services.AddSingleton<AccountService>();
            builder.Services.AddSingleton<UpdateService>();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<ApiClient>(sp =>
            {
                var client = new ApiClient();
                var url = Preferences.Default.Get("server_url", string.Empty);
                if (!string.IsNullOrEmpty(url)) client.SetBaseUrl(url);
                return client;
            });

#if ANDROID
            builder.Services.AddSingleton<INotificationService,
                VERA.Platforms.Android.Services.AndroidNotificationService>();
            builder.Services.AddSingleton<IAuthService,
                VERA.Platforms.Android.Services.AndroidBiometricService>();
#else
            builder.Services.AddSingleton<INotificationService, DefaultNotificationService>();
            builder.Services.AddSingleton<IAuthService, DefaultAuthService>();
#endif

            // ViewModels
            builder.Services.AddTransient<DartsViewModel>();
            builder.Services.AddSingleton<DashboardViewModel>();
            builder.Services.AddSingleton<GamificationViewModel>();
            builder.Services.AddSingleton<HistoryViewModel>();
            builder.Services.AddSingleton<StatistikViewModel>();
            builder.Services.AddTransient<EinstellungenViewModel>();
            builder.Services.AddTransient<EditEntryViewModel>();

            // Pages (transient für Navigation)
            builder.Services.AddTransient<Views.DartsPage>();
            builder.Services.AddTransient<Views.EditEntryPage>();
            builder.Services.AddTransient<Views.LockPage>();
            builder.Services.AddTransient<Views.LevelingPage>();
            builder.Services.AddTransient<Views.RegisterPage>();
            builder.Services.AddTransient<Views.LoginPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            Services = app.Services;
            return app;
        }
    }
}
