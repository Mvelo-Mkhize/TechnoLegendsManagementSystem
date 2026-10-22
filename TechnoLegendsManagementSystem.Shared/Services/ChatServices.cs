using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using TechnoLegendsManagementSystem.Shared.Models;

namespace TechnoLegendsManagementSystem.Shared.Services
{
    public class ChatException(string message) : Exception(message);

    public interface IChatService
    {
        Task<PersonDto> GetMeAsync();
        Task<List<ChannelDto>> GetChannelsAsync();
        Task<List<MessageDto>> GetMessagesAsync(long channelId, long? before = null);
        Task<MessageDto> SendAsync(long channelId, string text);
        Task MarkReadAsync(long channelId);
        Task<List<PersonDto>> GetPeopleAsync();
        Task<long> StartDmAsync(Guid userId);
        Task<long> CreateChannelAsync(string name);
    }

    public class ApiChatService(IHttpClientFactory factory) : IChatService
    {
        private HttpClient Http => factory.CreateClient("api");

        private async Task<HttpResponseMessage> Call(Func<HttpClient, Task<HttpResponseMessage>> request)
        {
            HttpResponseMessage res;
            try { res = await request(Http); }
            catch (HttpRequestException) { throw new ChatException("Can't reach the server. Check your connection and try again."); }
            catch (TaskCanceledException) { throw new ChatException("The server took too long to respond. Please try again."); }

            if (res.IsSuccessStatusCode) return res;

            var body = "";
            try { body = (await res.Content.ReadAsStringAsync()).Trim().Trim('"'); } catch { }

            throw new ChatException(res.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
                HttpStatusCode.Forbidden => "You don't have permission to do that.",
                HttpStatusCode.NotFound => "That chat or person could not be found.",
                HttpStatusCode.BadRequest when body.Length > 0 && !body.StartsWith('{') => body,
                HttpStatusCode.BadRequest => "That request wasn't valid.",
                _ => "Something went wrong on the server. Please try again."
            });
        }

        private static async Task<T> Read<T>(HttpResponseMessage res) =>
            await res.Content.ReadFromJsonAsync<T>() ?? throw new ChatException("The server sent an unexpected response.");

        public async Task<PersonDto> GetMeAsync() =>
            await Read<PersonDto>(await Call(h => h.GetAsync("api/channels/me")));

        public async Task<List<ChannelDto>> GetChannelsAsync() =>
            await Read<List<ChannelDto>>(await Call(h => h.GetAsync("api/channels")));

        public async Task<List<MessageDto>> GetMessagesAsync(long channelId, long? before = null) =>
            await Read<List<MessageDto>>(await Call(h => h.GetAsync(
                $"api/channels/{channelId}/messages?limit=50" + (before is null ? "" : $"&before={before}"))));

        public async Task<MessageDto> SendAsync(long channelId, string text) =>
            await Read<MessageDto>(await Call(h => h.PostAsJsonAsync($"api/channels/{channelId}/messages", new SendMessageRequest(text))));

        public async Task MarkReadAsync(long channelId) =>
            await Call(h => h.PostAsync($"api/channels/{channelId}/read", null));

        public async Task<List<PersonDto>> GetPeopleAsync() =>
            await Read<List<PersonDto>>(await Call(h => h.GetAsync("api/channels/people")));

        public async Task<long> StartDmAsync(Guid userId) =>
            await Read<long>(await Call(h => h.PostAsJsonAsync("api/channels/dm", new CreateDmRequest(userId))));

        public async Task<long> CreateChannelAsync(string name) =>
            await Read<long>(await Call(h => h.PostAsJsonAsync("api/channels", new CreateChannelRequest(name))));
    }

    public class ChatClient(IAuthService auth, IHttpClientFactory factory) : IAsyncDisposable
    {
        private HubConnection? _conn;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public event Action<MessageDto>? MessageReceived;
        public event Action? StateChanged;
        public string State { get; private set; } = "Disconnected";

        private void Set(string s) { State = s; StateChanged?.Invoke(); }

        public async Task EnsureStartedAsync()
        {
            await _gate.WaitAsync();
            try
            {
                if (_conn is null)
                {
                    var baseUrl = factory.CreateClient("api").BaseAddress
                        ?? throw new InvalidOperationException("API base address is not configured.");

                    _conn = new HubConnectionBuilder()
                        .WithUrl(new Uri(baseUrl, "hubs/chat"), o => o.AccessTokenProvider = () => auth.GetAccessTokenAsync())
                        .WithAutomaticReconnect()
                        .Build();

                    _conn.On<MessageDto>("MessageReceived", m => MessageReceived?.Invoke(m));
                    _conn.Reconnecting += _ => { Set("Reconnecting"); return Task.CompletedTask; };
                    _conn.Reconnected += _ => { Set("Connected"); return Task.CompletedTask; };
                    _conn.Closed += _ => { Set("Disconnected"); return Task.CompletedTask; };
                }

                if (_conn.State == HubConnectionState.Disconnected)
                {
                    Set("Connecting");
                    try { await _conn.StartAsync(); Set("Connected"); }
                    catch { Set("Disconnected"); throw; }
                }
            }
            finally { _gate.Release(); }
        }

        // call this on sign-out so the next user doesn't reuse the old connection
        public async Task StopAsync()
        {
            if (_conn is not null) await _conn.StopAsync();
            Set("Disconnected");
        }

        public async ValueTask DisposeAsync()
        {
            if (_conn is not null) await _conn.DisposeAsync();
        }
    }

    public static class ChatUi
    {
        private static readonly string[] Palette = { "#3b5bdb", "#0ca678", "#e8590c", "#7048e8", "#495057", "#d6336c", "#1c7ed6" };

        public static string Initials(string name)
        {
            var p = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return p.Length == 0 ? "?" : p.Length == 1 ? p[0][..1].ToUpper() : (p[0][..1] + p[^1][..1]).ToUpper();
        }

        public static string Color(string name) => Palette[name.Sum(c => (int)c) % Palette.Length];

        public static string TimeLabel(DateTime utc)
        {
            var l = utc.ToLocalTime(); var today = DateTime.Now.Date;
            return l.Date == today ? l.ToString("h:mm tt") : l.Date == today.AddDays(-1) ? "Yesterday" : l.ToString("MMM d");
        }

        public static string DayLabel(DateTime day)
        {
            var today = DateTime.Now.Date;
            return day == today ? "Today" : day == today.AddDays(-1) ? "Yesterday" : day.ToString("dddd, MMMM d");
        }
    }
}

