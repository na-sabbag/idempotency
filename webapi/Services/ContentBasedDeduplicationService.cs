using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using webapi.Models;

namespace webapi.Services
{
    /// <summary>
    /// Serviço para gerenciar Padrão 6: Desduplicação Content-Based
    /// Hash do conteúdo para detectar duplicatas
    /// Ideal para Mensageria e Eventos
    /// </summary>
    public interface IContentBasedDeduplicationService
    {
        Task<(bool isDuplicate, ContentBasedOperation? existingOperation)> ProcessOperationAsync(
            ContentBasedRequest request);
        Task<IEnumerable<ContentBasedOperation>> GetAllOperationsAsync();
        string ComputeContentHash(object content);
    }

    public class ContentBasedDeduplicationService : IContentBasedDeduplicationService
    {
        private readonly ConcurrentDictionary<string, ContentBasedOperation> _operations = new();
        private readonly ILogger<ContentBasedDeduplicationService> _logger;

        public ContentBasedDeduplicationService(ILogger<ContentBasedDeduplicationService> logger)
        {
            _logger = logger;
        }

        public Task<(bool isDuplicate, ContentBasedOperation? existingOperation)> ProcessOperationAsync(
            ContentBasedRequest request)
        {
            var contentHash = ComputeContentHash(request);

            if (_operations.TryGetValue(contentHash, out var existingOperation))
            {
                _logger.LogWarning(
                    "Operação duplicada detectada! Hash: {Hash}, Primeira vez: {FirstProcessed}",
                    contentHash, existingOperation.ProcessedAt);

                return Task.FromResult<(bool, ContentBasedOperation?)>((true, existingOperation));
            }

            var newOperation = new ContentBasedOperation
            {
                Hash = contentHash,
                Content = JsonSerializer.Serialize(request),
                ProcessedAt = DateTime.UtcNow
            };

            _operations[contentHash] = newOperation;

            _logger.LogInformation(
                "Nova operação processada: {Hash} - {EventType}",
                contentHash, request.EventType);

            return Task.FromResult<(bool, ContentBasedOperation?)>((false, null));
        }

        public string ComputeContentHash(object content)
        {
            var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(json);
            var hashBytes = sha256.ComputeHash(bytes);

            return Convert.ToHexString(hashBytes).ToLower();
        }

        public Task<IEnumerable<ContentBasedOperation>> GetAllOperationsAsync()
        {
            return Task.FromResult<IEnumerable<ContentBasedOperation>>(_operations.Values);
        }
    }
}

