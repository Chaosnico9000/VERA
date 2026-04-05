using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace VERA.Platforms.Android.Services
{
    [Service(Exported = false, ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class UpdateDownloadService : Service
    {
        public const string ChannelId       = "vera_update_channel";
        public const int    NotificationId  = 2001;

        public const string ActionStart     = "VERA_UPDATE_START";
        public const string ExtraUrl        = "update_url";

        // Broadcasts zurück an die UI
        public const string BroadcastAction   = "VERA_UPDATE_PROGRESS";
        public const string ExtraProgress     = "progress";      // 0.0–1.0, -1 = Fehler, 2.0 = fertig
        public const string ExtraStatusText   = "status_text";

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var url = intent?.GetStringExtra(ExtraUrl);
            if (string.IsNullOrEmpty(url))
            {
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            EnsureChannel();
            var notification = BuildNotification("Update wird heruntergeladen…", 0, true);

#pragma warning disable CA1416
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                StartForeground(NotificationId, notification, ForegroundService.TypeDataSync);
            else
                StartForeground(NotificationId, notification);
#pragma warning restore CA1416

            _ = RunDownloadAsync(url);

            return StartCommandResult.NotSticky;
        }

        private async Task RunDownloadAsync(string downloadUrl)
        {
            try
            {
                var context  = ApplicationContext!;
                var cacheDir = context.CacheDir!.AbsolutePath;
                var apkPath  = Path.Combine(cacheDir, "vera-update.apk");

                if (File.Exists(apkPath)) File.Delete(apkPath);

                using var http = new System.Net.Http.HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "VERA-App");
                http.Timeout = TimeSpan.FromMinutes(10);

                using var response = await http.GetAsync(downloadUrl,
                    System.Net.Http.HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var total   = response.Content.Headers.ContentLength ?? -1L;
                var buffer  = new byte[262144]; // 256 KB
                long done   = 0;

                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using var file   = new FileStream(apkPath, FileMode.Create, FileAccess.Write,
                                             FileShare.None, bufferSize: 262144, useAsync: true);
                int read;
                while ((read = await stream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
                    done += read;

                    if (total > 0)
                    {
                        var pct = (double)done / total;
                        UpdateNotification((int)(pct * 100), total);
                        SendBroadcast(pct, $"⬇️  {pct:P0} heruntergeladen…");
                    }
                }
                await file.FlushAsync().ConfigureAwait(false);

                // Fertig — Installer starten
                UpdateNotification(100, total, done: true);
                SendBroadcast(2.0, "✅  Download abgeschlossen");

                var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                    context,
                    context.PackageName + ".fileprovider",
                    new Java.IO.File(apkPath));

                var installIntent = new Intent(Intent.ActionView);
                installIntent.SetDataAndType(uri, "application/vnd.android.package-archive");
                installIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
                installIntent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(installIntent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateDownloadService] Fehler: {ex.Message}");
                SendBroadcast(-1.0, "❌  Download fehlgeschlagen");
            }
            finally
            {
#pragma warning disable CA1416
                StopForeground(StopForegroundFlags.Remove);
#pragma warning restore CA1416
                StopSelf();
            }
        }

        // ── Notifications ──────────────────────────────────────────────────────
        private Notification BuildNotification(string text, int progressPct, bool indeterminate = false, bool done = false)
        {
            var launchIntent = new Intent(this, typeof(global::VERA.MainActivity));
            launchIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
#pragma warning disable CA1416
            var pendingIntent = PendingIntent.GetActivity(
                this, 0, launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
#pragma warning restore CA1416

            var builder = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("VERA Update")
                .SetContentText(text)
                .SetSmallIcon(global::Android.Resource.Drawable.StatSysDownload)
                .SetContentIntent(pendingIntent)
                .SetOnlyAlertOnce(true)
                .SetPriority(NotificationCompat.PriorityLow)
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetColor(unchecked((int)0xFF3566E5));

            if (done)
            {
                builder.SetSmallIcon(global::Android.Resource.Drawable.StatSysDownloadDone)
                       .SetOngoing(false)
                       .SetAutoCancel(true);
            }
            else
            {
                builder.SetOngoing(true)
                       .SetProgress(100, progressPct, indeterminate);
            }

            return builder.Build()!;
        }

        private void UpdateNotification(int progressPct, long totalBytes, bool done = false)
        {
            var text = done
                ? "Download abgeschlossen — tippen zum Installieren"
                : totalBytes > 0
                    ? $"{progressPct}% von {totalBytes / 1024 / 1024} MB"
                    : $"{progressPct}%";

            var notification = BuildNotification(text, progressPct, indeterminate: false, done: done);
            var manager      = (NotificationManager?)GetSystemService(NotificationService);
            manager?.Notify(NotificationId, notification);
        }

        private void SendBroadcast(double progress, string statusText)
        {
            var intent = new Intent(BroadcastAction);
            intent.PutExtra(ExtraProgress, progress);
            intent.PutExtra(ExtraStatusText, statusText);
            SendBroadcast(intent);
        }

        private void EnsureChannel()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
            var manager = (NotificationManager?)GetSystemService(NotificationService);
            if (manager?.GetNotificationChannel(ChannelId) != null) return;
            var channel = new NotificationChannel(ChannelId, "VERA Updates", NotificationImportance.Low)
            {
                Description = "Fortschritt beim Herunterladen von App-Updates"
            };
            channel.SetShowBadge(false);
            manager?.CreateNotificationChannel(channel);
        }
    }
}
