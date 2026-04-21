namespace Credential.Models
{
    public class PresentationRequestModified
    {
        public string Type { get; set; }
        public string? clientId { get; set; }
        public string Scope { get; set; }
    }

    public class PresentationRequest
    {
        public string Type { get; set; }
        public string? clientId { get; set; }
        public string Scope { get; set; }
        public Dictionary<string, List<string>>? SelectedClaims { get; set; }
    }

    public class ApiResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public object result { get; set; }
    }

}
