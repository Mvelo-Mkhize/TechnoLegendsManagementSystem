using System.Text.Json;
using Microsoft.JSInterop;
using Supabase.Gotrue;
using TechnoLegendsManagementSystem.Shared.Services;

namespace TechnoLegendsManagementSystem.Web.Services;

public class WebSessionHandler : ISessionPersistence
{
    private readonly IJSRuntime _js;
    private const string StorageKey = "supabase_session";

    private Session? _cached;
    private bool _initialized;

    public WebSessionHandler(IJSRuntime js) => _js = js;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            _cached = string.IsNullOrEmpty(json)
                ? null
                : JsonSerializer.Deserialize<Session>(json);
        }
        catch
        {
            _cached = null;
        }
        finally
        {
            _initialized = true;
        }
    }

    public void SaveSession(Session session)
    {
        _cached = session;
        _ = PersistAsync(JsonSerializer.Serialize(session));
    }

    public Session? LoadSession() => _cached;

    public void DestroySession()
    {
        _cached = null;
        _ = RemoveAsync();
    }

    private async Task PersistAsync(string json)
    {
        try { await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json); }
        catch {       }
    }

    private async Task RemoveAsync()
    {
        try { await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey); }
        catch {    }
    }
}