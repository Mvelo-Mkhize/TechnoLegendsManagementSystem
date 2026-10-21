using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoLegendsMobile.Api.Data;
using TechnoLegendsManagementSystem.Shared.Models;

namespace TechnoLegendsMobile.Api.Controllers;

public record CommentRequest(string Text);

[ApiController]
public class TasksController(AppDb db) : ControllerBase
{
    private Guid Me => Guid.Parse(User.FindFirst("sub")!.Value);

    private IQueryable<TaskEntity> Query() => db.Tasks.AsNoTracking().AsSplitQuery()
        .Include(t => t.Project)
        .Include(t => t.Reporter)
        .Include(t => t.Assignees).ThenInclude(a => a.User)
        .Include(t => t.Scope)
        .Include(t => t.Subtasks)
        .Include(t => t.Attachments)
        .Include(t => t.Comments).ThenInclude(c => c.Author);

    [HttpGet("api/tasks")]
    public async Task<List<TaskItem>> GetAll()
    {
        var me = Me;
        var list = await Query().OrderBy(t => t.Due).ToListAsync();
        return list.Select(t => ToDto(t, me)).ToList();
    }

    [HttpGet("api/tasks/{id}")]
    public async Task<ActionResult<TaskItem>> Get(string id)
    {
        if (!TryParseId(id, out var key)) return NotFound();
        var t = await Query().FirstOrDefaultAsync(x => x.Id == key);
        if (t is null) return NotFound();
        return ToDto(t, Me);
    }

    [HttpPost("api/tasks")]
    public async Task<ActionResult<TaskItem>> Create(NewTask n)
    {
        var me = Me;

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Name == n.Project);
        if (project is null) return BadRequest("Unknown project.");

        var profile = await db.Profiles.FindAsync(me);
        if (profile is null) return BadRequest("No profile row for this user.");

        // NOTE: assignees are matched by full name for now. Team names match nothing and are ignored.
        var people = await db.Profiles.Where(p => n.Assignees.Contains(p.FullName)).ToListAsync();

        var e = new TaskEntity
        {
            Title = n.Title.Trim(),
            Description = n.Description,
            Status = WorkStatus.ToDo,
            Priority = n.Priority,
            Department = profile.Department,
            ProjectId = project.Id,
            Due = n.Due.Date.AddHours(17),
            Points = n.Points,
            ReporterId = me,
            Assignees = people
                .Select((p, i) => new TaskAssignee { UserId = p.Id, Role = i == 0 ? "Primary" : "Reviewer" })
                .ToList()
        };

        db.Tasks.Add(e);
        await db.SaveChangesAsync();

        try
        {
            db.AddNotifications(people.Select(p => p.Id).Where(u => u != me),
                "task", $"{profile.FullName} assigned you a task", e.Title, $"tasks/TL-{e.Id}");
            await db.SaveChangesAsync();
        }
        catch { /* the task is saved; a failed notification shouldn't fail the request */ }

        var saved = await Query().FirstAsync(t => t.Id == e.Id);
        return ToDto(saved, me);
    }

    [HttpPost("api/tasks/{id}/comments")]
    public async Task<IActionResult> AddComment(string id, CommentRequest req)
    {
        if (!TryParseId(id, out var key) || string.IsNullOrWhiteSpace(req.Text)) return BadRequest();

        var task = await db.Tasks.AsNoTracking().Where(t => t.Id == key)
            .Select(t => new { t.Title, t.ReporterId, Assignees = t.Assignees.Select(a => a.UserId).ToList() })
            .FirstOrDefaultAsync();
        if (task is null) return NotFound();

        var me = Me;
        db.Comments.Add(new CommentEntity { TaskId = key, AuthorId = me, Text = req.Text.Trim() });
        await db.SaveChangesAsync();

        try
        {
            var who = await db.Profiles.Where(p => p.Id == me).Select(p => p.FullName).FirstOrDefaultAsync() ?? "Someone";
            var targets = task.Assignees.Append(task.ReporterId).Where(u => u != me);
            db.AddNotifications(targets, "task", $"{who} commented on a task", task.Title, $"tasks/TL-{key}");
            await db.SaveChangesAsync();
        }
        catch { }

        return NoContent();
    }

    [HttpGet("api/lookups")]
    public async Task<AssignLookups> Lookups()
    {
        var people = await db.Profiles.AsNoTracking().OrderBy(p => p.FullName).ToListAsync();
        return new AssignLookups(
            await db.Projects.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(),
            people.Select(ToPerson).ToList(),
            people.Select(p => p.Department).Where(d => d != "").Distinct().ToList(),
            DateTime.Today.AddDays(5)); // TODO: real sprint table
    }

    // ---- helpers ----

    private static bool TryParseId(string id, out long key)
    {
        key = 0;
        return id.StartsWith("TL-") && long.TryParse(id[3..], out key);
    }

    private static Person ToPerson(Profile? p) => new(p?.FullName ?? "Unknown", p?.Title ?? "");

    private static string Ago(DateTime utc)
    {
        var s = DateTime.UtcNow - utc;
        if (s.TotalMinutes < 1) return "just now";
        if (s.TotalHours < 1) return $"{(int)s.TotalMinutes}m ago";
        if (s.TotalDays < 1) return $"{(int)s.TotalHours}h ago";
        return $"{(int)s.TotalDays}d ago";
    }

    private static TaskItem ToDto(TaskEntity t, Guid me) => new()
    {
        Id = $"TL-{t.Id}",
        Title = t.Title,
        Description = t.Description,
        Status = t.Status,
        Priority = t.Priority,
        Department = t.Department,
        Project = t.Project?.Name ?? "",
        Epic = t.Project?.Name ?? "",
        Sprint = t.Sprint,
        Due = t.Due,
        Points = t.Points,
        Badge = t.Badge,
        BadgeNote = t.BadgeNote,
        Reporter = ToPerson(t.Reporter),
        Assignees = t.Assignees.Select(a => new Assignee(ToPerson(a.User), a.Role)).ToList(),
        Scope = t.Scope.OrderBy(s => s.Id).Select(s => new ScopeItem(s.Text, s.Done)).ToList(),
        Subtasks = t.Subtasks.OrderBy(s => s.Id)
            .Select(s => new SubtaskItem(s.Title, s.Done, s.DoneAt is null ? "" : Ago(s.DoneAt.Value))).ToList(),
        Attachments = t.Attachments.Select(a => new Attachment(a.Name, a.Meta)).ToList(),
        Comments = t.Comments.OrderBy(c => c.CreatedAt)
            .Select(c => new Comment(c.Author?.FullName ?? "Unknown",
                                     c.AuthorId == me ? "You" : (c.Author?.Title ?? ""),
                                     Ago(c.CreatedAt), c.Text, c.AuthorId == me)).ToList()
    };
}