namespace Credential.Models
{
    public class PresentationRequest
    {
        public string Type { get; set; }
        public string? clientId { get; set; }
        public string Scope { get; set; }
        public Dictionary<string, List<string>> SelectedClaims { get; set; }
    }

}
