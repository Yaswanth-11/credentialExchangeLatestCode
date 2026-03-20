namespace Credential.Models
{
    public class PresentationDefinitionRequest
    {
        public PresentationDefinition PresentationDefinition { get; set; }
    }

    public class ParsedPresentationDefinition
    {
        public string requestedDocument { get; set; }
        public Dictionary<string, List<string>> selectedClaims { get; set; }
    }
}
