using System.Text.Json;
using Microsoft.JSInterop;
using Supabase.Gotrue;
using TechnoLegendsManagementSystem.Shared.Services;

namespace TechnoLegendsManagementSystem.Web.Services;

public class WebSessionHandler : ISessionPersistence
{
    private readonly IJSRuntime _js;
    private const string StorageKey = "supabase_session";

    public WebSessionHandler(IJSRuntime js) => _js = js;

    public void SaveSession(Session session)
    {
        var json = JsonSerializer.Serialize(session);
        _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public Session? LoadSession()
    {
        try
        {
            var json = _js.InvokeAsync<string?>("localStorage.getItem", StorageKey)
                .AsTask().GetAwaiter().GetResult();
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<Session>(json);
        }
        catch
        {
            return null;
        }
    }

    public void DestroySession()
    {
        _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }
}