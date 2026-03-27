using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Credential.Models;
using Credential.Models.Exceptions;
using Credential.RedisDB;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Credential.Services.Interface;
using Newtonsoft.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json.Linq;
using System.Linq;
using Credential.Services.Utilities;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using System.Net.Http;
using Lux.Infrastructure;
using Newtonsoft.Json.Serialization;
using System.Transactions;

namespace Credential.Services
{
    public class VerifiableCredentialService : IVerifiableCredentialService
    {
        private readonly IConfiguration _configuration;
        private readonly IDatabase _redisDb;
        private readonly IRedisTransactionStore _redisTransactionStore;
        private readonly string _nodeJsApiUrl;
        private readonly string _pvtUrl;
        private readonly string apiBaseUrl;
        private readonly HttpClient _httpClient;
        private readonly TimeSpan _transactionDataTtl;


        public ILogger<VerifiableCredentialService> _logger { get; }

        public VerifiableCredentialService(
            IConfiguration configuration,
            IConnectionMultiplexer redisConnection,
            IRedisTransactionStore redisTransactionStore,
            HttpClient httpClient,
            ILogger<VerifiableCredentialService> logger)
        {
            _configuration = configuration;
            _redisDb = redisConnection.GetDatabase();
            _redisTransactionStore = redisTransactionStore;
            _pvtUrl = _configuration["PvtSettings:PVTURL"];
            apiBaseUrl= _configuration["ApiSettings:OrgDetailsBaseUrl"];
            _httpClient = httpClient;
            var ttlMinutes = _configuration.GetValue<int?>("RedisSettings:TransactionDataTtlMinutes").GetValueOrDefault(5);
            _transactionDataTtl = TimeSpan.FromMinutes(ttlMinutes > 0 ? ttlMinutes : 5);
            _logger = logger;

        }

      
        public async Task<string> GenerateRequestUriAsync(PresentationRequest request)
        {
            try
            {
                var presentationDefinition = await GeneratePresentationDefinitionAsync(request);

                if (presentationDefinition == null)
                {
                    _logger.LogError("Failed to generate presentation definition.");
                    throw new Exception("Failed to generate presentation definition.");
                }

                var requestUri = await PrepareRequestUriAsync(presentationDefinition, request);

                return requestUri;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error generating request URI {0} ", ex.Message);
                throw new Exception("Error generating request URI", ex);
            }
        }

        private async Task<object> GeneratePresentationDefinitionAsync(PresentationRequest request)
        {
            try
            {
                var id = Guid.NewGuid().ToString();
                var inputDescriptors = new List<object>();

                // Construct input descriptor for the specified type
                var inputDescriptor = new
                {
                    id = $"ID card for {request.Type}",
                    format = new
                    {
                        ldp_vc = new
                        {
                            proof_type = new[] { "DataIntegrityProof" }
                        }
                    },
                    constraints = new
                    {
                        limit_disclosure = "required",
                        fields = new List<object>()
                    }
                };

                // Add the filter field for "type" only once
                if (!string.IsNullOrEmpty(request.Type))
                {
                    inputDescriptor.constraints.fields.Add(new
                    {
                        path = new[] { "$.type" },
                        filter = new
                        {
                            type = "string",
                            pattern = request.Type
                        }
                    });
                }

                // Iterate through selected claims and add fields to the input descriptor
                foreach (var claimCategory in request.SelectedClaims)
                {
                    foreach (var claimField in claimCategory.Value)
                    {
                        var fieldDescriptor = new
                        {
                            path = new[] { $"$.credentialSubject.{claimCategory.Key}.{claimField}" }
                        };

                        // Add the field descriptor to the input descriptor's fields array
                        inputDescriptor.constraints.fields.Add(fieldDescriptor);
                    }
                }

                // Add the input descriptor to the list
                inputDescriptors.Add(inputDescriptor);

                // Construct the presentation definition object
                var presentationDefinition = new
                {
                    id = id,
                    input_descriptors = inputDescriptors
                };
                _logger.LogInformation("presentation definition generated successfully");
                return presentationDefinition;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during presentation definition generation {0}", ex.Message);
                throw new Exception("Error during presentation definition generation", ex);
            }
        }


