using Microsoft.AspNetCore.Mvc;
using Credential.Models;
using System.Threading.Tasks;
using Credential.Services.Interface;
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
        public async Task<ServiceResult> GenerateRequestUri([FromBody] PresentationRequest request)
        {
            if (request == null || request.Type == null || request.Scope == null || request.SelectedClaims == null)
            {
                throw new LxException("Invalid request body. 'PresentationRequest' is required.", LxErrorCodes.E_UNSPECIFIED_ERROR);
            }

            var result = await _verifiableCredentialService.GenerateRequestUriAsync(request);
            _logger.LogInformation("Request URI generated successfully.");
            return new ServiceResult(true, "Request URI generated successfully.", 0, "", result);
        }

        [HttpGet("presentation/verify/result/{transactionId}")]
        public async Task<IActionResult> VerifyPresentationResponse(string transactionId)
        {
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
        public async Task<IActionResult> VerifyPresentationFromVpToken([FromBody] VerifyPresentationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.VerifiablePresentation))
            {
                throw new LxException("Invalid Verifiable Presentation provided.", LxErrorCodes.E_UNSPECIFIED_ERROR);
            }

            var result = await _verifiableCredentialService.VerifyPresentationFromVpTokenAsync(request.VerifiablePresentation);
            _logger.LogInformation("Presentation response verified successfully.");
            return Ok(result);
        }

        [HttpGet("presentation/verify/result_with_vptoken/{transactionId}")]
        public async Task<IActionResult> VerifyPresentationResponse_with_vptoken(string transactionId)
        {
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

