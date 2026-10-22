using System.Net;
using System.Net.Http.Json;
using TechnoLegendsManagementSystem.Shared.Models;

namespace TechnoLegendsManagementSystem.Shared.Services;

public interface INotificationService
{
    int UnreadCount { get; }
    event Action? Changed;
    Task<List<NotificationDto>> GetAsync();
    Task RefreshCountAsync();
    Task MarkReadAsync(long id);
    Task MarkAllReadAsync();
}

public class ApiNotificationService(IHttpClientFactory factory) : INotificationService
{
    public int UnreadCount { get; private set; }
    public event Action? Changed;

    private HttpClient Http => factory.CreateClient("api");
    private void Set(int n) { UnreadCount = n; Changed?.Invoke(); }

    private async Task<HttpResponseMessage> Call(Func<HttpClient, Task<HttpResponseMessage>> send)
    {
        HttpResponseMessage res;
        try { res = await send(Http); }
        catch (HttpRequestException) { throw new ApiException("Can't reach the server. Check your connection and try again."); }
        catch (TaskCanceledException) { throw new ApiException("The server took too long to respond. Please try again."); }

        if (res.IsSuccessStatusCode) return res;

        throw new ApiException(res.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session expired. Please sign in again.",
            HttpStatusCode.NotFound => "That notification no longer exists.",
            _ => "Something went wrong on the server. Please try again."
        }, (int)res.StatusCode);
    }

    public async Task<List<NotificationDto>> GetAsync()
    {
        var res = await Call(h => h.GetAsync("api/notifications"));
        var body = await res.Content.ReadFromJsonAsync<NotificationsResponse>()
                   ?? throw new ApiException("The server sent an unexpected response.");
        Set(body.Unread);
        return body.Items;
    }

    public async Task RefreshCountAsync()
    {
        var res = await Call(h => h.GetAsync("api/notifications/unread-count"));
        Set(await res.Content.ReadFromJsonAsync<int>());
    }

    public async Task MarkReadAsync(long id)
    {
        await Call(h => h.PostAsync($"api/notifications/{id}/read", null));
        Set(Math.Max(0, UnreadCount - 1));
    }

    public async Task MarkAllReadAsync()
    {
        await Call(h => h.PostAsync("api/notifications/read-all", null));
        Set(0);
    }
}