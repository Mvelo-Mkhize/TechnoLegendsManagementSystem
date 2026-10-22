using System;
using System.Collections.Generic;
using System.Text;

namespace TechnoLegendsManagementSystem.Shared.Models
{
    public enum WorkStatus { ToDo, InProgress, Done }
    public enum TaskPriority { High, Medium, Low }

    public record Person(string Name, string Title = "")
    {
        public string Initials => InitialsOf(Name);
        public static string InitialsOf(string name) =>
            string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpper(p[0])));
    }

    public record Assignee(Person Person, string Role);
    public record SubtaskItem(string Title, bool Done, string When);
    public record ScopeItem(string Text, bool Done);
    public record Attachment(string Name, string Meta);
    public record Comment(string Author, string Role, string When, string Text, bool IsMe = false);
    public record NewTask(string Title, string Project, List<string> Assignees, TaskPriority Priority,
                          DateTime Due, int Points, string Description, bool Draft);
    public record AssignLookups(List<string> Projects, List<Person> Members, List<string> Teams, DateTime SprintEnd);

    
    public static class CurrentUser
    {
        public static string Name { get; private set; } = "";
        public static string Department { get; private set; } = "";
        public static void Set(string name, string department) { Name = name; Department = department; }
        public static void Clear() { Name = ""; Department = ""; }
    }

    public class TaskItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public WorkStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public string Department { get; set; } = "";
        public string Project { get; set; } = "";
        public string Epic { get; set; } = "";
        public string Sprint { get; set; } = "";
        public DateTime Due { get; set; }
        public int Points { get; set; }
        public string? Badge { get; set; }
        public string? BadgeNote { get; set; }
        public Person Reporter { get; set; } = new("");
        public List<Assignee> Assignees { get; set; } = new();
        public List<ScopeItem> Scope { get; set; } = new();
        public List<SubtaskItem> Subtasks { get; set; } = new();
        public List<Attachment> Attachments { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();

        public int SubtasksDone => Subtasks.Count(s => s.Done);
        public int Progress => Subtasks.Count == 0 ? 0 : SubtasksDone * 100 / Subtasks.Count;
        public bool DueSoon => (Due.Date - DateTime.Today).Days <= 1;
        public string DueLabel
        {
            get
            {
                var days = (Due.Date - DateTime.Today).Days;
                var s = Due.ToString("MMM dd");
                return days == 0 ? $"{s} (Today)" : days == 1 ? $"{s} (Tomorrow)" : s;
            }
        }
    }
}
