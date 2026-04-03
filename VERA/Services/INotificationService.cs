namespace VERA.Services
{
    public interface INotificationService
    {
        void StartTimerNotification(string taskTitle);
        void UpdateTimerNotification(string timerText, string taskTitle);
        void StopTimerNotification();
        void SendReminderNotification(string title, string message);
    }
}
