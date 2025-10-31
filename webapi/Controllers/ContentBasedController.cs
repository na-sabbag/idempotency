using Microsoft.AspNetCore.Mvc;
using webapi.Models;
using webapi.Services;

namespace webapi.Controllers
{
    /// <summary>
    /// Padrão 6: Desduplicação Content-Based
    /// Hash do conteúdo da operação para detectar duplicatas
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ContentBasedController(
        IContentBasedDeduplicationService service,
        ILogger<ContentBasedController> logger) : ControllerBase
    {
        private readonly IContentBasedDeduplicationService _service = service;
        private readonly ILogger<ContentBasedController> _logger = logger;

        /// <summary>
        /// Listar todas as operações processadas
        /// </summary>
        [HttpGet("operations")]
        [ProducesResponseType(typeof(IEnumerable<ContentBasedOperation>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ContentBasedOperation>>> GetAllOperations()
        {
            var operations = await _service.GetAllOperationsAsync();
            return Ok(new
            {
                Count = operations.Count(),
                Operations = operations
            });
        }

        /// <summary>
        /// Processar evento com desduplicação por conteúdo
        /// </summary>
        /// <remarks>
        /// Content-Based Deduplication - ideal para Mensageria e Eventos.
        /// 
        /// Como funciona:
        /// 1. Calcula hash (SHA-256) do conteúdo completo
        /// 2. Verifica se já processou esse hash
        /// 3. Se sim = duplicata, ignora
        /// 4. Se não = processa e armazena o hash
        /// 
        /// Vantagens:
        /// - Não precisa de chave gerada pelo cliente
        /// - Não depende de timestamps
        /// - Detecta duplicatas exatas automaticamente
        /// 
        /// Caso de uso típico: Message Queues, Event Sourcing
        /// 
        /// Teste:
        /// 1. POST com um evento - processado
        /// 2. POST com EXATAMENTE o mesmo conteúdo - rejeitado (duplicata)
        /// 3. POST com conteúdo diferente - processado
        /// 4. Mesmo mudando apenas 1 caractere, o hash muda
        /// </remarks>
        [HttpPost("event")]
        [ProducesResponseType(typeof(ContentBasedResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ContentBasedResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ContentBasedResponse>> ProcessEvent(
            [FromBody] ContentBasedRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EventType))
            {
                return BadRequest(new { Message = "EventType é obrigatório" });
            }

            var contentHash = _service.ComputeContentHash(request);

            _logger.LogInformation(
                "Processando evento {EventType} com hash {Hash}",
                request.EventType, contentHash);

            var (isDuplicate, existingOperation) = await _service.ProcessOperationAsync(request);

            var response = new ContentBasedResponse
            {
                ContentHash = contentHash,
                IsDuplicate = isDuplicate
            };

            if (isDuplicate)
            {
                response.Processed = false;
                response.Message = "Evento duplicado detectado e ignorado";
                response.FirstProcessedAt = existingOperation!.ProcessedAt;

                _logger.LogWarning(
                    "Evento duplicado: {EventType} - Já processado em {FirstProcessed}",
                    request.EventType, existingOperation.ProcessedAt);

                return Conflict(response);
            }

            response.Processed = true;
            response.Message = "Evento processado com sucesso";
            response.FirstProcessedAt = DateTime.UtcNow;

            return Ok(response);
        }

        /// <summary>
        /// Calcular hash de um conteúdo (endpoint utilitário)
        /// </summary>
        /// <remarks>
        /// Endpoint educacional para entender como o hash funciona.
        /// </remarks>
        [HttpPost("compute-hash")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public ActionResult ComputeHash([FromBody] object content)
        {
            var hash = _service.ComputeContentHash(content);

            return Ok(new
            {
                Hash = hash,
                Algorithm = "SHA-256",
                Content = content,
                Explanation = "Mesmo conteúdo sempre gera o mesmo hash. " +
                    "Mudança mínima no conteúdo resulta em hash completamente diferente."
            });
        }
    }
}

