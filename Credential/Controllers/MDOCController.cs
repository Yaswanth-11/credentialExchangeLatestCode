using Credential.Models;
using Credential.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Lux.Infrastructure;
using Microsoft.AspNetCore.Cors;

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
        public IActionResult prepareRequestURI([FromBody] DocumentRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.DocumentType) || request.Claims == null || request.Claims.Count == 0)
            {
                return BadRequest(new ServiceResult(false, "Invalid request body", 400, "Invalid request body", null));
            }

            var serviceResult = _verifiableCredentialService.prepareRequestURI(request.DocumentType, request.Claims);
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
        public ServiceResult postISO(string transactionId, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object requestData)
        {
            _logger.LogInformation("Saving MDOC request for transactionId: {TransactionId}", transactionId);

            if (string.IsNullOrEmpty(transactionId))
            {
                throw new LxException("TransactionId is required.", LxErrorCodes.E_UNSPECIFIED_ERROR);
            }

            if (requestData == null)
            {
                return new ServiceResult(false, "Request body is missing.", 400, "Invalid request body", null);
            }

            try
            {
                return _verifiableCredentialService.postISO(transactionId, requestData);
            }
            catch (LxException ex)
            {
                return new ServiceResult(false, ex.Message, 400, "Invalid request body", null);
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