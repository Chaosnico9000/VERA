using Android.App;
using Android.Content.PM;
using Android.OS;

namespace VERA
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
              LaunchMode = LaunchMode.SingleTop,
              ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
                                     ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
                                     ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // POST_NOTIFICATIONS-Berechtigung für Android 13+ anfordern
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                RequestPermissions(["android.permission.POST_NOTIFICATIONS"], 0);
            }
        }
    }
}
