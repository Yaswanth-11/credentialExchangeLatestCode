using System;
using System.Runtime.InteropServices;


namespace Lux.Infrastructure.Native
{
    internal static class NativeMethods
    {
        private const string DLL_PATH = "CMAPI_DLL.dll";


        [DllImport(DLL_PATH,
            BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, EntryPoint = "CSRgen",
            ThrowOnUnmappableChar = true)]
        internal static extern int csrGen([MarshalAs(UnmanagedType.LPStr)] string keyId, int keyIdLen, ref IntPtr csr, ref int csrLen);

        //private const string DLL_PATH = "PKIService.dll";


        //[DllImport(DLL_PATH,
        //    BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
        //    CharSet = CharSet.Ansi, EntryPoint = "InitializePKI",
        //    ThrowOnUnmappableChar = true)]
        //internal static extern int InitializePKINative(
        //    [MarshalAs(UnmanagedType.LPStr)] string configParams);

        //[DllImport(DLL_PATH,
        //    BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
        //    CharSet = CharSet.Ansi, EntryPoint = "SetPIN",
        //    ThrowOnUnmappableChar = true)]
        //internal static extern int SetPINNative(
        //    [MarshalAs(UnmanagedType.LPStr)] string setPINRequest,
        //    ref IntPtr response,
        //    ref int responseLength);

        //[DllImport(DLL_PATH,
        //   BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
        //   CharSet = CharSet.Ansi, EntryPoint = "SignPADES",
        //   ThrowOnUnmappableChar = true)]
        //internal static extern int SignPADESNative(
        //   [MarshalAs(UnmanagedType.LPStr)] string signRequest,
        //   ref IntPtr response,
        //   ref int responseLength);

        //[DllImport(DLL_PATH,
        //    BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
        //    CharSet = CharSet.Ansi, EntryPoint = "CleanupPKI",
        //    ThrowOnUnmappableChar = true)]
        //internal static extern int CleanupPKINative();

        //[DllImport(DLL_PATH,
        //    BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
        //    CharSet = CharSet.Ansi, EntryPoint = "GetStatusMessagePKI",
        //    ThrowOnUnmappableChar = true)]
        //internal static extern IntPtr GetStatusMessagePKINative(int errorCode);

        //[DllImport(DLL_PATH,
        //    BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
        //    CharSet = CharSet.Ansi, EntryPoint = "FreeMemoryPKI",
        //    ThrowOnUnmappableChar = true)]
        //internal static extern int FreeMemoryPKINative(
        //    IntPtr buffer);

        //[DllImport(DLL_PATH,
        //    BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
        //    CharSet = CharSet.Ansi, EntryPoint = "GenerateTimestampEx",
        //    ThrowOnUnmappableChar = true)]
        //internal static extern int GenerateTimestampNative(
        //    [MarshalAs(UnmanagedType.LPArray)] byte[] timestampRequest,
        //    int timestampRequestLength,
        //    ref IntPtr response,
        //    ref int responseLength);

        //[DllImport(DLL_PATH,
        //    BestFitMapping = false, CallingConvention = CallingConvention.Cdecl,
        //    CharSet = CharSet.Ansi, EntryPoint = "GenerateTimestamp",
        //    ThrowOnUnmappableChar = true)]
        //internal static extern int POSDigiTimeStampNative(
        //    [MarshalAs(UnmanagedType.LPArray)] byte[] timestampRequest,
        //    int timestampRequestLength,
        //    ref IntPtr response,
        //    ref int responseLength);
    }
}
