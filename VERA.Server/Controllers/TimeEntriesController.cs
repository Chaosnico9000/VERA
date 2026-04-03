using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VERA.Server.Data;
using VERA.Server.Services;
using VERA.Shared.Dto;

namespace VERA.Server.Controllers
{
    [ApiController]
    [Route("api/entries")]
    [Authorize]
    public class TimeEntriesController : ControllerBase
    {
        private readonly VeraDbContext _db;

        public TimeEntriesController(VeraDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId  = AuthService.GetUserId(User);
            var entries = await _db.TimeEntries
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.StartTime)
                .Select(e => new TimeEntryDto(e.Id, e.Title, e.Category, e.StartTime, e.EndTime, e.Type))
                .ToListAsync();
            return Ok(entries);
        }

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] UpsertTimeEntryRequest req)
        {
            var userId = AuthService.GetUserId(User);

            if (req.EndTime.HasValue && req.EndTime.Value <= req.StartTime)
                return BadRequest(new VERA.Shared.Dto.ApiError("INVALID_TIME", "EndTime muss nach StartTime liegen."));

            if (req.Id.HasValue)
            {
                var existing = await _db.TimeEntries
                    .FirstOrDefaultAsync(e => e.Id == req.Id.Value && e.UserId == userId);
                if (existing is null) return NotFound();

                existing.Title     = req.Title;
                existing.Category  = req.Category;
                existing.StartTime = req.StartTime;
                existing.EndTime   = req.EndTime;
                existing.Type      = req.Type;
            }
            else
            {
                _db.TimeEntries.Add(new TimeEntry
                {
                    Id        = Guid.NewGuid(),
                    UserId    = userId,
                    Title     = req.Title,
                    Category  = req.Category,
                    StartTime = req.StartTime,
                    EndTime   = req.EndTime,
                    Type      = req.Type,
                });
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = AuthService.GetUserId(User);
            var entry  = await _db.TimeEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
            if (entry is null) return NotFound();
            _db.TimeEntries.Remove(entry);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromBody] List<TimeEntryDto> clientEntries)
        {
            if (clientEntries is null || clientEntries.Count == 0)
                return BadRequest(new VERA.Shared.Dto.ApiError("EMPTY_SYNC", "Keine Einträge übermittelt."));

            var userId   = AuthService.GetUserId(User);
            var serverIds = await _db.TimeEntries
                .Where(e => e.UserId == userId)
                .Select(e => e.Id)
                .ToHashSetAsync();

            foreach (var dto in clientEntries)
            {
                if (serverIds.Contains(dto.Id))
                {
                    var e = await _db.TimeEntries.FindAsync(dto.Id);
                    if (e is null || e.UserId != userId) continue;
                    e.Title = dto.Title; e.Category = dto.Category;
                    e.StartTime = dto.StartTime; e.EndTime = dto.EndTime; e.Type = dto.Type;
                }
                else
                {
                    _db.TimeEntries.Add(new TimeEntry
                    {
                        Id = dto.Id, UserId = userId,
                        Title = dto.Title, Category = dto.Category,
                        StartTime = dto.StartTime, EndTime = dto.EndTime, Type = dto.Type,
                    });
                }
            }
            await _db.SaveChangesAsync();

            var all = await _db.TimeEntries
                .Where(e => e.UserId == userId)
                .Select(e => new TimeEntryDto(e.Id, e.Title, e.Category, e.StartTime, e.EndTime, e.Type))
                .ToListAsync();
            return Ok(all);
        }
    }
}
