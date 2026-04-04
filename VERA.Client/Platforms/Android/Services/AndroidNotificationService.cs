using Android.Content;
using Android.OS;
using VERA.Services;

namespace VERA.Platforms.Android.Services
{
    public class AndroidNotificationService : INotificationService
    {
        private static Intent CreateIntent(string action, string timerText, string taskTitle)
        {
            var intent = new Intent(Platform.AppContext, typeof(TimerForegroundService));
            intent.SetAction(action);
            intent.PutExtra(TimerForegroundService.ExtraTimerText, timerText);
            intent.PutExtra(TimerForegroundService.ExtraTaskTitle, taskTitle);
            return intent;
        }

        private static void Start(Intent intent)
        {
#pragma warning disable CA1416
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                Platform.AppContext.StartForegroundService(intent);
            else
                Platform.AppContext.StartService(intent);
#pragma warning restore CA1416
        }

        public void StartTimerNotification(string taskTitle)
            => Start(CreateIntent(TimerForegroundService.ActionStart, "00:00:00", taskTitle));

        public void UpdateTimerNotification(string timerText, string taskTitle)
            => Start(CreateIntent(TimerForegroundService.ActionUpdate, timerText, taskTitle));

        public void StopTimerNotification()
        {
            var intent = new Intent(Platform.AppContext, typeof(TimerForegroundService));
            intent.SetAction(TimerForegroundService.ActionStop);
            Platform.AppContext.StartService(intent);
        }

        public void SendReminderNotification(string title, string message)
            => TimerForegroundService.ShowReminder(Platform.AppContext, title, message);
    }
}
