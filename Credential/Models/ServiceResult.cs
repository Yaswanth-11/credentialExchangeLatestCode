namespace Credential.Models
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public object Result { get; set; }

        public ServiceResult(bool success, string message, int errorCode, string errorMessage, object result)
        {
            Success = success;
            Message = message;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Result = result;
        }
    }
}
