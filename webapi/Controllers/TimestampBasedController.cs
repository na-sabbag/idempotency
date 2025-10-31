using Microsoft.AspNetCore.Mvc;
using webapi.Models;
using webapi.Services;

namespace webapi.Controllers
{
    /// <summary>
    /// Padrão 5: Desduplicação Timestamp-Based
    /// Ignora operações com timestamp anterior à última processada
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TimestampBasedController(
        ITimestampDeduplicationService service,
        ILogger<TimestampBasedController> logger) : ControllerBase
    {
        private readonly ITimestampDeduplicationService _service = service;
        private readonly ILogger<TimestampBasedController> _logger = logger;

        /// <summary>
        /// Listar todas as operações
        /// </summary>
        [HttpGet("operations")]
        [ProducesResponseType(typeof(IEnumerable<TimestampOperation>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<TimestampOperation>>> GetAllOperations()
        {
            var operations = await _service.GetAllOperationsAsync();
            return Ok(operations);
        }

        /// <summary>
        /// Obter operação por ID do recurso
        /// </summary>
        [HttpGet("operations/{resourceId}")]
        [ProducesResponseType(typeof(TimestampOperation), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TimestampOperation>> GetOperation(string resourceId)
        {
            var operation = await _service.GetOperationAsync(resourceId);
            
            if (operation == null)
                return NotFound(new { Message = $"Operação {resourceId} não encontrada" });

            return Ok(operation);
        }

        /// <summary>
        /// Atualizar sensor com timestamp
        /// </summary>
        /// <remarks>
        /// Timestamp-Based Deduplication - útil para sensores IoT.
        /// 
        /// Como funciona:
        /// 1. Cada operação inclui timestamp
        /// 2. Servidor armazena timestamp da última operação aceita
        /// 3. Operações com timestamp mais antigo são rejeitadas
        /// 4. Apenas operações mais recentes são processadas
        /// 
        /// ⚠️ CUIDADO: Problemas com Clock Skew!
        /// - Se relógios dos clientes não estão sincronizados
        /// - Pode rejeitar operações legítimas
        /// - Ou aceitar operações antigas
        /// 
        /// Teste:
        /// 1. POST com timestamp atual - aceito
        /// 2. POST com timestamp antigo - rejeitado
        /// 3. POST com timestamp futuro - aceito (mas cuidado!)
        /// 
        /// Caso de uso típico: Dados de sensores IoT
        /// </remarks>
        [HttpPost("sensor")]
        [ProducesResponseType(typeof(TimestampResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TimestampResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TimestampResponse>> UpdateSensor(
            [FromBody] TimestampRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ResourceId))
            {
                return BadRequest(new { Message = "ResourceId é obrigatório" });
            }

            _logger.LogInformation(
                "Processando operação para {ResourceId} com timestamp {Timestamp}",
                request.ResourceId, request.Timestamp);

            var (accepted, lastTimestamp) = await _service.ProcessOperationAsync(request);

            var response = new TimestampResponse
            {
                Accepted = accepted,
                ReceivedTimestamp = request.Timestamp,
                LastTimestamp = lastTimestamp
            };

            if (!accepted)
            {
                response.Message = $"Operação rejeitada: timestamp {request.Timestamp:O} " +
                    $"é anterior ao último processado {lastTimestamp:O}";
                
                return Conflict(response);
            }

            response.Message = "Operação aceita e processada";
            return Ok(response);
        }
    }
}

