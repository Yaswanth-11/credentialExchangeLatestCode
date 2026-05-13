namespace Credential.Models
{
    public class TokenIntrospectionOptions
    {
        public bool Enabled { get; set; }

        public bool? EnableWeb { get; set; }
        public bool? EnableMobile { get; set; }

        public bool? WebRequireForGet { get; set; }
        public bool? WebRequireForNonGet { get; set; }
        public bool? MobileRequireForGet { get; set; }
        public bool? MobileRequireForNonGet { get; set; }

        public string[]? SkipPaths { get; set; }

        public string IntrospectionUrl { get; set; }

        public string BasicAuth { get; set; }

        public int TimeoutSeconds { get; set; } = 10;
    }
}
