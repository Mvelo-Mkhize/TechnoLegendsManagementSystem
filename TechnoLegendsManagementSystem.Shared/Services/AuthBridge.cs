namespace TechnoLegendsManagementSystem.Shared.Services;

public class AuthBridge(SupabaseAuthService auth, ISessionPersistence persistence) : IAuthService
{
    public async Task<string?> GetAccessTokenAsync()
    {
        // restore the saved session if the app just launched
        if (auth.CurrentSession is null)
        {
            var saved = persistence.LoadSession();
            if (saved?.AccessToken is not null && saved.RefreshToken is not null)
            {
                try { await auth.SetSessionAsync(saved.AccessToken, saved.RefreshToken); }
                catch { persistence.DestroySession(); }
            }
        }
        return auth.CurrentSession?.AccessToken;
    }

    public Task SignOutAsync() => auth.SignOutAsync();
}