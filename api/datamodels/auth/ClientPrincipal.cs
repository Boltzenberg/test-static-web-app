using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Boltzenberg.Functions.Storage;
using Microsoft.AspNetCore.Http;

namespace Boltzenberg.Functions.DataModels.Auth
{
    public class ClientPrincipal
    {
        [JsonPropertyName("identityProvider")]
        public string? IdentityProvider { get; set; }

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("userDetails")]
        public string? UserDetails { get; set; }

        [JsonPropertyName("userRoles")]
        public IEnumerable<string>? UserRoles { get; set; }

        private static async Task AddAuthLogLine(string line)
        {
            await JsonStore.Create<AuthLog>(new AuthLog(line));
        }

        public static async Task<ClientPrincipal?> FromReq(HttpRequest req)
        {
            // 1. Read the header 
            if (!req.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var headerValues))
            {
                await AddAuthLogLine("Failed to find X-MS-CLIENT-PRINCIPAL");
                return null;
            }

            var encoded = headerValues.First();
            if (encoded == null)
            {
                await AddAuthLogLine("No encoded values in the X-MS-CLIENT-PRINCIPAL header");
                return null;
            }

            // 2. Decode Base64 → JSON 
            var decodedBytes = Convert.FromBase64String(encoded);
            var json = Encoding.UTF8.GetString(decodedBytes);

            await AddAuthLogLine("Authenticated user: " + json);

            // 3. Deserialize
            return JsonSerializer.Deserialize<ClientPrincipal>(json);
        }

        private bool IsAuthorizedFor(HashSet<string> authorizedUsers)
        {
            if (this.UserDetails == null)
            {
                return false;
            }


            string user = this.UserDetails.ToLowerInvariant();
            return authorizedUsers.Contains(user);
        }

        private static HashSet<string> AddressBookAuthorizedUsers = new HashSet<string>()
        {
            "jon_rosenberg@hotmail.com",
            "treester@hotmail.com",
            "teresar@outlook.com"
        };

        public bool IsAuthorizedForAddressBook()
        {
            return IsAuthorizedFor(AddressBookAuthorizedUsers);
        }

        private static HashSet<string> SecretSantaAdminAuthorizedUsers = new HashSet<string>()
        {
            "jon_rosenberg@hotmail.com",
        };

        public bool IsAuthorizedForSecretSantaAdmin()
        {
            return IsAuthorizedFor(SecretSantaAdminAuthorizedUsers);
        }
    }
}