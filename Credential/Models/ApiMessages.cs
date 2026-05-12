namespace Credential.Models
{
    public static class ApiMessages
    {
        public const string AuthorizationHeaderMissing =
            "Authorization header missing";

        public const string InvalidAuthorizationScheme =
            "Invalid authorization scheme";

        public const string AccessTokenMissing =
            "Access token missing";

        public const string TokenValidationFailed =
            "Token validation failed";

        public const string AccessTokenInactive =
            "Access token inactive";

        public const string ChecksumHeaderMissing =
            "Checksum header missing.";

        public const string RequestBodyEmpty =
            "Request body empty.";

        public const string TimestampOrNonceMissing =
            "Timestamp or nonce missing.";

        public const string InvalidTimestamp =
            "Invalid timestamp.";

        public const string RequestExpired =
            "Request expired.";

        public const string InvalidJsonPayload =
            "Invalid JSON payload.";

        public const string RequestIntegrityFailed =
            "Request integrity validation failed.";

        public const string OperationFailed =
            "Operation failed";
    }
}