        private async Task<string> PrepareRequestUriAsync(object presentationDefinition, PresentationRequest request)
        {
            try
            {
                var nonce = Guid.NewGuid().ToString("N");
                var transactionId = Guid.NewGuid().ToString("N");
                var requestId = Guid.NewGuid().ToString("N");

                // var orgDetailsUrl = $"{apiBaseUrl}/MDOCProvisioning/getOrgDetails/{request.clientId}";
                // _logger.LogError($"Failed to fetch org details: {orgDetailsUrl}");

                //string? orgName = null;

                // if (!string.IsNullOrEmpty(request.clientId))
                // {
                //     var response = await _httpClient.GetAsync(orgDetailsUrl);
                //     if (response.IsSuccessStatusCode)
                //     {
                //         var responseData = await response.Content.ReadAsStringAsync();
                //         var orgDetails = Newtonsoft.Json.JsonConvert.DeserializeObject<OrgDetailsResponse>(responseData);
                //         orgName = orgDetails?.Result?.orgName;
                //     }
                //     else
                //     {
                //         _logger.LogError($"Failed to fetch org details: {response.StatusCode}");
                //         throw new Exception("Failed to fetch organization details");
                //     }
                // }

                // If request.clientId is null, client_id should be null; otherwise, use orgName if available.
                //string? clientId = string.IsNullOrEmpty(request.clientId) ? "" : orgName;

                var responseUri = $"{_pvtUrl}/api/vc/holder/presentation/response/{transactionId}";
                //_logger.LogError($"Failed to fetch org details: {responseUri}");

                var authRequestObj = new
                {

                    client_id = request.clientId,
                    scope = request.Scope,
                    response_uri = responseUri,
                    response_type = "vp_token",
                    response_mode = "direct_post",
                    nonce,
                    state = requestId,
                    presentation_definition = presentationDefinition
                };

                //_logger.LogError($"Failed to fetch org details: {authRequestObj}");

                var jsonAuthRequestObj = Newtonsoft.Json.JsonConvert.SerializeObject(authRequestObj);

                await _redisTransactionStore.StoreStringAsync(
                    transactionId,
                    transactionId,
                    jsonAuthRequestObj,
                    "presentation-auth-request");

                // Log serialized data
                _logger.LogInformation("preparing request URI was successful");

                return $"{_pvtUrl}/api/vc/holder/presentation/requestObject/{transactionId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing request URI");
                throw new Exception("Error preparing request URI", ex);
            }
        }
        public async Task<AuthRequestObject> FetchRequestObjectAsync(string transactionId)
        {
            try
            {
                var requestObject = await _redisTransactionStore.GetRequiredObjectAsync<AuthRequestObject>(
                    transactionId,
                    transactionId,
                    "presentation-auth-request");

                _logger.LogInformation("fetchrequestobject was successful");
                return requestObject;

            }
            catch (TransactionStateException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching request object for transaction ID {0}", ex.Message);
                throw new Exception($"Error fetching request object for transaction ID: {transactionId}", ex);
            }
        }
        public async Task<ParsedPresentationDefinition> ParsePresentationDefinitionAsync(PresentationDefinition presentationDefinition)
        {
            try
            {
                if (presentationDefinition == null)
                {
                    throw new ArgumentException("PresentationDefinition is required.");
                }

                //Console.WriteLine("Initiating parsing of Presentation Definition.");

                var extractedClaims = new Dictionary<string, List<string>>();
                string requestedDocument = null;

                foreach (var inputDescriptor in presentationDefinition.input_descriptors)
                {
                    if (inputDescriptor.constraints?.fields != null)
                    {
                        foreach (var field in inputDescriptor.constraints.fields)
                        {
                            if (field.path != null && field.path[0] != "$.type" && field.path[0].StartsWith("$.credentialSubject"))
                            {
                                // Extract the path after $.credentialSubject
                                var relativePath = field.path[0].Substring("$.credentialSubject.".Length);
                                var parts = relativePath.Split('.'); // Split the path into parts

                                if (parts.Length >= 2) // Ensure there are at least two parts
                                {
                                    var rootObject = string.Join(".", parts.SkipLast(1));  // First part is the root object
                                    var claimName = parts[^1]; // Last part is the claim name

                                    if (!extractedClaims.ContainsKey(rootObject))
                                    {
                                        extractedClaims[rootObject] = new List<string>();
                                    }
                                    extractedClaims[rootObject].Add(claimName);
                                }
                            }

                            if (field.path != null && field.path[0] == "$.type")
                            {
                                requestedDocument = field.filter?.pattern;
                            }
                        }

                    }
                }

                if (requestedDocument == null || extractedClaims.Count == 0)
                {
                    _logger.LogError("Failed to parse required fields from the Presentation Definition.");
                    throw new Exception("Failed to parse required fields from the Presentation Definition.");
                }

                _logger.LogInformation("Successfully parsed Presentation Definition.");
                return new ParsedPresentationDefinition
                {
                    requestedDocument = requestedDocument,
                    selectedClaims = extractedClaims
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to parse Presentation Definition {0}", ex.Message);
                throw new Exception("Failed to parse Presentation Definition.", ex); // Optional: You can rethrow or log further.
            }
        }


        public async Task<VerifiablePresentationResponse> GeneratePresentationSubmissionAsync(PresentationSubmissionRequest request)
        {
            try
            {
                // Deserialize the PresentationDefinition from the request
                var presentationDefinition = request.presentation_Definition;

                // Generate the Presentation Submission
                var presentationSubmission = await GeneratePsHandler(presentationDefinition);

                // Call Node.js API for Verifiable Presentation
                var verifiablePresentationResponse = await CallNodeJsGenerateVpHandler(request);
                _logger.LogInformation("Request body is submitted to holdervp api of nodejs");
                // Debug: Log the raw response for clarity

                // Deserialize the raw response into a dynamic object to examine its structure
                ServiceResult deserializedDynamicResponse = JsonConvert.DeserializeObject<ServiceResult>(verifiablePresentationResponse.ToString());

                if (!deserializedDynamicResponse.Success)
                {
                    _logger.LogError("Error returned from node js service call");
                    throw new Exception("Error generating Presentation Submission");
                }

                // Deserialize into the VerifiablePresentation model
                var SerializeverifiablePresentation = JsonConvert.SerializeObject(deserializedDynamicResponse.Result);


                return new VerifiablePresentationResponse
                {
                    Success = true,
                    Message = "Creating Presentation submission successful",
                    ErrorCode = 0,
                    ErrorMessage = "",
                    Result = new Result
                    {
                        presentationSubmission = presentationSubmission,
                        verifiablePresentation = SerializeverifiablePresentation
                    }
                };
            }
            catch (Exception ex)
            {
                // Improved error handling
                _logger.LogError("Error generating Presentation Submission {0}", ex.Message);
                throw new Exception("Error generating Presentation Submission", ex);
            }
        }


        private async Task<PresentationSubmission> GeneratePsHandler(PresentationDefinition presentationDefinition)
        {
            // Implement C# logic for generating Presentation Submission (PS)
            var presentationSubmission = new PresentationSubmission
            {
                id = Guid.NewGuid().ToString(),
                definition_id = presentationDefinition.id,
                descriptor_map = new List<DescriptorMap>()
            };

            foreach (var descriptor in presentationDefinition.input_descriptors)
            {
                presentationSubmission.descriptor_map.Add(new DescriptorMap
                {
                    id = descriptor.id,
                    format = "ldp_vp",
                    path = "$",
                    path_nested = new PathNested
                    {
                        format = "ldp_vc",
                        path = "$.verifiableCredential[0]"
                    }
                });
            }

            return presentationSubmission;
        }


        private async Task<object> CallNodeJsGenerateVpHandler(PresentationSubmissionRequest request)
        {
            using (var httpClient = new HttpClient())
            {
                // Construct the payload
                var payload = new
                {
                    presentationDefinition = request.presentation_Definition,
                    VerifiableCredential = request.verifiableCredential,
                    SelectedClaims = request.selectedClaims,
                    nonce = request.Nonce,
                    HolderSUID = request.holderSUID
                };

                // Serialize the payload to JSON
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                try
                {
                    var apiurl = _configuration["ApiSettings:HolderVpUrl"];
                   
                   
                    // Log the payload being sent

                    // Send the POST request
                    var response = await httpClient.PostAsync(apiurl, content);
                   
                    _logger.LogInformation("received response from holdervp api of nodejs");
                    // Log the HTTP response metadata

                    // Read and log the response content
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("holdervp api of nodejs failed");
                        throw new Exception($"Failed to call Node.js API. Status: {response.StatusCode}, Response: {responseContent}");
                    }

                    // Deserialize and return the response object
                    return responseContent;
                }
                catch (Exception ex)
                {
                    // Log any exceptions that occur
                    //throw;
                    _logger.LogError("Error in  callnodejs holdervp api {0}", ex.Message);
                    throw new Exception("Error in  callnodejs holdervp api", ex);
                }
            }
        }

