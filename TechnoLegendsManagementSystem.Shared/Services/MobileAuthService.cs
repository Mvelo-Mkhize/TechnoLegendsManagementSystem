using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Mfa;

namespace TechnoLegendsManagementSystem.Shared.Services;

public class MobileAuthService
{
    private readonly Supabase.Client _supabase;

    public MobileAuthService(Supabase.Client supabase) => _supabase = supabase;

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

    public async Task<(string FactorId, string QrCode, string Secret)> EnrollTotpAsync()
    {
        var r = await _supabase.Auth.Enroll(new MfaEnrollParams
        {
            FactorType = "totp",
            FriendlyName = "TLMS-" + Guid.NewGuid().ToString("N")[..8]
        });
        return (r!.Id!, r.Totp!.QrCode!, r.Totp.Secret!);
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