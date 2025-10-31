using Microsoft.AspNetCore.Mvc;
using webapi.Models;
using webapi.Services;

namespace webapi.Controllers
{
    /// <summary>
    /// Padrão 2: Idempotência Natural
    /// PUT e DELETE são naturalmente idempotentes
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class NaturalIdempotencyController(
        INaturalIdempotencyService service,
        ILogger<NaturalIdempotencyController> logger) : ControllerBase
    {
        private readonly INaturalIdempotencyService _service = service;
        private readonly ILogger<NaturalIdempotencyController> _logger = logger;

        /// <summary>
        /// Listar todos os perfis
        /// </summary>
        [HttpGet("profiles")]
        [ProducesResponseType(typeof(IEnumerable<UserProfile>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserProfile>>> GetProfiles()
        {
            var profiles = await _service.GetAllProfilesAsync();
            return Ok(profiles);
        }

        /// <summary>
        /// Obter perfil por ID
        /// </summary>
        [HttpGet("profiles/{userId}")]
        [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfile>> GetProfile(string userId)
        {
            var profile = await _service.GetUserProfileAsync(userId);
            
            if (profile == null)
                return NotFound(new { Message = $"Perfil {userId} não encontrado" });

            return Ok(profile);
        }

        /// <summary>
        /// PUT - Idempotência Natural (substituição completa)
        /// </summary>
        /// <remarks>
        /// PUT é naturalmente idempotente porque sempre resulta no mesmo estado final.
        /// 
        /// Teste:
        /// 1. Faça PUT com os mesmos dados múltiplas vezes
        /// 2. O resultado será sempre o mesmo estado final
        /// 3. Não importa quantas vezes você execute
        /// 
        /// PUT substitui COMPLETAMENTE o recurso.
        /// </remarks>
        [HttpPut("profiles/{userId}")]
        [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserProfile>> UpdateProfile(
            string userId,
            [FromBody] UpdateUserProfileRequest request)
        {
            _logger.LogInformation("PUT idempotente: atualizando perfil {UserId}", userId);

            var profile = await _service.UpdateUserProfileAsync(userId, request);
            
            return Ok(profile);
        }

        /// <summary>
        /// DELETE - Idempotência Natural
        /// </summary>
        /// <remarks>
        /// DELETE é naturalmente idempotente porque deletar algo já deletado não muda nada.
        /// 
        /// Teste:
        /// 1. DELETE um recurso pela primeira vez - sucesso
        /// 2. DELETE o mesmo recurso novamente - ainda retorna sucesso
        /// 3. O estado final é o mesmo: recurso não existe
        /// 
        /// Retorna 204 No Content em ambos os casos (primeira vez ou repetido).
        /// </remarks>
        [HttpDelete("profiles/{userId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteProfile(string userId)
        {
            _logger.LogInformation("DELETE idempotente: removendo perfil {UserId}", userId);

            await _service.DeleteUserProfileAsync(userId);
            
            return NoContent();
        }

        /// <summary>
        /// POST - NÃO é idempotente (para comparação)
        /// </summary>
        /// <remarks>
        /// POST NÃO é idempotente - cada chamada cria um novo recurso.
        /// 
        /// Teste:
        /// 1. POST múltiplas vezes com os mesmos dados
        /// 2. Cada chamada cria um novo recurso com ID diferente
        /// 3. Esse é o comportamento esperado do POST
        /// </remarks>
        [HttpPost("profiles")]
        [ProducesResponseType(typeof(UserProfile), StatusCodes.Status201Created)]
        public async Task<ActionResult<UserProfile>> CreateProfile(
            [FromBody] UpdateUserProfileRequest request)
        {
            var userId = $"user-{Guid.NewGuid().ToString()[..8]}";
            
            _logger.LogInformation("POST (NÃO idempotente): criando novo perfil {UserId}", userId);

            var profile = await _service.UpdateUserProfileAsync(userId, request);
            
            return CreatedAtAction(nameof(GetProfile), new { userId }, profile);
        }
    }
}

