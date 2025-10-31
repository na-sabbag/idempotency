using System.Collections.Concurrent;
using webapi.Models;

namespace webapi.Services
{
    /// <summary>
    /// Serviço para gerenciar Padrão 5: Desduplicação Timestamp-Based
    /// Ignora operações com timestamp anterior
    /// CUIDADO: Problemas com clock skew!
    /// </summary>
    public interface ITimestampDeduplicationService
    {
        Task<(bool accepted, DateTime? lastTimestamp)> ProcessOperationAsync(TimestampRequest request);
        Task<TimestampOperation?> GetOperationAsync(string resourceId);
        Task<IEnumerable<TimestampOperation>> GetAllOperationsAsync();
    }

    public class TimestampDeduplicationService : ITimestampDeduplicationService
    {
        private readonly ConcurrentDictionary<string, TimestampOperation> _operations = new();
        private readonly ILogger<TimestampDeduplicationService> _logger;

        public TimestampDeduplicationService(ILogger<TimestampDeduplicationService> logger)
        {
            _logger = logger;

            _operations["sensor-1"] = new TimestampOperation
            {
                ResourceId = "sensor-1",
                Value = "25.5",
                LastUpdated = DateTime.UtcNow.AddMinutes(-5)
            };
        }

        public Task<(bool accepted, DateTime? lastTimestamp)> ProcessOperationAsync(TimestampRequest request)
        {
            if (!_operations.TryGetValue(request.ResourceId, out var existingOperation))
            {
                var newOperation = new TimestampOperation
                {
                    ResourceId = request.ResourceId,
                    Value = request.Value,
                    LastUpdated = request.Timestamp
                };

                _operations[request.ResourceId] = newOperation;
                
                _logger.LogInformation(
                    "Nova operação aceita: {ResourceId} @ {Timestamp}",
                    request.ResourceId, request.Timestamp);

                return Task.FromResult((true, (DateTime?)null));
            }

            if (request.Timestamp <= existingOperation.LastUpdated)
            {
                _logger.LogWarning(
                    "Operação rejeitada (timestamp antigo): {ResourceId} - Recebido: {Received}, Último: {Last}",
                    request.ResourceId, request.Timestamp, existingOperation.LastUpdated);

                return Task.FromResult((false, (DateTime?)existingOperation.LastUpdated));
            }

            var updatedOperation = new TimestampOperation
            {
                ResourceId = request.ResourceId,
                Value = request.Value,
                LastUpdated = request.Timestamp
            };

            _operations[request.ResourceId] = updatedOperation;

            _logger.LogInformation(
                "Operação atualizada: {ResourceId} @ {Timestamp}",
                request.ResourceId, request.Timestamp);

            return Task.FromResult((true, (DateTime?)existingOperation.LastUpdated));
        }

        public Task<TimestampOperation?> GetOperationAsync(string resourceId)
        {
            _operations.TryGetValue(resourceId, out var operation);
            return Task.FromResult(operation);
        }

        public Task<IEnumerable<TimestampOperation>> GetAllOperationsAsync()
        {
            return Task.FromResult<IEnumerable<TimestampOperation>>(_operations.Values);
        }
    }
}

