using Microsoft.AspNetCore.Http;
using OtpNet;

namespace Boltzenberg.Functions.DataModels.Auth
{
    public static class TOTP
    {
        private static string? JON_PHONE_TOTP_SECRET = Environment.GetEnvironmentVariable("JON_PHONE_TOTP_SECRET");

        public static bool CheckForJonsPhone(HttpRequest req)
        {
            if (!req.Headers.TryGetValue("X-JON-TOTPCODE", out var headerValues))
            {
                return false;
            }

            var totpCode = headerValues.First();
            if (totpCode == null)
            {
                return false;
            }

            // Retrieve the stored secret for this device
            if (JON_PHONE_TOTP_SECRET == null)
            {
                return false;
            }

            // Decode the Base32 secret and create a TOTP instance
            byte[] secretBytes = Base32Encoding.ToBytes(JON_PHONE_TOTP_SECRET);
            var totp = new Totp(secretBytes, step: 30, mode: OtpHashMode.Sha1, totpSize: 6);

            // Verify the code — VerificationWindow allows ±1 time step to handle clock skew
            bool isValid = totp.VerifyTotp(
                totpCode,
                out long timeStepMatched,
                window: new VerificationWindow(previous: 1, future: 1)
            );

            return isValid;
        }
    }
}