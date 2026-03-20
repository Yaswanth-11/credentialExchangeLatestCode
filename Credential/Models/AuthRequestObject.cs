using System.Collections.Generic;

namespace Credential.Models
{
    public class AuthRequestObject
    {
        public string client_id { get; set; }
        public string scope { get; set; }
        public string response_uri { get; set; }
        public string response_type { get; set; }
        public string response_mode { get; set; }
        public string nonce { get; set; }
        public string state { get; set; }
        public PresentationDefinition presentation_definition { get; set; }
    }
   

    public class PresentationDefinition
    {   
        public string id { get; set; }
        public List<InputDescriptor> input_descriptors { get; set; }
    }

    public class InputDescriptor
    {
        public string id { get; set; }
        public Format format { get; set; }
        public Constraints constraints { get; set; }
    }

    public class Format
    {
        public LdpVc ldp_vc { get; set; }
    }

    public class LdpVc
    {
        public List<string> proof_type { get; set; }
    }

    public class Constraints
    {
        public string limit_disclosure { get; set; }
        public List<Field> fields { get; set; }
    }

    public class Field
    {
        public List<string> path { get; set; }
        public Filter? filter { get; set; }  // New filter property
    }

    public class Filter
    {
        public string type { get; set; }
        public string pattern { get; set; }
    }
    public class PresentationSubmissionRequest
    {
        public PresentationDefinition presentation_Definition { get; set; } // Rename this property
        public string verifiableCredential { get; set; }
        public Dictionary<string, List<string>> selectedClaims { get; set; }
        public string Nonce { get; set; }
        public string holderSUID { get; set; }
    }
}
