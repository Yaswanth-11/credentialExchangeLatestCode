using Credential.Models;
using Credential.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Lux.Infrastructure;
using Microsoft.AspNetCore.Cors;
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
    [Route("api/mdoc")]
    [ApiController]
    public class MDOCController : ControllerBase
    {
        private readonly IVerifiableCredentialService _verifiableCredentialService;
        private readonly ILogger<MDOCController> _logger;

        public MDOCController(IVerifiableCredentialService verifiableCredentialService, ILogger<MDOCController> logger)
        {
            _verifiableCredentialService = verifiableCredentialService;
            _logger = logger;
        }

        [HttpPost("prepareRequestURI")]
        public IActionResult prepareRequestURI([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement request)
        {
            if (request.ValueKind != JsonValueKind.Object)
            {
                return Ok(new ServiceResult(false, "Invalid request body", 400, "Invalid request body", null));
            }

            string? documentType = null;
            if (request.TryGetProperty("DocumentType", out var documentTypeElement) && documentTypeElement.ValueKind == JsonValueKind.String)
            {
                documentType = documentTypeElement.GetString();
            }

            Dictionary<string, List<string>>? claims = null;
            if (request.TryGetProperty("Claims", out var claimsElement) && claimsElement.ValueKind == JsonValueKind.Object)
            {
                claims = new Dictionary<string, List<string>>();
                foreach (var claimProperty in claimsElement.EnumerateObject())
                {
                    if (claimProperty.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    var values = new List<string>();
                    foreach (var item in claimProperty.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var value = item.GetString();
                            if (!string.IsNullOrEmpty(value))
                            {
                                values.Add(value);
                            }
                        }
                    }

                    claims[claimProperty.Name] = values;
                }
            }

            if (string.IsNullOrEmpty(documentType) || claims == null || claims.Count == 0)
            {
                return Ok(new ServiceResult(false, "Invalid request body", 400, "Invalid request body", null));
            }

            var serviceResult = _verifiableCredentialService.prepareRequestURI(documentType, claims);
            _logger.LogInformation("Successfully generated Presentation Definition.");
            return Ok(serviceResult);
        }

        [HttpGet("getPresentationDefinition/{transactionId}")]
        public IActionResult getPresentationDefinition(string transactionId)
        {
            _logger.LogInformation("Fetching PresentationDefinition for transactionId: {TransactionId}", transactionId);

            if (string.IsNullOrEmpty(transactionId))
            {
                throw new LxException("Transaction ID is required.", LxErrorCodes.E_UNSPECIFIED_ERROR);
            }

            try
            {
                return Ok(_verifiableCredentialService.getPresentationDefinition(transactionId));
            }
            catch (LxException)
            {
                return NotFound(new ServiceResult(false, "Transaction ID not found or expired.", 404, "Not Found", null));
            }
        }

        [HttpPost("parsePresentationDefinition")]
        public ServiceResult parsePresentationDefinition([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object requestData)
        {
            _logger.LogInformation("Processing PresentationDefinition and QR Engagement Data.");

            if (requestData == null)
            {
                return new ServiceResult(false, "Request body is required.", 400, "Invalid request body", null);
            }

            try
            {
                return _verifiableCredentialService.parsePresentationDefinition(requestData);
            }
            catch (LxException ex)
            {
                return new ServiceResult(false, ex.Message, 400, "Invalid request body", null);
            }
        }

        [HttpPost("parseISO")]
        public ServiceResult parseISO([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object RequestData)
        {
            _logger.LogInformation("Processing PresentationDefinition and QR Engagement Data.");

            if (RequestData == null)
            {
                return new ServiceResult(false, "Request body is required.", 400, "Invalid request body", null);
            }

            try
            {
                return _verifiableCredentialService.parseISO(RequestData);
            }
            catch (LxException ex)
            {
                return new ServiceResult(false, ex.Message, 400, "Invalid request body", null);
            }
        }

        [HttpPost("postISO/{transactionId}")]
        public IActionResult postISO(string transactionId, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object requestData)
        {
            _logger.LogInformation("Saving MDOC request for transactionId: {TransactionId}", transactionId);

            if (string.IsNullOrEmpty(transactionId))
            {
                return NotFound(new ServiceResult(false, "TransactionId is required.", 404, "Not Found", null));
            }

            if (requestData == null)
            {
                return Ok(new ServiceResult(false, "Request body is missing.", 400, "Invalid request body", null));
            }

            try
            {
                return Ok(_verifiableCredentialService.postISO(transactionId, requestData));
            }
            catch (LxException ex)
            {
                return NotFound(new ServiceResult(false, ex.Message, 404, "Not Found", null));
            }
        }

        [HttpGet]
        [Route("getISO/{transactionId}")]
        public async Task<IActionResult> getISO(string transactionId)
        {
            try
            {
                return Ok(await _verifiableCredentialService.getISO(transactionId));
            }
            catch (LxException)
            {
                return NotFound(new ServiceResult(false, "Data Not Yet Posted.", 404, "Not Found", null));
            }
        }
    }
}