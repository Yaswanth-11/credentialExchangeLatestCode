using System;
using System.Runtime.InteropServices;
using Credential.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct LX_MDOC_DATA
{
    public int cipherSuite;
    public int transferType;
    public int transferVersion;
    public int peripheralServerMode;
    public int centralClientMode;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 37)]
    public string serviceUUID;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] ident;

    public uint identLen;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 67)]
    public byte[] connectionMethodString;
}

namespace Credential.Services.Utilities
{
    internal static class NativeMethods
    {
#if Linux
        private const string DLL_PATH = "libPKIServiceNativeUtils.so";
#elif Windows
        private const string DLL_PATH = "PKIServiceNativeUtils.dll";
#endif

#if Linux
        private const string PKI_DLL_PATH = "libPKIService.so";
#elif Windows
        private const string PKI_DLL_PATH = "PKIService.dll";
#endif

        [DllImport(DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "InitializeNativeUtils",
            ThrowOnUnmappableChar = true)]
        internal static extern int InitializePKIUtilsNative();

        [DllImport(DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "PKICleanupMDOCContext",
            ThrowOnUnmappableChar = true)]
        internal static extern int PKICleanupMDOCContext();

        [DllImport(DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "PKIParseMDOCDeviceEngagementData",
            ThrowOnUnmappableChar = true)]
        internal static extern int PKIParseMDOCDeviceEngagementData([MarshalAs(UnmanagedType.LPStr)]  string data,
          ref LX_MDOC_DATA mdoc_data);

        [DllImport(DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "PKIPrepareMDOCRequestData",
            ThrowOnUnmappableChar = true)]
        internal static extern int PKIPrepareMDOCRequestData([MarshalAs(UnmanagedType.LPStr)] string data,
          ref IntPtr mdoc_request, ref int mdoc_request_len);

        [DllImport(DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "PKIDecryptMessageFromDevice",
            ThrowOnUnmappableChar = true)]
        internal static extern int PKIDecryptMessageFromDevice(byte[] data, int dataLen,
            ref IntPtr response, ref int responseLen);

        //[DllImport(DLL_PATH,
        //    BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
        //    CharSet = CharSet.Ansi, EntryPoint = "ReportSentryError",
        //    ThrowOnUnmappableChar = true)]
        //internal static extern void ReportSentryErrorNative(string message);

        [DllImport(PKI_DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "InitializePKI",
            ThrowOnUnmappableChar = true)]
        internal static extern int InitializePKINative(string config);

        [DllImport(PKI_DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "IssueCertificate",
            ThrowOnUnmappableChar = true)]
        internal static extern int IssueCertificateNative(string request,
            ref IntPtr response, ref int responseLen);

        [DllImport(DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "PKIGetECKey",
            ThrowOnUnmappableChar = true)]
        internal static extern int GetECKey(
            ref IntPtr key,
            ref int keyLen);

        [DllImport(DLL_PATH,
           BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi, EntryPoint = "PKIAddChecksumToTransaction",
           ThrowOnUnmappableChar = true)]
        internal static extern int AddChecksumNative(
           [MarshalAs(UnmanagedType.LPStr)] string data,
           int dataLength,
           ref IntPtr checksum,
           ref int checksumLength);

        [DllImport(DLL_PATH,
           BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi, EntryPoint = "PKIVerifyChecksumOfTransaction",
           ThrowOnUnmappableChar = true)]
        internal static extern int VerifyChecksumNative(
           [MarshalAs(UnmanagedType.LPStr)] string data,
           int dataLength);

        [DllImport(DLL_PATH,
           BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi, EntryPoint = "PKIWrapSecureData",
           ThrowOnUnmappableChar = true)]
        internal static extern int PKIWrapSecureDataNative(
           [MarshalAs(UnmanagedType.LPStr)] string plainData,
           int dataLength,
           int fileFormatVersion,
           int fileContentVersion,
           int configType,
           ref IntPtr wrappedData,
           ref int wrappedDataLength);

        [DllImport(DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "CleanupNativeUtils",
            ThrowOnUnmappableChar = true)]
        internal static extern int CleanupPKINative();

        [DllImport(DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "PKINativeUtilsGetStatusMessage",
            ThrowOnUnmappableChar = true)]
        internal static extern IntPtr GetStatusMessagePKINative(int errorCode);

        [DllImport(DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "PKIFreeMemory",
            ThrowOnUnmappableChar = true)]
        internal static extern int FreeMemoryPKINative(
            IntPtr buffer);


        [DllImport(DLL_PATH,
           BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi, EntryPoint = "PKICreateSecureWireData",
           ThrowOnUnmappableChar = true)]
        internal static extern int PKICreateSecureWireDataNative(
           [MarshalAs(UnmanagedType.LPStr)] string plainData,
           int dataLength,
           ref IntPtr SecureWireData,
           ref int SecureWireDataLength);

        [DllImport(DLL_PATH,
           BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi, EntryPoint = "PKIDecryptSecureWireData",
           ThrowOnUnmappableChar = true)]
        internal static extern int PKIDecryptSecureWireDataNative(
           [MarshalAs(UnmanagedType.LPStr)] string secureWireData,
           int secureWireDataLength,
           ref IntPtr plainData,
           ref int plainDataLength);

        [DllImport(DLL_PATH,
           BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi, EntryPoint = "PKIResponsePreperator",
           ThrowOnUnmappableChar = true)]
        internal static extern int PKIResponsePreperatorNative(
           [MarshalAs(UnmanagedType.LPStr)] string request,
           int requestLen,
           ref IntPtr response,
           ref int responseLen);

        [DllImport(DLL_PATH,
           BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi, EntryPoint = "PKIRequestParser",
           ThrowOnUnmappableChar = true)]
        internal static extern int PKIRequestParserNative(
           byte[] request,
           int requestLen,
           ref int provisionType,
           ref IntPtr parsedData,
           ref int parsedDataLen);

        [DllImport(DLL_PATH,
           BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi, EntryPoint = "PKIParseSessionID",
           ThrowOnUnmappableChar = true)]
        internal static extern int PKIParseSessionIDNative(
           byte[] request,
           int requestLen,
           ref IntPtr parsedData,
           ref int parsedDataLen);

        [DllImport(DLL_PATH,
           BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
           CharSet = CharSet.Ansi, EntryPoint = "PKIGenerateCertificateChain",
           ThrowOnUnmappableChar = true)]
        internal static extern int PKIGenerateCertificateChainNative(
           string issuerId,
           ref IntPtr certChain,
           ref int certChainLen,
           ref IntPtr issuerCert,
           ref int issuerCertLen);
    }
}
