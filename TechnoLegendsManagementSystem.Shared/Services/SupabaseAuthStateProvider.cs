using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Supabase.Gotrue;

namespace TechnoLegendsManagementSystem.Shared.Services;

public class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly Supabase.Client _supabase;
    private readonly ISessionPersistence _sessionPersistence;

    public SupabaseAuthStateProvider(
        Supabase.Client supabase,
        ISessionPersistence sessionPersistence)
    {
        _supabase = supabase;
        _sessionPersistence = sessionPersistence;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Populate the cache on the client before we look at it.
        // (No-op on MAUI, real JS load on WASM.)
        try { await _sessionPersistence.InitializeAsync(); } catch { }

        if (_supabase.Auth.CurrentSession == null)
        {
            var persisted = _sessionPersistence.LoadSession();
            if (persisted?.AccessToken is not null && persisted.RefreshToken is not null)
            {
                try { await _supabase.Auth.SetSession(persisted.AccessToken, persisted.RefreshToken); }
                catch { _sessionPersistence.DestroySession(); }
            }
        }

        var session = _supabase.Auth.CurrentSession;
        if (session?.User == null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var aal = GetAal(session);
        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, session.User.Id ?? string.Empty),
        new(ClaimTypes.Email,          session.User.Email ?? string.Empty),
        new(ClaimTypes.Name,           session.User.Email ?? string.Empty),
        new("aal", aal)
    };

        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "Supabase")));
    }

    public void NotifyAuthenticationChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static string GetAal(Session session)
    {
        try
        {
            return session.AccessToken?.Contains("\"aal\":\"aal2\"") == true ? "aal2" : "aal1";
        }
        catch
        {
            return "aal1";
        }
    }
}