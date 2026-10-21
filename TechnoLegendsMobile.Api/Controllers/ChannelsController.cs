using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TechnoLegendsMobile.Api.Data;
using TechnoLegendsManagementSystem.Shared.Models;

namespace TechnoLegendsMobile.Api.Controllers;

[ApiController]
[Route("api/channels")]
public class ChannelsController(AppDb db, IHubContext<ChatHub> hub) : ControllerBase
{
    private Guid Me => Guid.Parse(User.FindFirst("sub")!.Value);

    private Task<bool> IsMember(long channelId, Guid me) =>
        db.ChannelMembers.AnyAsync(m => m.ChannelId == channelId && m.UserId == me);

    private static MessageDto ToDto(MessageEntity m) => new(
        m.Id, m.ChannelId, m.SenderId,
        m.Sender?.FullName ?? "Unknown", m.Sender?.Title ?? "",
        m.Text, m.CreatedAt);

    [HttpGet("me")]
    public async Task<ActionResult<PersonDto>> GetMe()
    {
        var me = Me;
        var p = await db.Profiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == me);
        return p is null ? NotFound() : new PersonDto(p.Id, p.FullName, p.Title);
    }

    [HttpGet("people")]
    public async Task<List<PersonDto>> People()
    {
        var me = Me;
        return await db.Profiles.AsNoTracking()
            .Where(p => p.Id != me)
            .OrderBy(p => p.FullName)
            .Select(p => new PersonDto(p.Id, p.FullName, p.Title))
            .ToListAsync();
    }

    [HttpGet]
    public async Task<List<ChannelDto>> GetAll()
    {
        var me = Me;

        var rows = await db.ChannelMembers.AsNoTracking()
            .Where(m => m.UserId == me)
            .Select(m => new
            {
                Id = m.ChannelId,
                Name = m.Channel!.Name,
                Kind = m.Channel.Kind,
                Unread = db.Messages.Count(x => x.ChannelId == m.ChannelId
                                                && x.CreatedAt > m.LastReadAt
                                                && x.SenderId != me),
                Last = db.Messages.Where(x => x.ChannelId == m.ChannelId)
                                  .OrderByDescending(x => x.Id)
                                  .Select(x => new { x.Text, Sender = x.Sender!.FullName, x.CreatedAt })
                                  .FirstOrDefault(),
                Members = db.ChannelMembers.Count(x => x.ChannelId == m.ChannelId)
            })
            .ToListAsync();

        // a DM shows the other person's name
        var dmIds = rows.Where(r => r.Kind == "dm").Select(r => r.Id).ToList();
        var others = await db.ChannelMembers.AsNoTracking()
            .Where(m => dmIds.Contains(m.ChannelId) && m.UserId != me)
            .Select(m => new { m.ChannelId, Name = m.User!.FullName })
            .ToListAsync();
        var otherNames = others.GroupBy(o => o.ChannelId).ToDictionary(g => g.Key, g => g.First().Name);

        return rows
            .Select(r => new ChannelDto(
                r.Id,
                r.Kind == "dm" && otherNames.TryGetValue(r.Id, out var n) ? n : r.Name,
                r.Kind, r.Last?.Text, r.Last?.Sender, r.Last?.CreatedAt, r.Unread, r.Members))
            .OrderByDescending(c => c.LastAt ?? DateTime.MinValue)
            .ThenBy(c => c.Name)
            .ToList();
    }

    [HttpGet("{id:long}/messages")]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(long id, [FromQuery] long? before, [FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 100);
        if (!await IsMember(id, Me)) return Forbid();

        var q = db.Messages.AsNoTracking().Where(m => m.ChannelId == id);
        if (before is not null) q = q.Where(m => m.Id < before);

        var rows = await q.OrderByDescending(m => m.Id).Take(limit).Include(m => m.Sender).ToListAsync();
        rows.Reverse(); // oldest first, ready to display
        return rows.Select(ToDto).ToList();
    }

    [HttpPost("{id:long}/messages")]
    public async Task<ActionResult<MessageDto>> Send(long id, SendMessageRequest req)
    {
        var me = Me;
        var text = (req.Text ?? "").Trim();
        if (text.Length is 0 or > 4000) return BadRequest("Message must be 1 to 4000 characters.");
        if (!await IsMember(id, me)) return Forbid();

        var e = new MessageEntity { ChannelId = id, SenderId = me, Text = text };
        db.Messages.Add(e);
        await db.SaveChangesAsync();

        e.Sender = await db.Profiles.AsNoTracking().FirstAsync(p => p.Id == me);
        var dto = ToDto(e);

        try
        {
            var memberIds = await db.ChannelMembers.Where(m => m.ChannelId == id).Select(m => m.UserId).ToListAsync();
            await hub.Clients.Users(memberIds.Select(g => g.ToString()).ToList())
                             .SendAsync("MessageReceived", dto);
        }
        catch { /* message is saved; live push is best-effort */ }

        return dto;
    }

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        var me = Me;
        var m = await db.ChannelMembers.FirstOrDefaultAsync(x => x.ChannelId == id && x.UserId == me);
        if (m is null) return Forbid();
        m.LastReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("dm")]
    public async Task<ActionResult<long>> StartDm(CreateDmRequest req)
    {
        var me = Me;
        if (req.UserId == me) return BadRequest("You can't message yourself.");
        if (!await db.Profiles.AnyAsync(p => p.Id == req.UserId)) return NotFound();

        var a = me.ToString(); var b = req.UserId.ToString();
        var key = string.CompareOrdinal(a, b) < 0 ? $"dm-{a}-{b}" : $"dm-{b}-{a}";

        var existing = await db.Channels.Where(c => c.Name == key).Select(c => c.Id).FirstOrDefaultAsync();
        if (existing != 0) return existing;

        try
        {
            var ch = new ChannelEntity
            {
                Name = key,
                Kind = "dm",
                CreatedBy = me,
                Members = new() { new ChannelMemberEntity { UserId = me }, new ChannelMemberEntity { UserId = req.UserId } }
            };
            db.Channels.Add(ch);
            await db.SaveChangesAsync();
            return ch.Id;
        }
        catch (DbUpdateException)
        {
            // someone created it at the same moment
            return await db.Channels.Where(c => c.Name == key).Select(c => c.Id).FirstAsync();
        }
    }

    [HttpPost]
    public async Task<ActionResult<long>> CreateChannel(CreateChannelRequest req)
    {
        var me = Me;
        var role = await db.Profiles.Where(p => p.Id == me).Select(p => p.Role).FirstOrDefaultAsync();
        if (role != "admin") return Forbid();

        var name = Regex.Replace((req.Name ?? "").Trim().ToLowerInvariant(), @"\s+", "-");
        if (!Regex.IsMatch(name, "^[a-z0-9-]{2,30}$"))
            return BadRequest("Channel names use 2 to 30 letters, numbers or dashes.");
        if (await db.Channels.AnyAsync(c => c.Name == name))
            return BadRequest("A channel with that name already exists.");

        var everyone = await db.Profiles.Select(p => p.Id).ToListAsync();
        var ch = new ChannelEntity
        {
            Name = name,
            Kind = "channel",
            CreatedBy = me,
            Members = everyone.Select(id => new ChannelMemberEntity { UserId = id }).ToList()
        };
        db.Channels.Add(ch);
        await db.SaveChangesAsync();
        return ch.Id;
    }
}