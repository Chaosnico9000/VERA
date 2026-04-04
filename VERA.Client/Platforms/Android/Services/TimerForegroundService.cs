using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace VERA.Platforms.Android.Services
{
    [Service(Exported = false, ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class TimerForegroundService : Service
    {
        public const string ChannelId        = "vera_timer_channel";
        public const string ReminderChannelId = "vera_reminder_channel";
        public const string ActionStart      = "VERA_START";
        public const string ActionUpdate     = "VERA_UPDATE";
        public const string ActionStop       = "VERA_STOP";
        public const string ExtraTimerText   = "timer_text";
        public const string ExtraTaskTitle   = "task_title";
        public const int    NotificationId   = 1001;
        public const int    ReminderId       = 1002;

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var action = intent?.Action ?? ActionStart;

            if (action == ActionStop)
            {
#pragma warning disable CA1416
                StopForeground(StopForegroundFlags.Remove);
#pragma warning restore CA1416
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            var timerText = intent?.GetStringExtra(ExtraTimerText) ?? "00:00:00";
            var taskTitle = intent?.GetStringExtra(ExtraTaskTitle) ?? "Timer";

            EnsureTimerChannel();
            var notification = BuildTimerNotification(timerText, taskTitle);

#pragma warning disable CA1416
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                StartForeground(NotificationId, notification, ForegroundService.TypeDataSync);
            else
                StartForeground(NotificationId, notification);
#pragma warning restore CA1416

            return StartCommandResult.Sticky;
        }

        // ── Reminder-Benachrichtigung (statisch, kein Foreground-Service nötig) ──
        public static void ShowReminder(Context context, string title, string message)
        {
            EnsureReminderChannel(context);

            var launchIntent = new Intent(context, typeof(global::VERA.MainActivity));
            launchIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
#pragma warning disable CA1416
            var pendingIntent = PendingIntent.GetActivity(
                context, ReminderId, launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
#pragma warning restore CA1416

            var notification = new NotificationCompat.Builder(context, ReminderChannelId)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent)
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetColor(unchecked((int)0xFF00C8F0))
                .Build()!;

            var manager = NotificationManagerCompat.From(context);
            manager!.Notify(ReminderId, notification);
        }

        private void EnsureTimerChannel()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
            var manager = (NotificationManager?)GetSystemService(NotificationService);
            if (manager?.GetNotificationChannel(ChannelId) != null) return;
            var channel = new NotificationChannel(ChannelId, "VERA Timer", NotificationImportance.Low)
            {
                Description = "Laufender Zeiterfassungs-Timer"
            };
            channel.SetShowBadge(true);
            manager?.CreateNotificationChannel(channel);
        }

        private static void EnsureReminderChannel(Context context)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
            var manager = (NotificationManager?)context.GetSystemService(NotificationService);
            if (manager?.GetNotificationChannel(ReminderChannelId) != null) return;
            var channel = new NotificationChannel(ReminderChannelId, "VERA Erinnerungen", NotificationImportance.High)
            {
                Description = "Erinnerungen zur Zeiterfassung"
            };
            channel.SetShowBadge(true);
            channel.EnableVibration(true);
            manager?.CreateNotificationChannel(channel);
        }

        private Notification BuildTimerNotification(string timerText, string taskTitle)
        {
            var launchIntent = new Intent(this, typeof(global::VERA.MainActivity));
            launchIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

#pragma warning disable CA1416
            var pendingIntent = PendingIntent.GetActivity(
                this, 0, launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
#pragma warning restore CA1416

            return new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle($"VERA  \u23f1  {timerText}")
                .SetContentText(taskTitle)
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                .SetOngoing(true)
                .SetContentIntent(pendingIntent)
                .SetCategory(NotificationCompat.CategoryService)
                .SetPriority(NotificationCompat.PriorityLow)
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetColor(unchecked((int)0xFF3566E5))
                .SetColorized(true)
                .Build()!;
        }
    }
}