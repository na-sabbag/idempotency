using System.Collections.Concurrent;
using webapi.Models;

namespace webapi.Services
{
    /// <summary>
    /// Serviço para gerenciar Padrão 3: Idempotência Version-Based (Optimistic Locking)
    /// </summary>
    public interface IVersionBasedIdempotencyService
    {
        Task<ResourceVersion?> GetResourceAsync(string id);
        Task<(bool success, ResourceVersion? resource, string? error)> UpdateResourceAsync(
            string id, UpdateResourceRequest request);
        Task<IEnumerable<ResourceVersion>> GetAllResourcesAsync();
    }

    public class VersionBasedIdempotencyService : IVersionBasedIdempotencyService
    {
        private readonly ConcurrentDictionary<string, ResourceVersion> _resources = new();
        private readonly ILogger<VersionBasedIdempotencyService> _logger;

        public VersionBasedIdempotencyService(ILogger<VersionBasedIdempotencyService> logger)
        {
            _logger = logger;

            _resources["config-1"] = new ResourceVersion
            {
                Id = "config-1",
                Name = "AppTimeout",
                Value = "30",
                Version = 1,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public Task<ResourceVersion?> GetResourceAsync(string id)
        {
            _resources.TryGetValue(id, out var resource);
            return Task.FromResult(resource);
        }

        public Task<(bool success, ResourceVersion? resource, string? error)> UpdateResourceAsync(
            string id, UpdateResourceRequest request)
        {
            if (!_resources.TryGetValue(id, out var currentResource))
            {
                var newResource = new ResourceVersion
                {
                    Id = id,
                    Name = request.Name,
                    Value = request.Value,
                    Version = 1,
                    UpdatedAt = DateTime.UtcNow
                };

                _resources[id] = newResource;
                _logger.LogInformation("Novo recurso criado: {Id} v{Version}", id, 1);
                return Task.FromResult<(bool, ResourceVersion?, string?)>((true, newResource, null));
            }

            if (currentResource.Version != request.ExpectedVersion)
            {
                _logger.LogWarning(
                    "Conflito de versão para {Id}: esperada {Expected}, atual {Current}",
                    id, request.ExpectedVersion, currentResource.Version);

                return Task.FromResult<(bool, ResourceVersion?, string?)>((
                    false,
                    null,
                    $"Conflito de versão. Esperada: {request.ExpectedVersion}, Atual: {currentResource.Version}"
                ));
            }

            var updatedResource = new ResourceVersion
            {
                Id = id,
                Name = request.Name,
                Value = request.Value,
                Version = currentResource.Version + 1,
                UpdatedAt = DateTime.UtcNow
            };

            _resources[id] = updatedResource;
            _logger.LogInformation(
                "Recurso atualizado: {Id} v{OldVersion} -> v{NewVersion}",
                id, currentResource.Version, updatedResource.Version);

            return Task.FromResult<(bool, ResourceVersion?, string?)>((true, updatedResource, null));
        }

        public Task<IEnumerable<ResourceVersion>> GetAllResourcesAsync()
        {
            return Task.FromResult<IEnumerable<ResourceVersion>>(_resources.Values);
        }
    }
}

