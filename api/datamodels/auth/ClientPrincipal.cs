namespace Boltzenberg.Functions.DataModels.Auth
{
    public class ClientPrincipal
    {
        public required string IdentityProvider { get; set; }
        public required string UserId { get; set; }
        public required string UserDetails { get; set; }
        public required IEnumerable<string> UserRoles { get; set; }
    }
}