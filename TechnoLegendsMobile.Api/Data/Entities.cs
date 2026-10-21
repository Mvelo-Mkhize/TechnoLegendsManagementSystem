using Microsoft.EntityFrameworkCore;
using TechnoLegendsManagementSystem.Shared.Models;

namespace TechnoLegendsMobile.Api.Data
{
    public class Profile
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Role { get; set; } = "member";
        public string Department { get; set; } = "";
    }

    public class Project
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class TaskEntity
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public WorkStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public string Department { get; set; } = "";
        public Guid ProjectId { get; set; }
        public Project? Project { get; set; }
        public string Sprint { get; set; } = "Sprint 42";
        public DateTime Due { get; set; }
        public int Points { get; set; }
        public string? Badge { get; set; }
        public string? BadgeNote { get; set; }
        public Guid ReporterId { get; set; }
        public Profile? Reporter { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<TaskAssignee> Assignees { get; set; } = new();
        public List<ScopeEntity> Scope { get; set; } = new();
        public List<SubtaskEntity> Subtasks { get; set; } = new();
        public List<AttachmentEntity> Attachments { get; set; } = new();
        public List<CommentEntity> Comments { get; set; } = new();
    }

    public class TaskAssignee
    {
        public long TaskId { get; set; }
        public Guid UserId { get; set; }
        public Profile? User { get; set; }
        public string Role { get; set; } = "Primary";
    }

    public class ScopeEntity
    {
        public long Id { get; set; }
        public long TaskId { get; set; }
        public string Text { get; set; } = "";
        public bool Done { get; set; }
    }

    public class SubtaskEntity
    {
        public long Id { get; set; }
        public long TaskId { get; set; }
        public string Title { get; set; } = "";
        public bool Done { get; set; }
        public DateTime? DoneAt { get; set; }
    }

    public class AttachmentEntity
    {
        public long Id { get; set; }
        public long TaskId { get; set; }
        public string Name { get; set; } = "";
        public string Meta { get; set; } = "";
    }

    public class CommentEntity
    {
        public long Id { get; set; }
        public long TaskId { get; set; }
        public Guid AuthorId { get; set; }
        public Profile? Author { get; set; }
        public string Text { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
    {
        public DbSet<Profile> Profiles => Set<Profile>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
        public DbSet<TaskAssignee> TaskAssignees => Set<TaskAssignee>();
        public DbSet<ScopeEntity> ScopeItems => Set<ScopeEntity>();
        public DbSet<SubtaskEntity> Subtasks => Set<SubtaskEntity>();
        public DbSet<AttachmentEntity> Attachments => Set<AttachmentEntity>();
        public DbSet<CommentEntity> Comments => Set<CommentEntity>();
        public DbSet<ChannelEntity> Channels => Set<ChannelEntity>();
        public DbSet<ChannelMemberEntity> ChannelMembers => Set<ChannelMemberEntity>();
        public DbSet<MessageEntity> Messages => Set<MessageEntity>();
        public DbSet<MeetingEntity> Meetings => Set<MeetingEntity>();
        public DbSet<MeetingAttendanceEntity> MeetingAttendance => Set<MeetingAttendanceEntity>();
        public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<TaskAssignee>().HasKey(x => new { x.TaskId, x.UserId });
            b.Entity<ChannelMemberEntity>().HasKey(x => new { x.ChannelId, x.UserId });
            b.Entity<TaskEntity>(t =>
            {
                t.Property(x => x.Status).HasConversion<string>();
                t.Property(x => x.Priority).HasConversion<string>();
                t.Property(x => x.Due).HasColumnType("timestamp without time zone");
                t.HasMany(x => x.Assignees).WithOne().HasForeignKey(x => x.TaskId);
                t.HasMany(x => x.Scope).WithOne().HasForeignKey(x => x.TaskId);
                t.HasMany(x => x.Subtasks).WithOne().HasForeignKey(x => x.TaskId);
                t.HasMany(x => x.Attachments).WithOne().HasForeignKey(x => x.TaskId);
                t.HasMany(x => x.Comments).WithOne().HasForeignKey(x => x.TaskId);
            });
            b.Entity<MeetingAttendanceEntity>().HasKey(x => new { x.MeetingId, x.UserId });
            b.Entity<MeetingEntity>().HasOne(m => m.Creator).WithMany().HasForeignKey(m => m.CreatedBy);
        }
    }

    public class ChannelEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "channel";
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<ChannelMemberEntity> Members { get; set; } = new();
    }

    public class ChannelMemberEntity
    {
        public long ChannelId { get; set; }
        public ChannelEntity? Channel { get; set; }
        public Guid UserId { get; set; }
        public Profile? User { get; set; }
        public DateTime LastReadAt { get; set; } = DateTime.UtcNow;
    }

    public class MessageEntity
    {
        public long Id { get; set; }
        public long ChannelId { get; set; }
        public Guid SenderId { get; set; }
        public Profile? Sender { get; set; }
        public string Text { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class MeetingEntity
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public string JoinUrl { get; set; } = "";
        public Guid CreatedBy { get; set; }
        public Profile? Creator { get; set; }
    }

    public class MeetingAttendanceEntity
    {
        public Guid MeetingId { get; set; }
        public Guid UserId { get; set; }
        public Profile? User { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public class NotificationEntity
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }
        public string Kind { get; set; } = "";
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public string? Link { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}