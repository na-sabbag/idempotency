using System.Collections.Concurrent;
using webapi.Models;

namespace webapi.Services
{
    /// <summary>
    /// Serviço para gerenciar Padrão 1: Chave de Idempotência
    /// </summary>
    public interface IIdempotencyKeyService
    {
        Task<(bool exists, IdempotencyKey? result)> CheckKeyAsync(string key);
        Task<IdempotencyKey> StoreResultAsync(string key, string result, int statusCode);
    }

    public class IdempotencyKeyService : IIdempotencyKeyService
    {
        private readonly ConcurrentDictionary<string, IdempotencyKey> _cache = new();
        private readonly ILogger<IdempotencyKeyService> _logger;

        public IdempotencyKeyService(ILogger<IdempotencyKeyService> logger)
        {
            _logger = logger;
        }

        public Task<(bool exists, IdempotencyKey? result)> CheckKeyAsync(string key)
        {
            if (_cache.TryGetValue(key, out var result))
            {
                _logger.LogInformation("Chave de idempotência encontrada: {Key}", key);
                return Task.FromResult<(bool, IdempotencyKey?)>((true, result));
            }

            _logger.LogInformation("Chave de idempotência não encontrada: {Key}", key);
            return Task.FromResult<(bool, IdempotencyKey?)>((false, null));
        }

        public Task<IdempotencyKey> StoreResultAsync(string key, string result, int statusCode)
        {
            var idempotencyKey = new IdempotencyKey
            {
                Key = key,
                Result = result,
                StatusCode = statusCode,
                CreatedAt = DateTime.UtcNow
            };

            _cache[key] = idempotencyKey;
            _logger.LogInformation("Resultado armazenado para chave: {Key}", key);

            return Task.FromResult(idempotencyKey);
        }
    }
}

