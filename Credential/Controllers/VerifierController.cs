using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Credential.Models;
using System.Threading.Tasks;
using Credential.Services.Interface;
using Microsoft.AspNetCore.Cors;
using Lux.Infrastructure;
using System.Text.Json;
using System.Text.RegularExpressions;

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
    [Route("api/verifier")]
    [ApiController]
    public class VerifierController : ControllerBase
    {
        private readonly IVerifiableCredentialService _verifiableCredentialService;
        private readonly ILogger<VerifierController> _logger;

        public VerifierController(IVerifiableCredentialService verifiableCredentialService, ILogger<VerifierController> logger)
        {
            _verifiableCredentialService = verifiableCredentialService;
            _logger = logger;
        }

        [HttpPost("presentation/request/uri")]
        public async Task<IActionResult> GenerateRequestUri([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement request)
        {
            if (request.ValueKind != JsonValueKind.Object)
            {
                return Ok(new ServiceResult(false, "Invalid request body. 'PresentationRequest' is required.", 400, "Invalid request body", null));
            }

            PresentationRequest? model;
            try
            {
                model = JsonSerializer.Deserialize<PresentationRequest>(
                    request.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception)
            {
                return Ok(new ServiceResult(false, "Invalid request body. 'PresentationRequest' is required.", 400, "Invalid request body", null));
            }

            if (model == null || model.Type == null || model.Scope == null || model.SelectedClaims == null)
            {
                return Ok(new ServiceResult(false, "Invalid request body. 'PresentationRequest' is required.", 400, "Invalid request body", null));
            }

            var result = await _verifiableCredentialService.GenerateRequestUriAsync(model);
            _logger.LogInformation("Request URI generated successfully.");
            return Ok(new ServiceResult(true, "Request URI generated successfully.", 0, "", result));
        }

        [HttpGet("presentation/verify/result/{transactionId}")]
        public async Task<IActionResult> VerifyPresentationResponse(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId) || !Regex.IsMatch(transactionId, "^[A-Za-z0-9_-]+$"))
            {
                return NotFound(new { success = false, message = "Transaction data not found." });
            }

            if (transactionId == null)
            {
                throw new LxException("'transactionId' is required.", LxErrorCodes.E_UNSPECIFIED_ERROR);
            }

            object result;
            try
            {
                result = await _verifiableCredentialService.VerifyPresentationResponseAsync(transactionId);
            }
            catch (Exception)
            {
                return NotFound(new { success = false, message = "Transaction data not found." });
            }
            _logger.LogInformation("Presentation response verified successfully.");
            return Ok(result);
        }

        [HttpPost("presentation/verify/result")]
        public async Task<IActionResult> VerifyPresentationFromVpToken([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement request)
        {
            if (request.ValueKind != JsonValueKind.Object)
            {
                return Ok(new ServiceResult(false, "Invalid Verifiable Presentation provided.", 400, "Invalid request body", null));
            }

            VerifyPresentationRequest? model;
            try
            {
                model = JsonSerializer.Deserialize<VerifyPresentationRequest>(
                    request.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception)
            {
                return Ok(new ServiceResult(false, "Invalid Verifiable Presentation provided.", 400, "Invalid request body", null));
            }

            if (model == null || string.IsNullOrWhiteSpace(model.VerifiablePresentation))
            {
                return Ok(new ServiceResult(false, "Invalid Verifiable Presentation provided.", 400, "Invalid request body", null));
            }

            var result = await _verifiableCredentialService.VerifyPresentationFromVpTokenAsync(model.VerifiablePresentation);
            _logger.LogInformation("Presentation response verified successfully.");
            return Ok(result);
        }

        [HttpGet("presentation/verify/result_with_vptoken/{transactionId}")]
        public async Task<IActionResult> VerifyPresentationResponse_with_vptoken(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId) || !Regex.IsMatch(transactionId, "^[A-Za-z0-9_-]+$"))
            {
                return NotFound(new { success = false, message = "Transaction data not found." });
            }

            if (!ModelState.IsValid)
            {
                throw new LxException("Invalid request body", LxErrorCodes.E_UNSPECIFIED_ERROR);
            }

            var result = await _verifiableCredentialService.VerifyPresentationResponseAsync_with_id(transactionId);
            _logger.LogInformation("Presentation response verified successfully.");
            return Ok(result);
        }
    }
}

