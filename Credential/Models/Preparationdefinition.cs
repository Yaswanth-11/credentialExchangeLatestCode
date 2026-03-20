using System;
using System.Collections.Generic;

namespace Credential.Models
{
    // Model for PresentationDefinition
    public class Presentationdefinition
    {
        public string DocType { get; set; }

        public string IssuerCertificateChain { get; set; }

        public List<string> NameSpaces { get; set; }

        public DocTypeName docTypeName { get; set; }
    }

    public class DocTypeName
    {
        public Dictionary<string, List<AttributeList>> Namespaces { get; set; }
    }

    public class AttributeList
    {
        public string Description { get; set; }

        public string Attribute { get; set; }
    }

    // Model for QREngagementData
    public class QREngagementData
    {
        public string Data { get; set; }  // The QR engagement data as a string
    }
}
