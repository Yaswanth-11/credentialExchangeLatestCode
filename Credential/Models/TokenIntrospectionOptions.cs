using System.Collections.Generic;

namespace Credential.Models
{
    public class TokenIntrospectionOptions
    {
        public bool Enabled { get; set; }

        public string IntrospectionUrl { get; set; }

        public string BasicAuth { get; set; }

        public int TimeoutSeconds { get; set; } = 10;
    }
}
