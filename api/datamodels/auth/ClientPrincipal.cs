using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Boltzenberg.Functions.DataModels.Auth
{
    public class ClientPrincipal
    {
        public string? IdentityProvider { get; set; }
        public string? UserId { get; set; }
        public string? UserDetails { get; set; }
        public IEnumerable<string>? UserRoles { get; set; }

        public static ClientPrincipal? FromReq(HttpRequest req)
        {
            // 1. Read the header 
            if (!req.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var headerValues))
            {
                return null;
            }

            var encoded = headerValues.First();
            if (encoded == null)
            {
                return null;
            }

            // 2. Decode Base64 → JSON 
            var decodedBytes = Convert.FromBase64String(encoded);
            var json = Encoding.UTF8.GetString(decodedBytes);

            // 3. Deserialize
            return JsonSerializer.Deserialize<ClientPrincipal>(json);
        }
    }
}