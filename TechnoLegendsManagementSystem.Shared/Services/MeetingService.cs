using System.Net;
using System.Net.Http.Json;
using TechnoLegendsManagementSystem.Shared.Models;

namespace TechnoLegendsManagementSystem.Shared.Services;

public interface IMeetingService
{
    Task<MeetingsOverviewDto> GetOverviewAsync();
    Task<List<AttendeeDto>> GetAttendeesAsync(Guid id);
    Task<JoinResultDto> JoinAsync(Guid id);
    Task<Guid> CreateAsync(CreateMeetingRequest req);
}

public class ApiMeetingService(IHttpClientFactory factory) : IMeetingService
{
    private HttpClient Http => factory.CreateClient("api");

    private async Task<HttpResponseMessage> Call(Func<HttpClient, Task<HttpResponseMessage>> send)
    {
        HttpResponseMessage res;
        try { res = await send(Http); }
        catch (HttpRequestException) { throw new ApiException("Can't reach the server. Check your connection and try again."); }
        catch (TaskCanceledException) { throw new ApiException("The server took too long to respond. Please try again."); }

        if (res.IsSuccessStatusCode) return res;

        var body = "";
        try { body = (await res.Content.ReadAsStringAsync()).Trim().Trim('"'); } catch { }

        throw new ApiException(res.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session expired. Please sign in again.",
            HttpStatusCode.NotFound => "That meeting could not be found.",
            HttpStatusCode.BadRequest when body.Length > 0 && !body.StartsWith('{') => body,
            HttpStatusCode.BadRequest => "That request wasn't valid.",
            _ => "Something went wrong on the server. Please try again."
        }, (int)res.StatusCode);
    }

    private static async Task<T> Read<T>(HttpResponseMessage res) =>
        await res.Content.ReadFromJsonAsync<T>() ?? throw new ApiException("The server sent an unexpected response.");

    public async Task<MeetingsOverviewDto> GetOverviewAsync() =>
        await Read<MeetingsOverviewDto>(await Call(h => h.GetAsync("api/meetings")));

    public async Task<List<AttendeeDto>> GetAttendeesAsync(Guid id) =>
        await Read<List<AttendeeDto>>(await Call(h => h.GetAsync($"api/meetings/{id}/attendees")));

    public async Task<JoinResultDto> JoinAsync(Guid id) =>
        await Read<JoinResultDto>(await Call(h => h.PostAsync($"api/meetings/{id}/join", null)));

    public async Task<Guid> CreateAsync(CreateMeetingRequest req) =>
        await Read<Guid>(await Call(h => h.PostAsJsonAsync("api/meetings", req)));
}