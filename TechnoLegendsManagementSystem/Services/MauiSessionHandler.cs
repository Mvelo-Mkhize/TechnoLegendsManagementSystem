using System.Text.Json;
using Supabase.Gotrue;
using TechnoLegendsManagementSystem.Shared.Services;

namespace TechnoLegendsManagementSystem.Services;

public class MauiSessionHandler : ISessionPersistence
{
    private const string StorageKey = "supabase_session";

    public void SaveSession(Session session)
    {
        var json = JsonSerializer.Serialize(session);
        SecureStorage.Default.SetAsync(StorageKey, json).GetAwaiter().GetResult();
    }

    public Session? LoadSession()
    {
        try
        {
            var json = SecureStorage.Default.GetAsync(StorageKey).GetAwaiter().GetResult();
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<Session>(json);
        }
        catch
        {
            return null;
        }
    }

    public void DestroySession() => SecureStorage.Default.Remove(StorageKey);
}