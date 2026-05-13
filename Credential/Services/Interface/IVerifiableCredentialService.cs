using Credential.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Credential.Services.Interface
{
    public interface IVerifiableCredentialService
    {
        
        Task<string> GenerateRequestUriAsync(PresentationRequest request, string accessToken);
        
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

        Task<ServiceResult> getPresentationDefinition(string transactionId);

        Task<ServiceResult> parsePresentationDefinition(object requestData);

        Task<ServiceResult> parseISO(object requestData);

        Task<ServiceResult> postISO(string transactionId, object requestData);

        Task<ServiceResult> getISO(string transactionId);

        public int verifychecksum(byte[] data, string checksum, int isJSON);
    }

}
