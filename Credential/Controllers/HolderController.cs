using Credential.Models;
using Credential.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Lux.Infrastructure;

namespace Credential.Controllers
{

      [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [EnableCors("AllowAll")]
    [ApiController]
    [Route("api/vc/holder")]
    public class HolderController : ControllerBase
    {
        private readonly IVerifiableCredentialService _verifiableCredentialService;
        private readonly ILogger<HolderController> _logger;
        public HolderController(IVerifiableCredentialService verifiableCredentialService, ILogger<HolderController> logger)
        {
            _verifiableCredentialService = verifiableCredentialService;
            _logger = logger;
        }

        // Get Request Object by Transaction ID
        [HttpGet("presentation/requestObject/{transaction_id}")]
        public async Task<ServiceResult> GetRequestObjectAsync(string transaction_id)
        {
            if (transaction_id == null)
            {
                throw new LxException("transaction_id should not be null", LxErrorCodes.E_UNSPECIFIED_ERROR);
            }

            object requestObject;
            try
            {
                requestObject = await _verifiableCredentialService.FetchRequestObjectAsync(transaction_id);
            }
            catch (Exception)
            {
                return new ServiceResult(false, "Request object not found for transaction ID", 404, "Not Found", transaction_id);
            }

            if (requestObject == null)
            {
                return new ServiceResult(false, "Request object not found for transaction ID", 0, "", transaction_id);
            }

            return new ServiceResult(true, "Request object fetched successfully.", 0, "", requestObject);
        }

        [HttpPost("presentation/definition/claims")]
        public async Task<ServiceResult> ParsePresentationDefinitionAsync([FromBody] PresentationDefinitionRequest request)
        {
            if (request == null || request.PresentationDefinition == null)
            {
                throw new LxException("Invalid request body. 'PresentationDefinition' is required.", LxErrorCodes.E_UNSPECIFIED_ERROR);
            }

            object result;
            try
            {
                result = await _verifiableCredentialService.ParsePresentationDefinitionAsync(request.PresentationDefinition);
            }
            catch (Exception)
            {
                throw new LxException("Invalid request body. 'PresentationDefinition' is required.", LxErrorCodes.E_UNSPECIFIED_ERROR);
            }

            return new ServiceResult(true, "Presentationdefinition parsed successfully", 0, "", result);
        }

        [HttpPost("presentation_with_claims/submission")]
        public async Task<IActionResult> GeneratePresentationSubmission([FromBody] PresentationSubmissionRequest request)
        {
            if (request == null || request.presentation_Definition == null || request.verifiableCredential == null || request.selectedClaims == null || request.Nonce == null || request.holderSUID == null)
            {
                throw new LxException("Invalid request body", LxErrorCodes.E_UNSPECIFIED_ERROR);
            }

            var result = await _verifiableCredentialService.GeneratePresentationSubmissionAsync(request);

            return Ok(result);
        }

        [HttpPost("presentation/response/{transaction_id}")]
        public async Task<ServiceResult> SubmitVpTokenAsync(string transaction_id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] VPTokenSubmissionRequest request)
        {
            if (request == null || request.VerifiablePresentation == null || request.PresentationSubmission == null)
            {
                return new ServiceResult(false, "Invalid request body.", 400, "Invalid request body", null);
            }

            await _verifiableCredentialService.SubmitVpTokenAsync(
                request.PresentationSubmission,
                request.VerifiablePresentation,
                request.State,
                transaction_id
            );

            _logger.LogInformation("vp token submission successful");
            return new ServiceResult(true, "VP token submission successful.", 0, "", "200 OK");
        }
    }
}
