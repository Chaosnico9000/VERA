using Android.Content;

namespace VERA.Platforms.Android.Services
{
    [BroadcastReceiver(Exported = false)]
    public class UpdateProgressReceiver : BroadcastReceiver
    {
        private Action<double, string>? _onProgress;

        // Parameterloser Konstruktor für Android-Manifest-Generierung
        public UpdateProgressReceiver() { }

        public UpdateProgressReceiver(Action<double, string> onProgress)
        {
            _onProgress = onProgress;
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action != UpdateDownloadService.BroadcastAction) return;
            var progress   = intent.GetDoubleExtra(UpdateDownloadService.ExtraProgress, 0.0);
            var statusText = intent.GetStringExtra(UpdateDownloadService.ExtraStatusText) ?? string.Empty;
            _onProgress?.Invoke(progress, statusText);
        }
    }
}
