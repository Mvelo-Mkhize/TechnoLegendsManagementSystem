
namespace Supabase.Gotrue.Mfa
{
    internal class VerifyFactorParams : MfaVerifyParams
    {
        public string FactorId { get; set; }
        public object ChallengeId { get; set; }
        public string Code { get; set; }
    }
}