using System.Collections.Concurrent;
using webapi.Models;

namespace webapi.Services
{
    /// <summary>
    /// Serviço para gerenciar Padrão 2: Idempotência Natural
    /// PUT e DELETE são naturalmente idempotentes
    /// </summary>
    public interface INaturalIdempotencyService
    {
        Task<UserProfile?> GetUserProfileAsync(string userId);
        Task<UserProfile> UpdateUserProfileAsync(string userId, UpdateUserProfileRequest request);
        Task<bool> DeleteUserProfileAsync(string userId);
        Task<IEnumerable<UserProfile>> GetAllProfilesAsync();
    }

    public class NaturalIdempotencyService : INaturalIdempotencyService
    {
        private readonly ConcurrentDictionary<string, UserProfile> _profiles = new();
        private readonly ILogger<NaturalIdempotencyService> _logger;

        public NaturalIdempotencyService(ILogger<NaturalIdempotencyService> logger)
        {
            _logger = logger;
            
            _profiles["user-1"] = new UserProfile
            {
                Id = "user-1",
                Name = "João Silva",
                Email = "joao@example.com",
                Bio = "Desenvolvedor",
                UpdatedAt = DateTime.UtcNow
            };
        }

        public Task<UserProfile?> GetUserProfileAsync(string userId)
        {
            _profiles.TryGetValue(userId, out var profile);
            return Task.FromResult(profile);
        }

        public Task<UserProfile> UpdateUserProfileAsync(string userId, UpdateUserProfileRequest request)
        {
            var profile = new UserProfile
            {
                Id = userId,
                Name = request.Name,
                Email = request.Email,
                Bio = request.Bio,
                UpdatedAt = DateTime.UtcNow
            };

            _profiles[userId] = profile;
            _logger.LogInformation("Perfil atualizado (idempotente): {UserId}", userId);

            return Task.FromResult(profile);
        }

        public Task<bool> DeleteUserProfileAsync(string userId)
        {
            var removed = _profiles.TryRemove(userId, out _);
            
            if (removed)
                _logger.LogInformation("Perfil deletado: {UserId}", userId);
            else
                _logger.LogInformation("Perfil já estava deletado: {UserId}", userId);

            return Task.FromResult(true);
        }

        public Task<IEnumerable<UserProfile>> GetAllProfilesAsync()
        {
            return Task.FromResult<IEnumerable<UserProfile>>(_profiles.Values);
        }
    }
}

