using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoLegendsMobile.Api.Data;
using TechnoLegendsManagementSystem.Shared.Models;

namespace TechnoLegendsMobile.Api.Controllers;

[ApiController]
[Route("api/meetings")]
public class MeetingsController(AppDb db) : ControllerBase
{
    private const int LateAfterMinutes = 10;
    private const int CheckInOpensMinutes = 15;

    private Guid Me => Guid.Parse(User.FindFirst("sub")!.Value);

    private static string StatusOf(DateTime start, DateTime joined) =>
        joined > start.AddMinutes(LateAfterMinutes) ? "Late" : "OnTime";

    [HttpGet]
    public async Task<MeetingsOverviewDto> Overview()
    {
        var me = Me;
        var now = DateTime.UtcNow;

        var upcoming = await db.Meetings.AsNoTracking().Include(m => m.Creator) 
            .Where(m => m.EndsAt > now).OrderBy(m => m.StartsAt).Take(20).ToListAsync();
        var past = await db.Meetings.AsNoTracking().Include(m => m.Creator)
            .Where(m => m.EndsAt <= now).OrderByDescending(m => m.StartsAt).Take(30).ToListAsync();

        var ids = upcoming.Concat(past).Select(m => m.Id).ToList();

        var counts = await db.MeetingAttendance.AsNoTracking()
            .Where(a => ids.Contains(a.MeetingId))
            .GroupBy(a => a.MeetingId)
            .Select(g => new { Id = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.N);

        var mine = await db.MeetingAttendance.AsNoTracking()
            .Where(a => ids.Contains(a.MeetingId) && a.UserId == me)
            .ToDictionaryAsync(a => a.MeetingId, a => a.JoinedAt);

        MeetingDto Map(MeetingEntity m)
        {
            DateTime? joined = mine.TryGetValue(m.Id, out var j) ? j : null;
            string? status = joined is not null ? StatusOf(m.StartsAt, joined.Value)
                           : m.EndsAt <= now ? "Missed" : null;
            return new MeetingDto(m.Id, m.Title, m.StartsAt, m.EndsAt,
                m.Creator?.FullName ?? "Unknown", counts.GetValueOrDefault(m.Id), status);
        }

        var pastDtos = past.Select(Map).ToList();
        var summary = new AttendanceSummaryDto(
            pastDtos.Count,
            pastDtos.Count(m => m.MyStatus == "OnTime"),
            pastDtos.Count(m => m.MyStatus == "Late"),
            pastDtos.Count(m => m.MyStatus == "Missed"));

        return new MeetingsOverviewDto(upcoming.Select(Map).ToList(), pastDtos, summary);
    }

    [HttpGet("{id:guid}/attendees")]
    public async Task<ActionResult<List<AttendeeDto>>> Attendees(Guid id)
    {
        var m = await db.Meetings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return NotFound();

        var rows = await db.MeetingAttendance.AsNoTracking().Include(a => a.User)
            .Where(a => a.MeetingId == id).OrderBy(a => a.JoinedAt).ToListAsync();

        return rows.Select(a => new AttendeeDto(
            a.User?.FullName ?? "Unknown", a.JoinedAt, StatusOf(m.StartsAt, a.JoinedAt))).ToList();
    }

    [HttpPost("{id:guid}/join")]
    public async Task<ActionResult<JoinResultDto>> Join(Guid id)
    {
        var me = Me;
        var now = DateTime.UtcNow;

        var m = await db.Meetings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return NotFound();
        if (now < m.StartsAt.AddMinutes(-CheckInOpensMinutes))
            return BadRequest($"Check-in opens {CheckInOpensMinutes} minutes before the meeting starts.");
        if (now >= m.EndsAt) return BadRequest("This meeting has already ended.");

        var exists = await db.MeetingAttendance.AnyAsync(a => a.MeetingId == id && a.UserId == me);
        if (!exists)
        {
            try
            {
                db.MeetingAttendance.Add(new MeetingAttendanceEntity { MeetingId = id, UserId = me, JoinedAt = now });
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException) { /* double tap: already recorded */ }
        }

        return new JoinResultDto(m.JoinUrl);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateMeetingRequest req)
    {
        var title = (req.Title ?? "").Trim();
        if (title.Length is < 3 or > 120) return BadRequest("Title must be 3 to 120 characters.");

        if (!Uri.TryCreate((req.JoinUrl ?? "").Trim(), UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
            return BadRequest("Paste the full meeting link. It must start with https://");

        if (req.DurationMinutes is < 5 or > 480) return BadRequest("Duration must be 5 to 480 minutes.");

        var start = req.StartsAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(req.StartsAt, DateTimeKind.Utc)
            : req.StartsAt.ToUniversalTime();
        if (start < DateTime.UtcNow.AddMinutes(-5)) return BadRequest("The start time can't be in the past.");

        var m = new MeetingEntity
        {
            Id = Guid.NewGuid(),
            Title = title,
            StartsAt = start,
            EndsAt = start.AddMinutes(req.DurationMinutes),
            JoinUrl = url.AbsoluteUri,
            CreatedBy = Me
        };
        db.Meetings.Add(m);
        await db.SaveChangesAsync();

        try
        {
            var host = await db.Profiles.Where(p => p.Id == m.CreatedBy).Select(p => p.FullName).FirstOrDefaultAsync() ?? "Someone";
            var everyone = await db.Profiles.Where(p => p.Id != m.CreatedBy).Select(p => p.Id).ToListAsync();
            db.AddNotifications(everyone, "meeting", $"{host} scheduled a meeting", m.Title, "attendance");
            await db.SaveChangesAsync();
        }
        catch { }

        return m.Id;
    }
}