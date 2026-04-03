using Credential.Models.Exceptions;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Credential.RedisDB
{
    public class RedisTransactionStore : IRedisTransactionStore
    {
        private const string ConsumeScript = @"
            local value = redis.call('GET', KEYS[1])
            if value then
                redis.call('DEL', KEYS[1])
            end
            return value
        ";

        private readonly IDatabase _database;
        private readonly ILogger<RedisTransactionStore> _logger;
        private readonly TimeSpan _defaultTtl;

        public RedisTransactionStore(
            IConnectionMultiplexer redisConnection,
            IConfiguration configuration,
            ILogger<RedisTransactionStore> logger)
        {
            _database = redisConnection.GetDatabase();
            _logger = logger;

            var configuredMinutes = configuration.GetValue<int?>("TransactionDataTtlMinutes");
            var ttlMinutes = configuredMinutes.GetValueOrDefault(5);
            if (ttlMinutes <= 0)
            {
                ttlMinutes = 5;
            }

            _defaultTtl = TimeSpan.FromMinutes(ttlMinutes);
        }

        public async Task StoreStringAsync(string key, string transactionId, string payload, string dataType, TimeSpan? ttl = null)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                _logger.LogError(
                    "Redis payload was empty. DataType={DataType} TransactionId={TransactionId} Key={Key}",
                    dataType,
                    transactionId,
                    key);
                throw TransactionStateException.MissingPayload(key, transactionId);
            }

            var effectiveTtl = ttl ?? _defaultTtl;
            var isSet = await _database.StringSetAsync(key, payload, expiry: effectiveTtl, when: When.Always);
            if (!isSet)
            {
                _logger.LogError(
                    "Redis write failed. DataType={DataType} TransactionId={TransactionId} Key={Key} TTL={Ttl}",
                    dataType,
                    transactionId,
                    key,
                    effectiveTtl);
                throw TransactionStateException.StorageFailed(key, transactionId);
            }

            _logger.LogInformation(
                "Redis write succeeded. DataType={DataType} TransactionId={TransactionId} Key={Key} TTL={Ttl}",
                dataType,
                transactionId,
                key,
                effectiveTtl);
        }

        public async Task EnsureExistsAndLogTtlAsync(string key, string transactionId, string dataType)
        {
            var exists = await _database.KeyExistsAsync(key);
            if (!exists)
            {
                _logger.LogWarning(
                    "Redis key missing or expired. DataType={DataType} TransactionId={TransactionId} Key={Key}",
                    dataType,
                    transactionId,
                    key);
                throw TransactionStateException.ExpiredOrInvalid(key, transactionId);
            }

            var ttl = await _database.KeyTimeToLiveAsync(key);
            if (ttl.HasValue)
            {
                _logger.LogInformation(
                    "Redis key exists with TTL. DataType={DataType} TransactionId={TransactionId} Key={Key} TTL={Ttl}",
                    dataType,
                    transactionId,
                    key,
                    ttl.Value);
            }
            else
            {
                _logger.LogWarning(
                    "Redis key exists without TTL. DataType={DataType} TransactionId={TransactionId} Key={Key}",
                    dataType,
                    transactionId,
                    key);
            }
        }

        public async Task<string> GetRequiredStringAsync(string key, string transactionId, string dataType)
        {
            await EnsureExistsAndLogTtlAsync(key, transactionId, dataType);

            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                _logger.LogError(
                    "Redis payload missing after key existence check. DataType={DataType} TransactionId={TransactionId} Key={Key}",
                    dataType,
                    transactionId,
                    key);
                throw TransactionStateException.MissingPayload(key, transactionId);
            }

            _logger.LogInformation(
                "Redis read succeeded. DataType={DataType} TransactionId={TransactionId} Key={Key}",
                dataType,
                transactionId,
                key);

            return value!;
        }

        public async Task<T> GetRequiredObjectAsync<T>(string key, string transactionId, string dataType) where T : class
        {
            var payload = await GetRequiredStringAsync(key, transactionId, dataType);

            try
            {
                var deserialized = JsonConvert.DeserializeObject<T>(payload);
                if (deserialized == null)
                {
                    _logger.LogError(
                        "Redis deserialization returned null. DataType={DataType} TransactionId={TransactionId} Key={Key} TargetType={TargetType}",
                        dataType,
                        transactionId,
                        key,
                        typeof(T).Name);
                    throw TransactionStateException.DeserializationFailed(key, transactionId);
                }

                _logger.LogInformation(
                    "Redis deserialization succeeded. DataType={DataType} TransactionId={TransactionId} Key={Key} TargetType={TargetType}",
                    dataType,
                    transactionId,
                    key,
                    typeof(T).Name);

                return deserialized;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis deserialization failed. DataType={DataType} TransactionId={TransactionId} Key={Key} TargetType={TargetType}",
                    dataType,
                    transactionId,
                    key,
                    typeof(T).Name);
                throw TransactionStateException.DeserializationFailed(key, transactionId, ex);
            }
        }

        public async Task<string> ConsumeRequiredStringAsync(string key, string transactionId, string dataType)
        {
            await EnsureExistsAndLogTtlAsync(key, transactionId, dataType);

            var result = await _database.ScriptEvaluateAsync(
                ConsumeScript,
                new RedisKey[] { key },
                Array.Empty<RedisValue>());

            if (result.IsNull)
            {
                _logger.LogWarning(
                    "Redis consume found no payload; key likely consumed by another node. DataType={DataType} TransactionId={TransactionId} Key={Key}",
                    dataType,
                    transactionId,
                    key);
                throw TransactionStateException.ExpiredOrInvalid(key, transactionId);
            }

            var payload = (string)result!;
            if (string.IsNullOrWhiteSpace(payload))
            {
                _logger.LogError(
                    "Redis consume returned empty payload. DataType={DataType} TransactionId={TransactionId} Key={Key}",
                    dataType,
                    transactionId,
                    key);
                throw TransactionStateException.MissingPayload(key, transactionId);
            }

            _logger.LogInformation(
                "Redis consume succeeded and key invalidated. DataType={DataType} TransactionId={TransactionId} Key={Key}",
                dataType,
                transactionId,
                key);

            return payload;
        }

        public async Task DeleteIfExistsAsync(string key, string transactionId, string dataType)
        {
            var deleted = await _database.KeyDeleteAsync(key);
            if (deleted)
            {
                _logger.LogInformation(
                    "Redis key deleted. DataType={DataType} TransactionId={TransactionId} Key={Key}",
                    dataType,
                    transactionId,
                    key);
            }
            else
            {
                _logger.LogInformation(
                    "Redis key already absent during delete. DataType={DataType} TransactionId={TransactionId} Key={Key}",
                    dataType,
                    transactionId,
                    key);
            }
        }
    }
}
