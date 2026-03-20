using Lux.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.InteropServices;
using Credential.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Runtime.InteropServices;

namespace Credential.Services.Utilities
{
    public sealed class PKIMethods
    {
        private static readonly Lazy<PKIMethods> instance = new Lazy<PKIMethods>(() => new PKIMethods());

        private PKIMethods()
        {
            //InitializePKI();
            InitializePKIUtils();
        }

        public static PKIMethods Instance
        {
            get
            {
                return instance.Value;
            }
        }
        private void InitializePKI()
        {
            try
            {
                string config = File.ReadAllText("./config_pki.json");
                int result = NativeMethods.InitializePKINative(config);
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_NATIVE_INIT);
                }
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_NATIVE_INIT);
            }
        }
        private void InitializePKIUtils()
        {
            try
            {
                int result = NativeMethods.InitializePKIUtilsNative();
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_NATIVE_INIT);
                }
                //NativeMethods.ReportSentryErrorNative("Mdoc test");
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_NATIVE_INIT);
            }
        }

        public void PKIIssueCertificate(string issuerId, ref string issuerCert)
        {
            IntPtr issuerCertPtr = IntPtr.Zero;
            int issuerCertLen = 0;

            try
            {
                JObject req = new JObject();
                JObject callStackObj = new JObject();
                callStackObj["walletCertificate"] = true;
                callStackObj["keyID"] = issuerId;
                callStackObj["commonName"] = issuerId;
                callStackObj["subscriberDigitalID"] = issuerId;
                callStackObj["countryName"] = "IN";

                req["callStack"] = callStackObj;

                string request = JsonConvert.SerializeObject(req);

                int result = NativeMethods.IssueCertificateNative(request,
                    ref issuerCertPtr, ref issuerCertLen);
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_GET_EC_KEY);
                }

                issuerCert = Marshal.PtrToStringAnsi(issuerCertPtr, issuerCertLen);
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_GET_EC_KEY);
            }
        }


        public void PKIGenerateCertificateChain(string issuerId, ref string certChain, ref string issuerCert)
        {
            IntPtr certChainPtr = IntPtr.Zero;
            IntPtr issuerCertPtr = IntPtr.Zero;
            int certChainLen = 0;
            int issuerCertLen = 0;

            try
            {
                int result = NativeMethods.PKIGenerateCertificateChainNative(issuerId, ref certChainPtr
                    , ref certChainLen, ref issuerCertPtr, ref issuerCertLen);
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_GET_EC_KEY);
                }

                certChain = Marshal.PtrToStringAnsi(certChainPtr, certChainLen);
                issuerCert = Marshal.PtrToStringAnsi(issuerCertPtr, issuerCertLen);
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_GET_EC_KEY);
            }
        }

        public void PKIGetECKey(ref IntPtr key, ref int keyLen)
        {
            try
            {
                int result = NativeMethods.GetECKey(ref key, ref keyLen);
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_GET_EC_KEY);
                }
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_GET_EC_KEY);
            }
        }

        public string AddChecksum(string data)
        {
            // local variables
            string response;
            IntPtr responseBuffer = IntPtr.Zero;
            int responseBufferLength = 0;

            try
            {
                if (String.IsNullOrEmpty(data))
                {
                    throw new LxException("Input data must not be null or empty, in the AddChecksum \"data\" parameter", LxErrorCodes.E_INVALID_ARGUMENT);
                }

                int result = NativeMethods.AddChecksumNative(
                     data,
                     data.Length,
                     ref responseBuffer,
                     ref responseBufferLength);
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_NATIVE_ADD_CHECKSUM);
                }

                response = Marshal.PtrToStringAnsi(responseBuffer, responseBufferLength);

                return response;
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_NATIVE_ADD_CHECKSUM);
            }
            finally
            {
                if (responseBuffer != IntPtr.Zero)
                {
                    // Free the buffer
                    FreeMemoryPKI(responseBuffer);
                    responseBuffer = IntPtr.Zero;
                }
            }
        }
        public byte[] MDOCProvisioningRequestParser(byte[] data, ref int provisionType)
        {
            // local variables
            int dataLen = data.Length;
            IntPtr parsedData = IntPtr.Zero;
            int parsedDataLen = 0;
            byte[] responseData;
            try
            {
                int result = NativeMethods.PKIRequestParserNative(
                     data,
                     dataLen,
                     ref provisionType,
                     ref parsedData,
                     ref parsedDataLen);
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_NATIVE_ADD_CHECKSUM);
                }

                responseData = new byte[parsedDataLen];
                Marshal.Copy(parsedData, responseData, 0, parsedDataLen);

                return responseData;
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_NATIVE_ADD_CHECKSUM);
            }
            finally
            {
                if (parsedData != IntPtr.Zero)
                {
                    // Free the buffer
                    FreeMemoryPKI(parsedData);
                    parsedData = IntPtr.Zero;
                }
            }
        }

        public byte[] MDOCProvisioningParseSessionId(byte[] data)
        {
            // local variables
            int dataLen = data.Length;
            IntPtr parsedData = IntPtr.Zero;
            int parsedDataLen = 0;
            byte[] responseData;
            try
            {
                int result = NativeMethods.PKIParseSessionIDNative(
                     data,
                     dataLen,
                     ref parsedData,
                     ref parsedDataLen);
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_NATIVE_ADD_CHECKSUM);
                }

                responseData = new byte[parsedDataLen];
                Marshal.Copy(parsedData, responseData, 0, parsedDataLen);

                return responseData;
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_NATIVE_ADD_CHECKSUM);
            }
            finally
            {
                if (parsedData != IntPtr.Zero)
                {
                    // Free the buffer
                    FreeMemoryPKI(parsedData);
                    parsedData = IntPtr.Zero;
                }
            }
        }
        public byte[] MDOCProvisioningResponsePreperator(string data)
        {
            // local variables
            IntPtr responseBuffer = IntPtr.Zero;
            int responseBufferLength = 0;
            int dataLen = data.Length;  
            byte[] responseData;
            try
            {
                int result = NativeMethods.PKIResponsePreperatorNative(
                     data,
                     dataLen,
                     ref responseBuffer,
                     ref responseBufferLength);
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_NATIVE_ADD_CHECKSUM);
                }


                responseData = new byte[responseBufferLength];
                Marshal.Copy(responseBuffer, responseData, 0, responseBufferLength);

                return responseData;
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_NATIVE_ADD_CHECKSUM);
            }
            finally
            {
                if (responseBuffer != IntPtr.Zero)
                {
                    // Free the buffer
                    FreeMemoryPKI(responseBuffer);
                    responseBuffer = IntPtr.Zero;
                }
            }
        }
        private void FreeMemoryPKI(IntPtr buffer)
        {
            try
            {
                int result = NativeMethods.FreeMemoryPKINative(buffer);
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_NATIVE_FREE_MEM);
                }
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_NATIVE_FREE_MEM);
            }
        }

        private string GetStatusMessagePKI(int errorCode)
        {
            string response = null;
            IntPtr responseBuffer = IntPtr.Zero;

            try
            {
                responseBuffer = NativeMethods.GetStatusMessagePKINative(errorCode);

                response = Marshal.PtrToStringAnsi(responseBuffer);
                return response;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_NATIVE_GET_MSG);
            }
        }

        //1
        public void PKIParseMDOCDeviceEngagement(string data, ref LX_MDOC_DATA mdocData)
        {
            try
            {
                // Ensure that the provided data is not null or empty
                if (string.IsNullOrEmpty(data))
                {
                    throw new ArgumentNullException(nameof(data), "Input data cannot be null or empty");
                }
                Console.WriteLine("Calling PKIParseMDOCDeviceEngagementData with input data: " + data);

                // Call the native C++ function via P/Invoke
                int result = NativeMethods.PKIParseMDOCDeviceEngagementData(data, ref mdocData);

                Console.WriteLine("Native call returned: " + result);

                // Handle errors returned by the native function
                if (result != 0)  // Assuming 0 is a success code; adjust if necessary
                {
                    string error = GetStatusMessagePKI(result);
                    // Log the error (optional)
                    Console.WriteLine("Error occurred: " + error);
                    throw new LxException(error, LxErrorCodes.E_PKI_PARSE_MDOC_DEVICE_ENGAGEMENT_DATA);
                }
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred: " + ex.Message);
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_PARSE_MDOC_DEVICE_ENGAGEMENT_DATA);
            }
        }


        //2
        public void PKIPrepareMDOCRequests(string data, ref byte[] mdocRequest)
        {
            IntPtr mdocRequestPtr = IntPtr.Zero;
            int mdocRequestLen = 0;

            try
            {
                // Ensure that the provided data is not null or empty
                if (string.IsNullOrEmpty(data))
                {
                    throw new ArgumentNullException(nameof(data), "Input data cannot be null or empty");
                }

                // Call the native C++ function via P/Invoke
                int result = NativeMethods.PKIPrepareMDOCRequestData(data, ref mdocRequestPtr, ref mdocRequestLen);

                // Handle errors returned by the native function
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_PREPARE_MDOC_REQUEST);
                }

                // Convert the pointer to byte array if successful
                if (mdocRequestLen > 0 && mdocRequestPtr != IntPtr.Zero)
                {
                    mdocRequest = new byte[mdocRequestLen];
                    Marshal.Copy(mdocRequestPtr, mdocRequest, 0, mdocRequestLen);
                }
                else
                {
                    mdocRequest = new byte[0]; // In case of no data returned
                }
            }
            catch (LxException)
            {
                throw;  // Re-throw custom exception if any occurs
            }
            catch (Exception ex)
            {
                // Catch any general exceptions and wrap them in a custom LxException
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_PREPARE_MDOC_REQUEST);
            }
            finally
            {
                // Clean up any unmanaged resources if needed
                //if (mdocRequestPtr != IntPtr.Zero)
                //{
                //    Marshal.FreeCoTaskMem(mdocRequestPtr); // Free allocated memory if necessary
                //}
                if (mdocRequestPtr != IntPtr.Zero)
                {
                    // Free the buffer
                    FreeMemoryPKI(mdocRequestPtr);
                    mdocRequestPtr = IntPtr.Zero;
                }
            }
        }



        //3
        public void MDOCDecryptMessageFromDevice(byte[] data, ref string response)
        {
            // Local variables
            IntPtr responseBuffer = IntPtr.Zero;
            int responseBufferLength = 0;
            int dataLen = data.Length;
            byte[] responseData;

            try
            {
                // Call the native function via P/Invoke
                int result = NativeMethods.PKIDecryptMessageFromDevice(
                    data,
                    dataLen,
                    ref responseBuffer,
                    ref responseBufferLength);

                // Check the result of the native function call
                if (result != 0)
                {
                    // Handle errors with the appropriate message
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_NATIVE_DECRYPT_ERROR);
                }

                if (responseBufferLength > 0 && responseBuffer != IntPtr.Zero)
                {
                    responseData = new byte[responseBufferLength];
                    Marshal.Copy(responseBuffer, responseData, 0, responseBufferLength);

                    response = Encoding.UTF8.GetString(responseData);
                }
                else
                {
                    response = ""; // In case of no data returned
                }
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_NATIVE_DECRYPT_ERROR);
            }
            finally
            {
                // Free the unmanaged memory used for the response buffer
                if (responseBuffer != IntPtr.Zero)
                {
                    // Free the buffer
                    FreeMemoryPKI(responseBuffer);
                    responseBuffer = IntPtr.Zero;
                }
            }
            
        }

        public void PKICleanupMDOCContexts()
        {
            try
            {
                int result = NativeMethods.PKICleanupMDOCContext();
                if (result != 0)
                {
                    string error = GetStatusMessagePKI(result);
                    throw new LxException(error, LxErrorCodes.E_PKI_NATIVE_INIT);
                }
                //NativeMethods.ReportSentryErrorNative("Mdoc test");
            }
            catch (LxException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message, LxErrorCodes.E_PKI_NATIVE_INIT);
            }
        }

    }
}