        public async Task SubmitVpTokenAsync(PresentationSubmission presentationSubmission,object verifiablePresentation,
           string state,string transactionId, bool isRejected=false)
        {

            

            if(isRejected)
            {

                _logger.LogInformation("Presentation submission is rejected by the holder for transactionId: {0}", transactionId);
                
                var presentationSubmissionRejectedData = new
            {
                isRejected = isRejected,
                presentation_submission = "null",
                 vp_token = "null" // Ensure vp_token is stored as a JSON object
            };

            // Serialize the complete data to JSON and store in Redis
                 var presentationSubmissionRejectedDataJson = JsonConvert.SerializeObject(presentationSubmissionRejectedData);

                    await _redisTransactionStore.StoreStringAsync(
                        transactionId,
                        transactionId,
                        presentationSubmissionRejectedDataJson,
                        "presentation-submission");

                    _logger.LogInformation("rejected presentation submission are stored in redis.");

                return; // Exit the method after handling rejection
            }

            // Parameter validation
            if (presentationSubmission == null)
                throw new Exception("presentation_submission parameter is missing");

            if (verifiablePresentation == null)
 
                throw new Exception("vp_token parameter is missing");

            if (string.IsNullOrEmpty(state))
                throw new Exception("state parameter is missing");

            if (string.IsNullOrEmpty(transactionId))
                throw new Exception("transactionId parameter is missing");

            var requestObjectJson = await _redisTransactionStore.GetRequiredStringAsync(
                transactionId,
                transactionId,
                "presentation-auth-request");

            // Deserialize the request object
            dynamic requestObject;
            try
            {
                requestObject = JsonConvert.DeserializeObject<dynamic>(requestObjectJson)
                    ?? throw TransactionStateException.DeserializationFailed(transactionId, transactionId);
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                throw TransactionStateException.DeserializationFailed(transactionId, transactionId, ex);
            }

            // Extract the presentation definition and state from the request object
            var presentationDefinition = requestObject?.presentation_definition;
            var requestState = requestObject?.state;

            // Validate the state from the request object
            if (requestState != state)
                throw new Exception("State does not match with the corresponding transaction.");

            // Validate the presentation definition ID
            if (presentationDefinition?.id != presentationSubmission.definition_id)
                throw new Exception("Presentation submission does not match the corresponding presentation definition.");

            // Validate the descriptor map ID
            if (presentationDefinition?.input_descriptors[0]?.id != presentationSubmission.descriptor_map[0]?.id)
                throw new Exception("Presentation submission does not match the corresponding presentation definition descriptor.");

            // Properly handle vp_token serialization
            string vpTokenJson;
 
            if (verifiablePresentation is string presentationString)
            {
                // If verifiablePresentation is already a valid JSON string, use it directly
                vpTokenJson = presentationString;
            }
            else if (verifiablePresentation is JsonElement jsonElement)
            {
                // If verifiablePresentation is a JsonElement, serialize it properly
                vpTokenJson = jsonElement.GetRawText();
            }
            else
            {
                // If verifiablePresentation is an object, serialize it to a JSON string
                vpTokenJson = JsonConvert.SerializeObject(verifiablePresentation);
            }

            dynamic vpTokenObject;

            // If vpTokenJson is a string, parse it into a dynamic JSON object
            if (!string.IsNullOrEmpty(vpTokenJson))
            {
                vpTokenObject = JsonConvert.DeserializeObject<dynamic>(vpTokenJson);
            }
            else
            {
                throw new Exception("vp_token cannot be null or empty");
            }
            
            // Prepare the complete data for submission
            var completeData = new
            {
                presentation_submission = presentationSubmission,
                 vp_token = vpTokenObject // Ensure vp_token is stored as a JSON object
            };

            // Serialize the complete data to JSON and store in Redis
            var completeDataJson = JsonConvert.SerializeObject(completeData);

            await _redisTransactionStore.StoreStringAsync(
                transactionId,
                transactionId,
                completeDataJson,
                "presentation-submission");

            _logger.LogInformation("presentation submission,vptoken are stored in redis.");
 
        }
        
