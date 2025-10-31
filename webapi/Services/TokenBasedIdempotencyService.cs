using System.Collections.Concurrent;
using webapi.Models;

namespace webapi.Services
{
    /// <summary>
    /// Serviço para gerenciar Padrão 4: Idempotência Token-Based
    /// Token único gerado pelo servidor, usado uma única vez
    /// </summary>
    public interface ITokenBasedIdempotencyService
    {
        Task<IdempotencyToken> GenerateTokenAsync();
        Task<(bool valid, string? error)> ValidateAndConsumeTokenAsync(string token);
    }

    public class TokenBasedIdempotencyService : ITokenBasedIdempotencyService
    {
        private readonly ConcurrentDictionary<string, IdempotencyToken> _tokens = new();
        private readonly ILogger<TokenBasedIdempotencyService> _logger;
        private readonly TimeSpan _tokenExpiration = TimeSpan.FromMinutes(15);

        public TokenBasedIdempotencyService(ILogger<TokenBasedIdempotencyService> logger)
        {
            _logger = logger;
        }

        public Task<IdempotencyToken> GenerateTokenAsync()
        {
            var token = new IdempotencyToken
            {
                Token = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_tokenExpiration),
                IsUsed = false
            };

            _tokens[token.Token] = token;
            _logger.LogInformation("Token gerado: {Token}", token.Token);

            CleanupExpiredTokens();

            return Task.FromResult(token);
        }

        public Task<(bool valid, string? error)> ValidateAndConsumeTokenAsync(string token)
        {
            if (!_tokens.TryGetValue(token, out var tokenData))
            {
                _logger.LogWarning("Token não encontrado: {Token}", token);
                return Task.FromResult<(bool, string?)>((false, "Token inválido ou não encontrado"));
            }

            if (tokenData.IsUsed)
            {
                _logger.LogWarning("Token já foi usado: {Token}", token);
                return Task.FromResult<(bool, string?)>((false, "Token já foi utilizado"));
            }

            if (DateTime.UtcNow > tokenData.ExpiresAt)
            {
                _logger.LogWarning("Token expirado: {Token}", token);
                return Task.FromResult<(bool, string?)>((false, "Token expirado"));
            }

            tokenData.IsUsed = true;
            _logger.LogInformation("Token consumido: {Token}", token);

            return Task.FromResult<(bool, string?)>((true, null));
        }

        private void CleanupExpiredTokens()
        {
            var expiredTokens = _tokens
                .Where(kvp => DateTime.UtcNow > kvp.Value.ExpiresAt.AddHours(1))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in expiredTokens)
            {
                _tokens.TryRemove(token, out _);
            }

            if (expiredTokens.Any())
            {
                _logger.LogInformation("Tokens expirados removidos: {Count}", expiredTokens.Count);
            }
        }
    }
}

