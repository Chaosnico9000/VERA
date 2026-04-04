namespace VERA.Services
{
    public class DefaultNotificationService : INotificationService
    {
        public void StartTimerNotification(string taskTitle) { }
        public void UpdateTimerNotification(string timerText, string taskTitle) { }
        public void StopTimerNotification() { }
        public void SendReminderNotification(string title, string message) { }
    }
}
