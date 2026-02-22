using Boltzenberg.Functions.Storage;
using Microsoft.AspNetCore.Http;

namespace Boltzenberg.Functions.DataModels.Auth
{
    public class RefreshableToken : CosmosDocument
    {
        public static string RefreshableTokenAppId = "RefreshableTokenApp";
        public static string RefreshableTokenId = "ReplaceWithDeviceId";
        public static string RefreshableTokenHeaderName = "X-JON-REFRESHABLETOKEN";

        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; } = default!;

        public RefreshableToken()
            : base(RefreshableTokenAppId)
        {
            this.id = RefreshableTokenId;
        }

        public static RefreshableToken Generate()
        {
            RefreshableToken token = new RefreshableToken();
            token.Token = GenerateRefreshToken();
            token.Expiration = DateTime.UtcNow.AddHours(2);
            return token;
        }

        public static async Task<bool> Check(HttpRequest req)
        {
            if (!req.Headers.TryGetValue(RefreshableTokenHeaderName, out var headerValues))
            {
                return false;
            }

            var incomingToken = headerValues.First();
            if (incomingToken == null)
            {
                return false;
            }

            OperationResult<RefreshableToken> result = await JsonStore.Read<RefreshableToken>(RefreshableTokenAppId, RefreshableTokenId);
            if (result.Code != ResultCode.Success || result.Entity == null)
            {
                return false;
            }

            if (result.Entity.Expiration < DateTime.UtcNow)
            {
                return false;
            }

            if (result.Entity.Token != incomingToken)
            {
                return false;
            }

            return true;
        }

        private static string GenerateRefreshToken()
        {
            // Refresh token is just a secure random string — validation is done via DB lookup
            var bytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
