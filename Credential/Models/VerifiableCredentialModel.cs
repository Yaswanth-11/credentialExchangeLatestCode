using Newtonsoft.Json;
using StackExchange.Redis;


namespace Credential.Models
{
    public class PresentationSubmission
    {


        public string id { get; set; }
        public string definition_id { get; set; }
        public List<DescriptorMap> descriptor_map { get; set; }
    }

    public class DescriptorMap
    {
        public string id { get; set; }
        public string format { get; set; }
        public string path { get; set; }
        public PathNested path_nested { get; set; }
    }

    public class PathNested
    {
        public string format { get; set; }
        public string path { get; set; }
    }
      public class CredentialSubject
    {
        public string id { get; set; }
        public string type { get; set; }
        public Document Document { get; set; }
    }

    public class Document
    {
        public string type { get; set; }
        public string name { get; set; }
    }

    public class CredentialStatus
    {
        public string id { get; set; }
        public string type { get; set; }
        public string revocationListCredential { get; set; }
        public string revocationListIndex { get; set; }
    }

    public class Proof
    {
        public string type { get; set; }
        public string created { get; set; }
        public string verificationMethod { get; set; }
        public string cryptosuite { get; set; }
        public string proofPurpose { get; set; }
        public string?challenge { get; set; }
        public string proofValue { get; set; }
         // This is specific to the response for presentation submission
       

    }
    
    public class VerifiablePresentation
    {
        [JsonProperty("@context")]
       
        public List<string> @context { get; set; }
        public List<string> type { get; set; }
        public List<VerifiableCredentialModel> verifiableCredential { get; set; }
        public string id { get; set; }
        public Proof proof { get; set; }
    }
    public class VerifiableCredentialModel
    {
        [JsonProperty("@context")]
       
        public List<string>@context { get; set; }
        public List<string> type { get; set; }

        public string issuanceDate { get; set; }
        public string issuer { get; set; }

        public CredentialStatus credentialStatus { get; set; }
        public CredentialSubject credentialSubject { get; set; }
        public Proof proof { get; set; }
        // public string ExpirationDate { get; set; }
        //public string Name { get; set; }
        //public string Description { get; set; }


    }
    public class VerifiablePresentationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public Result Result { get; set; }
    }


    public class Result
    {
        public PresentationSubmission presentationSubmission { get; set; }
        public object verifiablePresentation { get; set; }

    }
    public class VPTokenSubmissionRequest
    {
        public PresentationSubmission PresentationSubmission { get; set; }
        public object VerifiablePresentation { get; set; }
        public string State { get; set; }
        
        public bool IsRejected { get; set; }

    }
    public class VerificationResult
    {
        public string VerifyResult { get; set; }
        public dynamic AttributesList { get; set; }
    }
}