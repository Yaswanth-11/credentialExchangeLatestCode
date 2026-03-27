using Credential.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Credential.Services.Interface
{
    public interface IVerifiableCredentialService
    {
        
        Task<string> GenerateRequestUriAsync(PresentationRequest request);
        // Task<object> FetchRequestObjectAsync(string transactionId);
        Task<AuthRequestObject> FetchRequestObjectAsync(string transactionId);
        Task<ParsedPresentationDefinition> ParsePresentationDefinitionAsync(PresentationDefinition presentationDefinition);
        Task<VerifiablePresentationResponse> GeneratePresentationSubmissionAsync(PresentationSubmissionRequest request);
        Task SubmitVpTokenAsync(
            PresentationSubmission presentationSubmission,
             object verifiablePresentation,
            string state,
            string transactionId, bool isRejected=false);

        Task<object> VerifyPresentationResponseAsync(string transactionId);

        Task<string> VerifyPresentationFromVpTokenAsync(string verifiablePresentation);

        Task<string> VerifyPresentationResponseAsync_with_id(string transactionId);

        public ServiceResult prepareRequestURI(string docType, Dictionary<string, List<string>> claims);

        public ServiceResult getPresentationDefinition(string transactionId);

        public ServiceResult parsePresentationDefinition(object requestData);

        public ServiceResult parseISO(object requestData);

        public ServiceResult postISO(string transactionId, object requestData);

        Task<ServiceResult> getISO(string transactionId);
    }

}
