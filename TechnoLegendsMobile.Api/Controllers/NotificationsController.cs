using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoLegendsMobile.Api.Data;
using TechnoLegendsManagementSystem.Shared.Models;

namespace TechnoLegendsMobile.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(AppDb db) : ControllerBase
{
    private Guid Me => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<NotificationsResponse> List()
    {
        var me = Me;
        var items = await db.Notifications.AsNoTracking()
            .Where(n => n.UserId == me)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(n.Id, n.Kind, n.Title, n.Body, n.Link, n.CreatedAt, n.ReadAt != null))
            .ToListAsync();

        var unread = await db.Notifications.CountAsync(n => n.UserId == me && n.ReadAt == null);
        return new NotificationsResponse(items, unread);
    }

    [HttpGet("unread-count")]
    public async Task<int> UnreadCount()
    {
        var me = Me;
        return await db.Notifications.CountAsync(n => n.UserId == me && n.ReadAt == null);
    }

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> Read(long id)
    {
        var me = Me;
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == me);
        if (n is null) return NotFound();
        n.ReadAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll()
    {
        var me = Me;
        var now = (DateTime?)DateTime.UtcNow;
        await db.Notifications.Where(n => n.UserId == me && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now));
        return NoContent();
    }
}