using Microsoft.AspNetCore.Mvc;
using webapi.Models;
using webapi.Services;

namespace webapi.Controllers
{
    /// <summary>
    /// Padrão 3: Idempotência Version-Based (Optimistic Locking)
    /// Cada recurso tem versão, operações incluem versão esperada
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class VersionBasedController : ControllerBase
    {
        private readonly IVersionBasedIdempotencyService _service;
        private readonly ILogger<VersionBasedController> _logger;

        public VersionBasedController(
            IVersionBasedIdempotencyService service,
            ILogger<VersionBasedController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Listar todos os recursos
        /// </summary>
        [HttpGet("resources")]
        [ProducesResponseType(typeof(IEnumerable<ResourceVersion>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ResourceVersion>>> GetAllResources()
        {
            var resources = await _service.GetAllResourcesAsync();
            return Ok(resources);
        }

        /// <summary>
        /// Obter recurso por ID (com versão atual)
        /// </summary>
        [HttpGet("resources/{id}")]
        [ProducesResponseType(typeof(ResourceVersion), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResourceVersion>> GetResource(string id)
        {
            var resource = await _service.GetResourceAsync(id);
            
            if (resource == null)
                return NotFound(new { Message = $"Recurso {id} não encontrado" });

            return Ok(resource);
        }

        /// <summary>
        /// Atualizar recurso com Optimistic Locking
        /// </summary>
        /// <remarks>
        /// Optimistic Locking usando versões.
        /// 
        /// Como funciona:
        /// 1. Cliente lê o recurso e obtém a versão atual
        /// 2. Cliente envia atualização incluindo a versão esperada
        /// 3. Servidor verifica se a versão ainda é a mesma
        /// 4. Se diferente = conflito (alguém atualizou antes)
        /// 5. Se igual = atualização aceita e versão incrementada
        /// 
        /// Teste:
        /// 1. GET /api/versionbased/resources/config-1 (versão: 1)
        /// 2. PUT com expectedVersion: 1 - sucesso (versão vira 2)
        /// 3. PUT novamente com expectedVersion: 1 - conflito!
        /// 4. GET novamente (versão: 2)
        /// 5. PUT com expectedVersion: 2 - sucesso (versão vira 3)
        /// 
        /// Isso previne lost updates (atualizações perdidas).
        /// </remarks>
        [HttpPut("resources/{id}")]
        [ProducesResponseType(typeof(ResourceVersion), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(VersionConflictResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResourceVersion>> UpdateResource(
            string id,
            [FromBody] UpdateResourceRequest request)
        {
            _logger.LogInformation(
                "Atualizando recurso {Id} com versão esperada {ExpectedVersion}",
                id, request.ExpectedVersion);

            var (success, resource, error) = await _service.UpdateResourceAsync(id, request);

            if (!success)
            {
                var currentResource = await _service.GetResourceAsync(id);
                
                return Conflict(new VersionConflictResponse
                {
                    Message = error!,
                    CurrentVersion = currentResource?.Version ?? 0,
                    ExpectedVersion = request.ExpectedVersion
                });
            }

            return Ok(resource);
        }
    }
}

