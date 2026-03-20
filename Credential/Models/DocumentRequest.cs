using System.Collections.Generic;

namespace Credential.Models
{
    public class DocumentRequest
    {
        public string DocumentType { get; set; }
        public Dictionary<string, List<string>> Claims { get; set; }
    }
    public class Fields
    {
        public string path { get; set; }
        public string filter { get; set; }
    }
}
    