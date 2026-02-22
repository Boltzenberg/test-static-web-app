using Boltzenberg.Functions.Storage;
using Microsoft.AspNetCore.Http;

namespace Boltzenberg.Functions.DataModels.Auth
{
    public static class AuthZChecker
    {
        public static async Task<bool> IsAuthorizedForRefreshToken(HttpRequest req)
        {
            if (await RefreshableToken.Check(req))
            {
                return true;
            }

            return false;
        }

        public static bool IsAuthorizedForSignIn(HttpRequest req)
        {
            if (TOTP.CheckForJonsPhone(req))
            {
                return true;
            }

            return false;
        }

        public static async Task<bool> IsAuthorizedForAddressBook(HttpRequest req)
        {
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal != null && principal.IsAuthorizedForAddressBook())
            {
                return true;
            }

            if (TOTP.CheckForJonsPhone(req))
            {
                return true;
            }

            if (await RefreshableToken.Check(req))
            {
                return true;
            }

            return false;
        }

        public static async Task<bool> IsAuthorizedForSecretSantaAdmin(HttpRequest req)
        {
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal != null && principal.IsAuthorizedForSecretSantaAdmin())
            {
                return true;
            }

            if (TOTP.CheckForJonsPhone(req))
            {
                return true;
            }

            if (await RefreshableToken.Check(req))
            {
                return true;
            }

            return false;
        }

        private static async Task AddAuthLogLine(string line)
        {
            await JsonStore.Create<AuthLog>(new AuthLog(line));
        }
    }
}