        private static bool IsSuccessResponse(object response)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse((string)response);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("Success", out JsonElement successProperty) && successProperty.GetBoolean())
                {
                    return true;
                }
            }
           catch (Newtonsoft.Json.JsonException ex)
            {
                throw new Exception("invalid json format ",ex);
            }

           return false;
        }

        public async Task<object> VerifyPresentationResponseAsync(string transactionId)
        {
            try
            {
                var redisValue = await _redisTransactionStore.GetRequiredStringAsync(
                    transactionId,
                    transactionId,
                    "presentation-response");

                dynamic presentationResponse;
                try
                {
                    presentationResponse = JsonConvert.DeserializeObject<dynamic>(redisValue)
                        ?? throw TransactionStateException.DeserializationFailed(transactionId, transactionId);
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize presentation response for transactionId: {TransactionId}", transactionId);
                    throw TransactionStateException.DeserializationFailed(transactionId, transactionId, ex);
                }

                object verifyResultResponse = "Data not yet posted";
                JToken attributesList = null;

                if(presentationResponse?.isRejected == true)
                {
                    _logger.LogInformation("Presentation submission is rejected by the holder for transactionId: {0}", transactionId);
                    
                    verifyResultResponse = "Presentation submission is rejected by the holder.";
                     return new ServiceResult(false, "Presentation submission is rejected by the holder.", 0, "", null);

                }
                else if (presentationResponse?.presentation_submission == null &&
                    presentationResponse?.response_type == "vp_token" &&
                    presentationResponse?.response_mode == "direct_post")
                {
                    verifyResultResponse = "Data not yet posted";
                     return new ServiceResult(false, "Data not yet posted", 0, "", null);
                }
                else if (presentationResponse?.presentation_submission != null &&
                         presentationResponse?.vp_token != null)
                {
                    // Deserialize vp_token
                    var vpToken = JsonConvert.DeserializeObject<JObject>(presentationResponse.vp_token.ToString());

                    // Verify the presentation
                    verifyResultResponse = await VerifyVerifiablePresentationAsync(vpToken);
                    _logger.LogInformation("response from vptokenm url of nodejs is processing...");
                    if (IsSuccessResponse(verifyResultResponse))
                    {
                        await _redisTransactionStore.DeleteIfExistsAsync(
                            transactionId,
                            transactionId,
                            "presentation-response");
                    }
                    return verifyResultResponse;
                }
                else
                {
                    _logger.LogError("Invalid presentation response format.");
                    return new ServiceResult(false, "Invalid presentation response format.", 0, "", null);
                }
            }
            catch (TransactionStateException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying presentation response");
                throw new Exception("Error verifying presentation response", ex);
            }
        }

        private async Task<string> VerifyVerifiablePresentationAsync(dynamic vpToken)
        {
            try
            {
                var url = _configuration["ApiSettings:VerifyVpTokenUrl"];
                
               
                // Serialize the vpToken to JSON and then encode it as Base64
                string vpTokenJson = JsonConvert.SerializeObject(vpToken);
                string vpTokenBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(vpTokenJson));

                // Create the request body with the Base64-encoded vpToken
                var content = new StringContent(JsonConvert.SerializeObject(new { verifiablePresentation = vpTokenBase64 }), System.Text.Encoding.UTF8, "application/json");

                // Make the HTTP POST request
                var response = await _httpClient.PostAsync(url, content);
                _logger.LogInformation("Request body is submitted to vptokenurl of nodejs");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Verification of presentation response done");
                    return await response.Content.ReadAsStringAsync(); // Return the response as a string
                }
                else
                {
                    _logger.LogError("Verification failed");
                    // Handle failed response (e.g., log it or throw an exception)
                    return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Request failed{0}", ex.Message);
                // Handle network or other HttpRequestException errors
                return $"Request failed: {ex.Message}";
            }
        }

        public async Task<string> VerifyPresentationFromVpTokenAsync(string verifiablePresentation)
        {
            var apiUrl = _configuration["ApiSettings:Verifyurlwithoutid"];
            

            try
            {
                // Prepare the request object
                var requestPayload = new
                {
                    verifiablePresentation = verifiablePresentation
                };

                // Serialize it to JSON
                var jsonBody = System.Text.Json.JsonSerializer.Serialize(requestPayload);

                // Wrap it in StringContent
                var requestContent = new StringContent(
                    jsonBody,
                    Encoding.UTF8,
                    "application/json"
                );

                // Send POST request
                var response = await _httpClient.PostAsync(apiUrl, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Verification of presentation response successful");
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Verification failed: {0}", errorContent);
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Request failed: {0}", ex.Message);
                return $"Request failed: {ex.Message}";
            }
        }
        private async Task<string> VerifyVerifiablePresentationAsyncwithId(string transactionId)
        {
            try
            {
                // Build the URL with the transactionId appended at the end
                var Urlapi = _configuration["ApiSettings:VerifyVptokenwithIdUrl"];
               
                var urlWithTransactionId = $"{Urlapi}/{transactionId}";

                _logger.LogInformation("Sending GET request to: {0}", urlWithTransactionId);

                // Make the GET request
                var response = await _httpClient.GetAsync(urlWithTransactionId);             

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Verification successful for transactionId: {0}", transactionId);
                    return await response.Content.ReadAsStringAsync(); // Return the response content
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Verification failed for transactionId: {0}. Status: {1}, Error: {2}",
                        transactionId, response.StatusCode, errorContent);
                    var resultError = new ServiceResult(false, "Verification failed for transactionId", (int)response.StatusCode, "", errorContent);

                    // Serialize to JSON string and return
                    return JsonConvert.SerializeObject(resultError);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("HTTP request failed for transactionId: {0}. Error: {1}", transactionId, ex.Message);
                return $"Request failed: {ex.Message}";
            }
        }

        public async Task<string> VerifyPresentationResponseAsync_with_id(string transactionId)
        {
            try
            {
                var redisValue = await _redisTransactionStore.GetRequiredStringAsync(
                    transactionId,
                    transactionId,
                    "presentation-response");

                dynamic presentationResponse;
                try
                {
                    presentationResponse = JsonConvert.DeserializeObject<dynamic>(redisValue)
                        ?? throw TransactionStateException.DeserializationFailed(transactionId, transactionId);
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    throw TransactionStateException.DeserializationFailed(transactionId, transactionId, ex);
                }

                if(presentationResponse?.isRejected == true)
                {
                    _logger.LogInformation("Presentation submission is rejected by the holder for transactionId: {0}", transactionId);
                    
                    var result = new ServiceResult(false, "Presentation submission is rejected by the holder.", 400, "", null);

                    // Serialize to JSON string and return
                    return JsonConvert.SerializeObject(result);

                }
                else if (presentationResponse?.presentation_submission == null &&
                    presentationResponse?.response_type == "vp_token" &&
                    presentationResponse?.response_mode == "direct_post")
                {
                    _logger.LogInformation("Data not yet posted for transaction ID: {0}", transactionId);
                    var result1= new ServiceResult(false, "Data not yet posted for transaction ID", 400, "", null);

                    // Serialize to JSON string and return
                    return JsonConvert.SerializeObject(result1);
                }

                // Case: Valid presentation_submission and vp_token are present
                else if (presentationResponse?.presentation_submission != null &&
                         presentationResponse?.vp_token != null)
                {
                    // Directly call the verification method with transactionId
                    var verifyResultString = await VerifyVerifiablePresentationAsyncwithId(transactionId);

                    // Return the raw result from the verification API (as string)
                    return verifyResultString;
                }

                // Case: Invalid format
                else
                {
                    _logger.LogError("Invalid presentation response format for transactionId: {0}", transactionId);
                    var result2 = new ServiceResult(false, "Invalid presentation response format for transactionId", 400, "", null);

                    // Serialize to JSON string and return
                    return JsonConvert.SerializeObject(result2);
                   
                }
            }
            catch (TransactionStateException ex)
            {
                _logger.LogWarning(ex, "Invalid or expired transaction while verifying by id. TransactionId={TransactionId}", transactionId);
                var result = new ServiceResult(false, ex.Message, 400, "", null);
                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying presentation response for transactionId: {0}", transactionId);
                var result3 = new ServiceResult(false, "Error verifying presentation response for transactionId", 500, "", ex.Message);
                return JsonConvert.SerializeObject(result3);
            }
        }



        public ServiceResult prepareRequestURI(string docType, Dictionary<string, List<string>> claims)
        {
            string transactionId = Guid.NewGuid().ToString("N");
            string definitionId = Guid.NewGuid().ToString("N");
            string transactionUrl = "";

            try
            {
                if (string.IsNullOrEmpty(docType) || claims == null || claims.Count == 0 || claims.All(c => c.Value == null || c.Value.Count == 0))
                {
                    return new ServiceResult(false, "Invalid input data", 400, "Invalid request body", null);
                }

                List<Dictionary<string, object>> fieldsList = new List<Dictionary<string, object>>();

                foreach (var claim in claims)
                {
                    foreach (var name in claim.Value)
                    {
                        fieldsList.Add(new Dictionary<string, object>
                {
                    { "path",  $"$.{docType}.namespaces.{claim.Key}.{name}"  },
                    { "filter", null }
                });
                    }
                }

                var presentationDefinition = new Dictionary<string, object>
        {
            { "presentationDefinition", new Dictionary<string, object>
                {
                    { "id", definitionId },
                    { "format", new Dictionary<string, object>
                        {
                            { "mso_mdoc", new Dictionary<string, object>
                                {
                                    { "proof_type", new List<string> { "COSE_KEY" } }
                                }
                            }
                        }
                    },
                    { "input_descriptors", new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object>
                            {
                                { "constraints", new Dictionary<string, object>
                                    {
                                        { "fields", fieldsList }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

                var setOk = _redisDb.StringSet(
                    transactionId,
                    JsonConvert.SerializeObject(presentationDefinition),
                    expiry: _transactionDataTtl,
                    when: When.Always);
                if (!setOk)
                {
                    throw TransactionStateException.StorageFailed(transactionId, transactionId);
                }
                _logger.LogInformation(
                    "Stored mdoc presentation definition in Redis. TransactionId={TransactionId} TTL={Ttl}",
                    transactionId,
                    _transactionDataTtl);

                transactionUrl = $"{_configuration["ApiSettings:MdocUrl"]}/api/mdoc/getPresentationDefinition/{transactionId}";

            }
            catch (LxException ex)
            {
                _logger.LogError("Error occurred while generating presentation definition: {0}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while generating presentation definition: {0}", ex.Message);
                throw new LxException(ex.Message, LxErrorCodes.E_UNSPECIFIED_ERROR);
            }

            return new ServiceResult(true, "Presentation definition generated successfully", 0, "Success", transactionUrl);
        }


        public ServiceResult getPresentationDefinition(string transactionId)
        {
            try
            {
                var data = _redisTransactionStore
                    .ConsumeRequiredStringAsync(transactionId, transactionId, "mdoc-presentation-definition")
                    .GetAwaiter()
                    .GetResult();

                string URL = $"{_configuration["ApiSettings:MdocUrl"]}/api/mdoc/postISO/{transactionId}";

                var responseObject = new
                {
                    presentationDefinition = data,
                    responseURI = URL
                };

                return new ServiceResult(true, "Presentation data fetched successfully", 0, "Success", responseObject);
            }
            catch (TransactionStateException ex)
            {
                _logger.LogWarning(ex, "Transaction not found or expired while fetching mdoc presentation definition. TransactionId={TransactionId}", transactionId);
                throw new LxException(ex.Message, LxErrorCodes.E_INVALID_REQUEST);
            }
            catch (LxException ex)
            {
                _logger.LogError("Error occurred while fetching presentation data for transactionId {0}: {1}", transactionId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while fetching presentation data for transactionId {0}: {1}", transactionId, ex.Message);
                throw new LxException(ex.Message, LxErrorCodes.E_UNSPECIFIED_ERROR);
            }
        }

        public ServiceResult parsePresentationDefinition(object requestData)
        {
           

            string presentationId = "";
            string MdocRequest = "";
            try
            {
                JObject reqObj = JObject.Parse(requestData.ToString());


                var presentationDefinitionData = reqObj["presentationDefinition"];
                if (presentationDefinitionData == null)
                {
                    _logger.LogWarning("No presentationDefinition provided in the request.");
                    return new ServiceResult(false, "presentationDefinition is missing in the request.", 400, "Invalid request body", null);
                }

                presentationId = presentationDefinitionData["id"]?.ToString();
                if (string.IsNullOrEmpty(presentationId))
                {
                    _logger.LogWarning("No ID found in presentationDefinition.");
                    throw new LxException("presentationDefinition is missing in the request.", LxErrorCodes.E_UNSPECIFIED_ERROR);
                }

                var inputDescriptors = presentationDefinitionData["input_descriptors"] as JArray;
                if (inputDescriptors == null)
                {
                    _logger.LogWarning("No input_descriptors provided in the request.");
                    throw new LxException("input_descriptors is missing in the presentationDefinition.", LxErrorCodes.E_UNSPECIFIED_ERROR);
                }

                var dynamicAttributes = new Dictionary<string, Dictionary<string, string>>();
                string? docType = null;
                string? nameSpaces = null;

                // Iterate over the input_descriptors and extract fields dynamically
                foreach (var descriptor in inputDescriptors)
                {
                    var constraints = descriptor["constraints"];
                    if (constraints != null && constraints["fields"] is JArray fieldsArray)
                    {
                        foreach (var field in fieldsArray)
                        {
                            if (field["path"] != null)
                            {
                                string path = field["path"].ToString();

                                if (path.StartsWith("$."))
                                {
                                    path = path.Substring(2);
                                }
                                int namespaceIndex = path.IndexOf(".namespaces.");
                                if (namespaceIndex > 0)
                                {
                                    docType = path.Substring(0, namespaceIndex); // Extract docType from path
                                }

                                int namespacesStartIndex = path.IndexOf(".namespaces.") + 11; // Move past ".namespaces."
                                if (namespacesStartIndex > 10) // Ensure ".namespaces." exists
                                {
                                    string subPath = path.Substring(namespacesStartIndex);
                                    string[] subPathParts = subPath.Split('.');
                                    if (subPathParts.Length > 1)
                                    {
                                        nameSpaces = string.Join(".", subPathParts.Take(subPathParts.Length - 1)); // Exclude last part
                                    }
                                }

                                if (!string.IsNullOrEmpty(nameSpaces) && nameSpaces.StartsWith("."))
                                {
                                    nameSpaces = nameSpaces.Substring(1);
                                }


                                string[] pathParts = path.Split('.');

                                // Extract the last part of the path as the attribute (e.g., "name", "birthdate")
                                string attribute = pathParts.Last().Trim();

                                if (!dynamicAttributes.ContainsKey(attribute))
                                {
                                    dynamicAttributes[attribute] = new Dictionary<string, string>
                                    {
                                        ["description"] = attribute,
                                        ["attribute"] = attribute
                                    };
                                }
                            }
                        }
                    }
                }



                var modifiedPresentationDefinition = new Dictionary<string, object>
                {
                    ["docType"] = docType,
                    ["issuerCertificateChain"] = "LS0tLS1CRUdJTiBDRVJUSUZJQ0FURS0tLS0tDQpNSUlCTnpDQjNxQURBZ0VDQWdFQk1Bb0dDQ3FHU000OUJBTUNNQnN4R1RBWEJnTlZCQU1NRUdZNU1qQXdPV1U0DQpOVE5pTm1Jd05EVXdIaGNOTWpRd05URTNNVEV5TURBeFdoY05NalV3TlRFM01URXlNREF4V2pBYk1Sa3dGd1lEDQpWUVFEREJCbU9USXdNRGxsT0RVellqWmlNRFExTUZrd0V3WUhLb1pJemowQ0FRWUlLb1pJemowREFRY0RRZ0FFDQoydDZMV3UvQXFBTGh5ZG95RHFXYlh4TEFMbWZJdkdSdkRKWWg3R2p0OWdMa3UxbE4zSTZUMFlHcGFhclFNdGk3DQorV1Q2aFZwdE41OGljSk4yNG5qK2U2TVRNQkV3RHdZRFZSMFRBUUgvQkFVd0F3RUIvekFLQmdncWhrak9QUVFEDQpBZ05JQURCRkFpQTU4TVRlTGVicnBKbUN3MER6d2dpQy9UNTBNMTdZM2xZdVpIZVBMeDdYWHdJaEFMd1duSEtYDQpXMFdocW54UnVWSURtdk5LTlRvcjN4bHl3OG9vU0Y1dGhiNXINCi0tLS0tRU5EIENFUlRJRklDQVRFLS0tLS0NCi0tLS0tQkVHSU4gQ0VSVElGSUNBVEUtLS0tLQ0KTUlJQlZUQ0IrNkFEQWdFQ0FnRUJNQW9HQ0NxR1NNNDlCQU1DTUJzeEdUQVhCZ05WQkFNTUVHWTVNakF3T1dVNA0KTlROaU5tSXdORFV3SGhjTk1qUXdOVEUzTVRFeU1ETTFXaGNOTWpVd05URTNNVEV5TURNMVdqQXJNU2t3SndZRA0KVlFRRERDQXlaakJpT0RrMU9HSXhPRFprTmprd016VTFZVE5oWVRBMk1UTmtOamxqT0RCWk1CTUdCeXFHU000OQ0KQWdFR0NDcUdTTTQ5QXdFSEEwSUFCR2g0MXEzSmJId05VaVF6bEpuSWRCZWVJTnpNSVZkazBROS9TS2NqOGM2Mw0KcnFzakpTYXZRUVIrV0JIN1ZiUU0wUGJBYis4NXg3OWxJV0l5K2hHZ0lEdWpJREFlTUE4R0ExVWRFd0VCL3dRRg0KTUFNQkFmOHdDd1lEVlIwUEJBUURBZ0tFTUFvR0NDcUdTTTQ5QkFNQ0Ewa0FNRVlDSVFEUkVkUTVmeHJBSW1aMA0KNFB6UWxjbmMzY0dHbVJoOUU4dFlQam1pQlZ4NkNnSWhBTkduUTlISlJ2d1BzbmVuYXRWV0hPQWo1dWhNSVVhMA0KMlI1SWQ5MEE5dmY2DQotLS0tLUVORCBDRVJUSUZJQ0FURS0tLS0tDQotLS0tLUJFR0lOIENFUlRJRklDQVRFLS0tLS0NCk1JSUJaVENDQVF1Z0F3SUJBZ0lCQVRBS0JnZ3Foa2pPUFFRREFqQXJNU2t3SndZRFZRUUREQ0F5WmpCaU9EazENCk9HSXhPRFprTmprd016VTFZVE5oWVRBMk1UTmtOamxqT0RBZUZ3MHlOREExTVRjeE1USXhNRGRhRncweU5UQTENCk1UY3hNVEl4TURkYU1Dc3hLVEFuQmdOVkJBTU1JR1U1TkdWak1XRTRaamhpT1RnM1pURXpNRFJqWVRjMk0yTXoNCk1tTXhZV1UzTUZrd0V3WUhLb1pJemowQ0FRWUlLb1pJemowREFRY0RRZ0FFakFCbTF0Rlltd25zRXZVVHY4elINCm5JREJBRVJ4YVZmdkRZT3RPLzFNd3JQZW1KdFRsY0lJazVva1orckc2WjkrZWtyckFDT0pJRDlXV3o3RGsxdVYNCnU2TWdNQjR3RHdZRFZSMFRBUUgvQkFVd0F3RUIvekFMQmdOVkhROEVCQU1DQW9Rd0NnWUlLb1pJemowRUF3SUQNClNBQXdSUUlnYjhtS0lNbWJSeEYzL2N0M0dhYWMzc0dtYVJ6c0RVWDBMQmNCUVhrQ0RBUUNJUURVbWQrR0p3ZXQNCkZYM0xNSFRvTlR2ZmQ1Z2xzSjdoOFd5dTlYRGw1cHg0UVE9PQ0KLS0tLS1FTkQgQ0VSVElGSUNBVEUtLS0tLQ==",
                    ["nameSpaces"] = new List<string> { nameSpaces },
                    [docType] = new Dictionary<string, object>
                    {
                        ["namespaces"] = new Dictionary<string, object>
                        {
                            [nameSpaces] = dynamicAttributes.Values.ToList()
                        }
                    }
                };

                


                var qrDataString = reqObj["qrEngagementData"]?.ToString();
                if (string.IsNullOrEmpty(qrDataString))
                {
                    _logger.LogWarning("No qrDataString provided in the request.");
                    throw new LxException("qrDataString is missing in the request.", LxErrorCodes.E_UNSPECIFIED_ERROR);
                }

                string QRDataKey = $"qrData:{presentationId}";
                _redisTransactionStore
                    .StoreStringAsync(QRDataKey, presentationId, qrDataString, "mdoc-qr-data")
                    .GetAwaiter()
                    .GetResult();

                LX_MDOC_DATA mdocData = new LX_MDOC_DATA();
                PKIMethods.Instance.PKIParseMDOCDeviceEngagement(qrDataString, ref mdocData);

              

                string jsonData = JsonConvert.SerializeObject(modifiedPresentationDefinition);

                string MPresentationDefinitionKey = $"jsonData:{presentationId}";
                _redisTransactionStore
                    .StoreStringAsync(MPresentationDefinitionKey, presentationId, jsonData, "mdoc-presentation-json")
                    .GetAwaiter()
                    .GetResult();

                byte[] mdocRequest = null;
                PKIMethods.Instance.PKIPrepareMDOCRequests(jsonData, ref mdocRequest);

                MdocRequest = mdocRequest != null && mdocRequest.Length > 0
                    ? Convert.ToBase64String(mdocRequest)
                    : string.Empty;  // Empty string if no data is returned

                if (string.IsNullOrEmpty(MdocRequest))
                {
                    _logger.LogError("MDOC Request is empty. Unable to proceed.");
                    throw new LxException("MDOC Request is empty.", LxErrorCodes.E_UNSPECIFIED_ERROR);
                }

               

                PKIMethods.Instance.PKICleanupMDOCContexts();

            }
            catch (TransactionStateException ex)
            {
                _logger.LogError(ex, "Redis transaction state error while parsing presentation definition. PresentationId={PresentationId}", presentationId);
                throw new LxException(ex.Message, LxErrorCodes.E_INVALID_REQUEST);
            }
            catch (LxException ex)
            {
                _logger.LogError(ex, "An error occurred while processing the presentation and engagement data.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the presentation and engagement data.");
                throw new LxException(ex.Message, LxErrorCodes.E_UNSPECIFIED_ERROR);
            }
            return new ServiceResult(true, "MDOC request processed successfully", 0, "Success", new { MdocRequest, presentationId });
        }


        public ServiceResult parseISO(object requestData)
        {
            string decryptedData = "";
            try
            {
                JObject reqObj = JObject.Parse(requestData.ToString());
                var CBOREncodeddata = reqObj["CBOREncodedDevice"];
                if (CBOREncodeddata == null)
                {
                    _logger.LogWarning("No CBOREncodeddata provided in the request.");
                    throw new LxException("CBOREncodeddata is missing in the request.", LxErrorCodes.E_UNSPECIFIED_ERROR);
                }

                // Extract presentation ID from request
                var presentationId = reqObj["presentationId"];
                if (presentationId==null)
                {
                    _logger.LogWarning("No ID provided in the presentationDefinition.");
                    throw new LxException("ID is missing in the presentationDefinition.", LxErrorCodes.E_UNSPECIFIED_ERROR);
                }

                // Define keys for Redis retrieval
                string QRDataKey = $"qrData:{presentationId}";
                string qrDataString = _redisTransactionStore
                    .ConsumeRequiredStringAsync(QRDataKey, presentationId.ToString(), "mdoc-qr-data")
                    .GetAwaiter()
                    .GetResult();

                string MPresentationDefinitionKey = $"jsonData:{presentationId}";
                string jsonData = _redisTransactionStore
                    .ConsumeRequiredStringAsync(MPresentationDefinitionKey, presentationId.ToString(), "mdoc-presentation-json")
                    .GetAwaiter()
                    .GetResult();

                LX_MDOC_DATA mdocData = new LX_MDOC_DATA();

                // Parse the QR data string into MDOC data
                PKIMethods.Instance.PKIParseMDOCDeviceEngagement(qrDataString, ref mdocData);

                byte[] mdocRequest = null;
                // Prepare the MDOC request
                PKIMethods.Instance.PKIPrepareMDOCRequests(jsonData, ref mdocRequest);
               
                string MdocRequest = mdocRequest != null && mdocRequest.Length > 0
                ? Convert.ToBase64String(mdocRequest)
                : string.Empty;  // Empty string if no data is returned

                // Log or use the Base64-encoded MDOC request
                if (!string.IsNullOrEmpty(MdocRequest))
                {
                    _logger.LogInformation("MDOC Request prepared successfully.");
                }
                else
                {
                    _logger.LogWarning("MDOC Request is empty.");
                }

                byte[] CBORDataByte;
                try
                {
                    string cborHexString = CBOREncodeddata.ToString(); // Convert JToken to string
                    CBORDataByte = Convert.FromHexString(cborHexString);
                }
                catch (FormatException)
                {
                    _logger.LogError("Invalid hex string format in requestData.");
                    throw new LxException("Invalid hex string format in requestData.", LxErrorCodes.E_UNSPECIFIED_ERROR);
                }

                // Decrypt the data
                decryptedData = "";

                PKIMethods.Instance.MDOCDecryptMessageFromDevice(CBORDataByte, ref decryptedData);

                PKIMethods.Instance.PKICleanupMDOCContexts();
                
            }
            catch (TransactionStateException ex)
            {
                _logger.LogWarning(ex, "Expired or invalid Redis transaction for parseISO. PresentationId={PresentationId}", requestData?.ToString());
                throw new LxException(ex.Message, LxErrorCodes.E_INVALID_REQUEST);
            }
            catch (LxException ex)
            {
                _logger.LogError("An error occurred while processing the request: {ErrorMessage}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while processing the request: {ErrorMessage}", ex.Message);
                throw new LxException(ex.Message, LxErrorCodes.E_UNSPECIFIED_ERROR);
            }
            return new ServiceResult(true, "Decrypted data successfully.", 200, "OK", decryptedData );
        }


        public ServiceResult postISO(string transactionId, object requestData)
        {
            try
            {
                //JObject reqObj = JObject.Parse(requestData.ToString());

                if (string.IsNullOrEmpty(transactionId))
                {
                    throw new LxException("TransactionId is missing in the request.", LxErrorCodes.E_UNSPECIFIED_ERROR);
                }

                if (requestData == null)
                {
                    throw new LxException("Request body is missing.", LxErrorCodes.E_UNSPECIFIED_ERROR);
                }

                // Convert JObject to string and store it in Redis
                string jsonString = requestData.ToString();

                string redisKey = $"{transactionId}_mdoc";
                _redisTransactionStore
                    .StoreStringAsync(redisKey, transactionId, jsonString, "mdoc-iso-post")
                    .GetAwaiter()
                    .GetResult();

                return new ServiceResult(true, "JSON object saved successfully.", 200, "OK", "Posted Successfully");
            }
            catch (TransactionStateException ex)
            {
                _logger.LogError(ex, "Failed to store ISO data in Redis. TransactionId={TransactionId}", transactionId);
                throw new LxException(ex.Message, LxErrorCodes.E_INVALID_REQUEST);
            }
            catch (LxException ex)
            {
                _logger.LogError("An error occurred while processing the request: {ErrorMessage}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while processing the request: {ErrorMessage}", ex.Message);
                throw new LxException(ex.Message, LxErrorCodes.E_UNSPECIFIED_ERROR);
            }
        }

        public async Task<ServiceResult> getISO(string transactionId)
        {
            try
            {
                var response = "";

                var redisKey = $"{transactionId}_mdoc";

                var pollingInterval = TimeSpan.FromSeconds(10);
                var maxPollingAttempts = 30; // Adjust based on your requirements
                var pollingAttempts = 0;

                while (pollingAttempts < maxPollingAttempts)
                {
                    var exists = await _redisDb.KeyExistsAsync(redisKey);
                    if (exists)
                    {
                        response = await _redisTransactionStore.ConsumeRequiredStringAsync(
                            redisKey,
                            transactionId,
                            "mdoc-iso-post");
                        break;
                    }

                    // Delay before the next poll
                    await Task.Delay(pollingInterval);
                    pollingAttempts++;
                }

                if (string.IsNullOrEmpty(response))
                {
                    return new ServiceResult(false, "An error occurred while saving request data.", 500, "Internal Server Error", "Operation timed out");
                }

                return new ServiceResult(true, "JSON object saved successfully.", 200, "OK", response);
            }
            catch (TransactionStateException ex)
            {
                _logger.LogWarning(ex, "Expired or invalid transaction while fetching ISO data. TransactionId={TransactionId}", transactionId);
                throw new LxException(ex.Message, LxErrorCodes.E_INVALID_REQUEST);
            }
            catch (LxException ex)
            {
                _logger.LogError("An error occurred while processing the request: {ErrorMessage}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while processing the request: {ErrorMessage}", ex.Message);
                throw new LxException(ex.Message, LxErrorCodes.E_UNSPECIFIED_ERROR);
            }
        }
    }
}