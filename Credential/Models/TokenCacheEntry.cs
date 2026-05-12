namespace Credential.Models
{
    public class TokenCacheEntry
    {
        public bool Active { get; set; }

        public string? ClientId { get; set; }

        public string? Username { get; set; }

        public string? Scope { get; set; }

        public string? OrgId { get; set; }
    }
}
