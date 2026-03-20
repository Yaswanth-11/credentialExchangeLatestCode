using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Lux.Infrastructure.Native
{
    public static class PKIServiceMethods
    {
        public static int PKIcsrGen(string keyId, int keyIdLen, ref IntPtr csr, ref int csrLen)
        {
            int result = 0;
            try
            {
                result = NativeMethods.csrGen(keyId, keyIdLen, ref csr, ref csrLen);
                if (result == 2)
                {
                    throw new LxException("Error in generate CSR native as keyId already exists", LxErrorCodes.E_UNSPECIFIED_ERROR);
                }
                if (result == 0)
                {
                    throw new LxException("Error in generate CSR native", LxErrorCodes.E_UNSPECIFIED_ERROR);
                }
            }
            catch (LxException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LxException(ex.Message);
            }
            return result;
        }

        //public static void InitializePKI(string configParams)
        //{
        //    try
        //    {
        //        int result = NativeMethods.InitializePKINative(configParams);
        //        if (result != 0)
        //        {
        //            string error = "Failed to initialize PKI Service.";
        //            throw new LxException(error, result);
        //        }
        //    }
        //    catch (LxException)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new LxException(ex.Message);
        //    }
        //}

        //public static string SetPIN(string setPINRequest)
        //{
        //    // local variables
        //    string response;
        //    IntPtr responseBuffer = IntPtr.Zero;
        //    int responseBufferLength = 0;

        //    try
        //    {
        //        int result = NativeMethods.SetPINNative(
        //             setPINRequest,
        //             ref responseBuffer,
        //             ref responseBufferLength);
        //        if (result == 0)
        //        {
        //            response = Marshal.PtrToStringAnsi(responseBuffer, responseBufferLength);
        //        }
        //        else
        //        {
        //            string error = GetStatusMessagePKI(result);
        //            throw new LxException(error, result);
        //        }

        //        return response;
        //    }
        //    catch (LxException)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //    finally
        //    {
        //        if (responseBuffer != IntPtr.Zero)
        //        {
        //            // Free the buffer
        //            FreeMemoryPKI(responseBuffer);
        //            responseBuffer = IntPtr.Zero;
        //        }
        //    }
        //}

        //public static string SignPADES(string signRequest)
        //{
        //    // local variables
        //    string response;
        //    IntPtr responseBuffer = IntPtr.Zero;
        //    int responseBufferLength = 0;

        //    try
        //    {
        //        int result = NativeMethods.SignPADESNative(
        //             signRequest,
        //             ref responseBuffer,
        //             ref responseBufferLength);
        //        if (result == 0)
        //        {
        //            response = Marshal.PtrToStringAnsi(responseBuffer, responseBufferLength);
        //        }
        //        else
        //        {
        //            string error = GetStatusMessagePKI(result);
        //            throw new LxException(error, result);
        //        }

        //        return response;
        //    }
        //    catch (LxException)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //    finally
        //    {
        //        if (responseBuffer != IntPtr.Zero)
        //        {
        //            // Free the buffer
        //            FreeMemoryPKI(responseBuffer);
        //            responseBuffer = IntPtr.Zero;
        //        }
        //    }
        //}

        //internal static string GetStatusMessagePKI(int errorCode)
        //{
        //    string response = null;
        //    IntPtr responseBuffer = IntPtr.Zero;

        //    try
        //    {
        //        responseBuffer = NativeMethods.GetStatusMessagePKINative(errorCode);

        //        response = Marshal.PtrToStringAnsi(responseBuffer);
        //        return response;
        //    }
        //    catch (LxException)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //internal static void FreeMemoryPKI(IntPtr buffer)
        //{
        //    try
        //    {
        //        int result = NativeMethods.FreeMemoryPKINative(buffer);
        //        if (result != 0)
        //        {
        //            string error = GetStatusMessagePKI(result);
        //            throw new LxException(error, result);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public static void CleanupPKI()
        //{
        //    try
        //    {
        //        int result = NativeMethods.CleanupPKINative();
        //        if (result != 0)
        //        {
        //            string error = GetStatusMessagePKI(result);
        //            throw new LxException(error, result);
        //        }
        //    }
        //    catch (LxException)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public static byte[] GenerateTimestamp(byte[] timestampRequest)
        //{
        //    // local variables
        //    byte[] response;
        //    IntPtr responseBuffer = IntPtr.Zero;
        //    int responseBufferLength = 0;

        //    try
        //    {
        //        int result = NativeMethods.GenerateTimestampNative(
        //             timestampRequest,
        //             timestampRequest.Length,
        //             ref responseBuffer,
        //             ref responseBufferLength);
        //        if (result == 0)
        //        {
        //            response = new byte[responseBufferLength];
        //            Marshal.Copy(responseBuffer, response, 0, responseBufferLength);
        //        }
        //        else
        //        {
        //            string error = GetStatusMessagePKI(result);
        //            throw new LxException(error, result);
        //        }

        //        return response;
        //    }
        //    catch (LxException)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //    finally
        //    {
        //        if (responseBuffer != IntPtr.Zero)
        //        {
        //            // Free the buffer
        //            FreeMemoryPKI(responseBuffer);
        //            responseBuffer = IntPtr.Zero;
        //        }
        //    }
        //}

        //public static byte[] POSDigiTimeStamp(byte[] timestampRequest)
        //{
        //    // local variables
        //    byte[] response;
        //    IntPtr responseBuffer = IntPtr.Zero;
        //    int responseBufferLength = 0;

        //    try
        //    {
        //        int result = NativeMethods.POSDigiTimeStampNative(
        //             timestampRequest,
        //             timestampRequest.Length,
        //             ref responseBuffer,
        //             ref responseBufferLength);
        //        if (result == 0)
        //        {
        //            response = new byte[responseBufferLength];
        //            Marshal.Copy(responseBuffer, response, 0, responseBufferLength);
        //        }
        //        else
        //        {
        //            string error = GetStatusMessagePKI(result);
        //            throw new LxException(error, result);
        //        }

        //        return response;
        //    }
        //    catch (LxException)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        //var failResponse = new FailResponse
        //        //{
        //        //    Status = "Fail",
        //        //    ErrorCode = ex.ErrorCode,
        //        //    ErrorMessage = ex.Message
        //        //};
        //        //return Ok(failResponse);
        //        throw ex;
        //    }
        //    finally
        //    {
        //        if (responseBuffer != IntPtr.Zero)
        //        {
        //            // Free the buffer
        //            FreeMemoryPKI(responseBuffer);
        //            responseBuffer = IntPtr.Zero;
        //        }
        //    }
        //}
    }
}