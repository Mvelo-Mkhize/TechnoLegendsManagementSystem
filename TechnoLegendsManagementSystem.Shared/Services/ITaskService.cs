using System;
using System.Collections.Generic;
using System.Text;
using TechnoLegendsManagementSystem.Shared.Models;

namespace TechnoLegendsManagementSystem.Shared.Services
{
    public interface ITaskService
    {
        Task<List<TaskItem>> GetTasksAsync();
        Task<TaskItem?> GetTaskAsync(string id);
        Task<TaskItem> CreateTaskAsync(NewTask task);
        Task AddCommentAsync(string id, string text);
        Task<AssignLookups> GetLookupsAsync();
    }
}
