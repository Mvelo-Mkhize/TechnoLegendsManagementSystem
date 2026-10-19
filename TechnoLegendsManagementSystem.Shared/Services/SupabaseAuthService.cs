using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Mfa; // Required for MfaEnrollParams, MfaChallengeParams, etc.

namespace TechnoLegendsManagementSystem.Shared.Services;

public class SupabaseAuthService
{
    private readonly Supabase.Client _supabase;

    public SupabaseAuthService(Supabase.Client supabase) => _supabase = supabase;

    public Session? CurrentSession => _supabase.Auth.CurrentSession;

    public async Task<Session?> SignInAsync(string email, string password)
    {
        try
        {
            return await _supabase.Auth.SignIn(email, password);
        }
        catch (GotrueException ex)
        {
            throw new ApplicationException($"Sign-in failed: {ex.Message}", ex);
        }
    }

    public async Task<List<Factor>?> ListFactorsAsync()
    {
        var response = await _supabase.Auth.ListFactors();
        return response?.Totp;
    }

    public async Task<object> EnrollTotpAsync(string friendlyName = "TechnoLegends Authenticator")
    {
        // FactorType is a string property, not an enum. Use the literal "totp".
        var response = await _supabase.Auth.Enroll(new MfaEnrollParams
        {
            FactorType = "totp",
            FriendlyName = friendlyName
        });
        return response!;
    }

    public async Task<Session> ChallengeAndVerifyMfaAsync(string factorId, string code)
    {
        var challenge = await _supabase.Auth.Challenge(new MfaChallengeParams
        {
            FactorId = factorId
        });

        return await _supabase.Auth.Verify(new MfaVerifyParams
        {
            FactorId = factorId,
            ChallengeId = challenge.Id,
            Code = code
        });
    }

    public Task SignOutAsync() => _supabase.Auth.SignOut();

    public Task SetSessionAsync(string accessToken, string refreshToken)
        => _supabase.Auth.SetSession(accessToken, refreshToken);
}