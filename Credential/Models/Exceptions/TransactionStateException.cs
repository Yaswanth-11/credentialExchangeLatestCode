namespace Credential.Models.Exceptions
{
    public enum TransactionStateErrorType
    {
        ExpiredOrInvalid,
        MissingPayload,
        DeserializationFailed,
        StorageFailed
    }

    public sealed class TransactionStateException : Exception
    {
        public string Key { get; }

        public string TransactionId { get; }

        public TransactionStateErrorType ErrorType { get; }

        public TransactionStateException(
            string message,
            string key,
            string transactionId,
            TransactionStateErrorType errorType,
            Exception? innerException = null)
            : base(message, innerException)
        {
            Key = key;
            TransactionId = transactionId;
            ErrorType = errorType;
        }

        public static TransactionStateException ExpiredOrInvalid(string key, string transactionId)
        {
            return new TransactionStateException(
                $"Transaction data was not found or has expired. TransactionId: {transactionId}, Key: {key}",
                key,
                transactionId,
                TransactionStateErrorType.ExpiredOrInvalid);
        }

        public static TransactionStateException MissingPayload(string key, string transactionId)
        {
            return new TransactionStateException(
                $"Transaction payload is missing or empty. TransactionId: {transactionId}, Key: {key}",
                key,
                transactionId,
                TransactionStateErrorType.MissingPayload);
        }

        public static TransactionStateException DeserializationFailed(string key, string transactionId, Exception? innerException = null)
        {
            return new TransactionStateException(
                $"Transaction payload could not be deserialized. TransactionId: {transactionId}, Key: {key}",
                key,
                transactionId,
                TransactionStateErrorType.DeserializationFailed,
                innerException);
        }

        public static TransactionStateException StorageFailed(string key, string transactionId)
        {
            return new TransactionStateException(
                $"Failed to persist transaction payload in Redis. TransactionId: {transactionId}, Key: {key}",
                key,
                transactionId,
                TransactionStateErrorType.StorageFailed);
        }
    }
}
