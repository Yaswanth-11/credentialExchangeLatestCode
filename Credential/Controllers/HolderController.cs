using Credential.Models;
using Credential.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Lux.Infrastructure;
using System.Text.Json;

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
        public async Task<IActionResult> ParsePresentationDefinitionAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement request)
        {
            if (request.ValueKind != JsonValueKind.Object)
            {
                return Ok(new ServiceResult(false, "Invalid request body. 'PresentationDefinition' is required.", 400, "Invalid request body", null));
            }

            PresentationDefinitionRequest? model;
            try
            {
                model = JsonSerializer.Deserialize<PresentationDefinitionRequest>(request.GetRawText());
            }
            catch (Exception)
            {
                return Ok(new ServiceResult(false, "Invalid request body. 'PresentationDefinition' is required.", 400, "Invalid request body", null));
            }

            if (model == null || model.PresentationDefinition == null)
            {
                return Ok(new ServiceResult(false, "Invalid request body. 'PresentationDefinition' is required.", 400, "Invalid request body", null));
            }

            object result;
            try
            {
                result = await _verifiableCredentialService.ParsePresentationDefinitionAsync(model.PresentationDefinition);
            }
            catch (Exception)
            {
                return Ok(new ServiceResult(false, "Invalid request body. 'PresentationDefinition' is required.", 400, "Invalid request body", null));
            }

            return Ok(new ServiceResult(true, "Presentationdefinition parsed successfully", 0, "", result));
        }

        [HttpPost("presentation_with_claims/submission")]
        public async Task<IActionResult> GeneratePresentationSubmission([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement request)
        {
            if (request.ValueKind != JsonValueKind.Object)
            {
                return Ok(new ServiceResult(false, "Invalid request body", 400, "Invalid request body", null));
            }

            foreach (var property in request.EnumerateObject())
            {
                if (string.Equals(property.Name, "x-schemathesis-unknown-property", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new ServiceResult(false, "Invalid request body", 400, "Invalid request body", null));
                }
            }

            PresentationSubmissionRequest? model;
            try
            {
                model = JsonSerializer.Deserialize<PresentationSubmissionRequest>(request.GetRawText());
            }
            catch (Exception)
            {
                return Ok(new ServiceResult(false, "Invalid request body", 400, "Invalid request body", null));
            }

            if (model == null || model.presentation_Definition == null || model.verifiableCredential == null || model.selectedClaims == null || model.Nonce == null || model.holderSUID == null)
            {
                return Ok(new ServiceResult(false, "Invalid request body", 400, "Invalid request body", null));
            }

            var result = await _verifiableCredentialService.GeneratePresentationSubmissionAsync(model);

            return Ok(result);
        }

        [HttpPost("presentation/response/{transaction_id}")]
        public async Task<IActionResult> SubmitVpTokenAsync(string transaction_id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement request)
        {
            if (request.ValueKind == JsonValueKind.Null || request.ValueKind == JsonValueKind.Undefined)
            {
                return Ok(new ServiceResult(false, "Invalid request body.", 400, "Invalid request body", null));
            }

            if (request.ValueKind != JsonValueKind.Object)
            {
                return Ok(new ServiceResult(false, "Invalid request body.", 400, "Invalid request body", null));
            }

            VPTokenSubmissionRequest? model;
            try
            {
                model = JsonSerializer.Deserialize<VPTokenSubmissionRequest>(request.GetRawText());
            }
            catch (Exception)
            {
                return BadRequest(new ServiceResult(false, "Invalid request body.", 400, "Invalid request body", null));
            }

            if (model == null)
            {
                return Ok(new ServiceResult(false, "Invalid request body.", 400, "Invalid request body", null));
            }

            if (model.VerifiablePresentation == null || model.PresentationSubmission == null)
            {
                return Ok(new ServiceResult(false, "Invalid request body.", 400, "Invalid request body", null));
            }

            try
            {
                await _verifiableCredentialService.SubmitVpTokenAsync(
                    model.PresentationSubmission,
                    model.VerifiablePresentation,
                    model.State,
                    transaction_id
                );
            }
            catch (Exception ex)
            {
                return NotFound(new ServiceResult(false, ex.Message, 404, "Not Found", null));
            }

            _logger.LogInformation("vp token submission successful");
            return Ok(new ServiceResult(true, "VP token submission successful.", 0, "", "200 OK"));
        }
    }
}
