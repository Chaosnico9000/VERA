using VERA.Models;

namespace VERA.Services
{
    public interface ITimeTrackingService
    {
        Task<List<TimeEntry>> GetEntriesAsync();
        Task<TimeEntry?> GetActiveEntryAsync();
        Task<TimeEntry> StartTimerAsync(string title, string category = "");
        Task StopTimerAsync(Guid id);
        Task DeleteEntryAsync(Guid id);
        Task<TimeEntry> AddSonderTagAsync(EntryType type, string title, DateTime date);
        Task UpdateEntryAsync(TimeEntry entry);
        Task AddManualEntryAsync(TimeEntry entry);
    }
}
