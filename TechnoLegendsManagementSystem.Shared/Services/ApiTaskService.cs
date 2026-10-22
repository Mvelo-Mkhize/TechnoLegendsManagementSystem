using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TechnoLegendsManagementSystem.Shared.Models;



namespace TechnoLegendsManagementSystem.Shared.Services
{
    public class ApiTaskService(IHttpClientFactory factory) : ITaskService
    {
        private HttpClient Http => factory.CreateClient("api");

        // Every API call goes through here, so errors are handled in one place.
        private async Task<HttpResponseMessage> Call(Func<HttpClient, Task<HttpResponseMessage>> send, bool notFoundOk = false)
        {
            HttpResponseMessage res;
            try
            {
                res = await send(Http);
            }
            catch (TaskCanceledException ex)
            {
                throw new ApiException("The server took too long to respond. Try again.", null, ex);
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("Can't reach the server. Check your connection and that the API is running.", null, ex);
            }

            if (res.IsSuccessStatusCode || (notFoundOk && res.StatusCode == HttpStatusCode.NotFound))
                return res;

            throw new ApiException(await ErrorText(res), (int)res.StatusCode);
        }

        private static async Task<string> ErrorText(HttpResponseMessage res)
        {
            if (res.StatusCode == HttpStatusCode.Unauthorized)
                return "Your session expired or isn't verified. Please sign in again.";
            if (res.StatusCode == HttpStatusCode.Forbidden)
                return "You don't have access to that.";

            try
            {
                using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                if (doc.RootElement.TryGetProperty("detail", out var d) && d.GetString() is { Length: > 0 } text)
                    return text;
            }
            catch { /* not JSON, fall through */ }

            return $"Server error ({(int)res.StatusCode}).";
        }

        private static async Task<T> Read<T>(HttpResponseMessage res) where T : new()
            => await res.Content.ReadFromJsonAsync<T>() ?? new T();

        public async Task<List<TaskItem>> GetTasksAsync()
            => await Read<List<TaskItem>>(await Call(h => h.GetAsync("api/tasks")));

        public async Task<TaskItem?> GetTaskAsync(string id)
        {
            var res = await Call(h => h.GetAsync($"api/tasks/{id}"), notFoundOk: true);
            return res.StatusCode == HttpStatusCode.NotFound ? null : await res.Content.ReadFromJsonAsync<TaskItem>();
        }

        public async Task<TaskItem> CreateTaskAsync(NewTask task)
        {
            var res = await Call(h => h.PostAsJsonAsync("api/tasks", task));
            return (await res.Content.ReadFromJsonAsync<TaskItem>())!;
        }

        public async Task AddCommentAsync(string id, string text)
            => await Call(h => h.PostAsJsonAsync($"api/tasks/{id}/comments", new { text }));

        public async Task<AssignLookups> GetLookupsAsync()
        {
            var res = await Call(h => h.GetAsync("api/lookups"));
            return (await res.Content.ReadFromJsonAsync<AssignLookups>())!;
        }
    }
}
