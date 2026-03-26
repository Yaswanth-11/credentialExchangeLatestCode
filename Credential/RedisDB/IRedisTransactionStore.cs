namespace Credential.RedisDB
{
    public interface IRedisTransactionStore
    {
        Task StoreStringAsync(string key, string transactionId, string payload, string dataType, TimeSpan? ttl = null);

        Task EnsureExistsAndLogTtlAsync(string key, string transactionId, string dataType);

        Task<string> GetRequiredStringAsync(string key, string transactionId, string dataType);

        Task<T> GetRequiredObjectAsync<T>(string key, string transactionId, string dataType) where T : class;

        Task<string> ConsumeRequiredStringAsync(string key, string transactionId, string dataType);

        Task DeleteIfExistsAsync(string key, string transactionId, string dataType);
    }
}
