using Microsoft.AspNetCore.Mvc;
using webapi.Models;
using webapi.Services;

namespace webapi.Controllers
{
    /// <summary>
    /// Mock de Mensageria (Producer e Consumer)
    /// Demonstra desduplicação em sistemas de filas
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class MessageQueueController(
        MessageQueueService queueService,
        ILogger<MessageQueueController> logger) : ControllerBase
    {
        private readonly MessageQueueService _queueService = queueService;
        private readonly ILogger<MessageQueueController> _logger = logger;

        /// <summary>
        /// Publicar evento na fila (Producer)
        /// </summary>
        /// <remarks>
        /// Simula um Producer de mensageria.
        /// 
        /// O evento é adicionado à fila e será processado pelo Consumer em background.
        /// O Consumer usa Content-Based Deduplication para evitar processar duplicatas.
        /// 
        /// Teste:
        /// 1. POST um evento
        /// 2. POST o MESMO evento novamente (duplicata)
        /// 3. Verifique o histórico de processamento
        /// 4. O Consumer terá detectado e ignorado a duplicata
        /// 
        /// Isso simula cenários reais onde:
        /// - Mensagens podem ser entregues múltiplas vezes
        /// - Network retry pode causar duplicatas
        /// - Producer pode enviar duplicatas por erro
        /// </remarks>
        [HttpPost("publish")]
        [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
        public async Task<ActionResult> PublishEvent([FromBody] PublishEventRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EventType))
            {
                return BadRequest(new { Message = "EventType é obrigatório" });
            }

            var @event = new MessageQueueEvent
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = request.EventType,
                Payload = request.Payload,
                CreatedAt = DateTime.UtcNow
            };

            await _queueService.PublishEventAsync(@event);

            _logger.LogInformation(
                "Evento publicado: {EventId} - {EventType}",
                @event.EventId, @event.EventType);

            return AcceptedAtAction(nameof(GetProcessingHistory), new
            {
                Message = "Evento publicado e será processado pelo Consumer",
                @event.EventId,
                @event.EventType,
                Note = "O Consumer em background processará este evento com desduplicação"
            });
        }

        /// <summary>
        /// Obter histórico de processamento do Consumer
        /// </summary>
        /// <remarks>
        /// Mostra todos os eventos processados pelo Consumer, incluindo:
        /// - Eventos processados com sucesso
        /// - Eventos detectados como duplicatas
        /// - Timestamp de processamento
        /// </remarks>
        [HttpGet("processing-history")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetProcessingHistory()
        {
            var history = await _queueService.GetProcessingHistoryAsync();

            var summary = new
            {
                TotalProcessed = history.Count(),
                Successful = history.Count(h => h.Success && !h.WasDuplicate),
                Duplicates = history.Count(h => h.WasDuplicate),
                Failed = history.Count(h => !h.Success && !h.WasDuplicate)
            };

            return Ok(new
            {
                Summary = summary,
                History = history.OrderByDescending(h => h.ProcessedAt).Take(50)
            });
        }
    }
}

