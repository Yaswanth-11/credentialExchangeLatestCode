namespace Credential.Models
{
    public class CredentialResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public object Result { get; set; }
    }
}
