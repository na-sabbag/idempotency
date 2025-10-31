using Microsoft.AspNetCore.Mvc;
using webapi.Models;
using webapi.Services;

namespace webapi.Controllers
{
    /// <summary>
    /// Padrão 1: Chave de Idempotência
    /// Cliente gera identificador único, servidor armazena e verifica
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class IdempotencyKeyController(
        IIdempotencyKeyService service,
        ILogger<IdempotencyKeyController> logger) : ControllerBase
    {
        private readonly IIdempotencyKeyService _service = service;
        private readonly ILogger<IdempotencyKeyController> _logger = logger;

        /// <summary>
        /// Criar pedido com chave de idempotência
        /// </summary>
        /// <remarks>
        /// Envie a mesma chave múltiplas vezes e receberá o mesmo resultado.
        /// 
        /// Exemplo de chave: UUID ou GUID gerado pelo cliente
        /// 
        /// Teste:
        /// 1. Faça uma requisição com uma chave
        /// 2. Faça novamente com a MESMA chave - receberá o resultado em cache
        /// 3. Faça com uma chave diferente - processará normalmente
        /// </remarks>
        [HttpPost("order")]
        [ProducesResponseType(typeof(IdempotencyResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IdempotencyResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<IdempotencyResponse>> CreateOrder(
            [FromBody] IdempotencyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return BadRequest(new { Message = "IdempotencyKey é obrigatória" });
            }

            var (exists, cachedResult) = await _service.CheckKeyAsync(request.IdempotencyKey);

            if (exists)
            {
                _logger.LogInformation(
                    "Retornando resultado em cache para chave: {Key}",
                    request.IdempotencyKey);

                var cachedResponse = System.Text.Json.JsonSerializer
                    .Deserialize<IdempotencyResponse>(cachedResult!.Result);

                return StatusCode(cachedResult.StatusCode, cachedResponse);
            }

            var response = new IdempotencyResponse
            {
                Message = "Pedido criado com sucesso",
                ProcessId = Guid.NewGuid().ToString(),
                IsFromCache = false,
                Timestamp = DateTime.UtcNow
            };

            await Task.Delay(100);

            var resultJson = System.Text.Json.JsonSerializer.Serialize(response);
            await _service.StoreResultAsync(request.IdempotencyKey, resultJson, StatusCodes.Status201Created);

            return CreatedAtAction(nameof(CreateOrder), response);
        }

        /// <summary>
        /// Exemplo de como usar em uma operação de pagamento
        /// </summary>
        [HttpPost("payment")]
        [ProducesResponseType(typeof(IdempotencyResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IdempotencyResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<IdempotencyResponse>> ProcessPayment(
            [FromBody] IdempotencyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return BadRequest(new { Message = "IdempotencyKey é obrigatória" });
            }

            var (exists, cachedResult) = await _service.CheckKeyAsync(request.IdempotencyKey);

            if (exists)
            {
                var cachedResponse = System.Text.Json.JsonSerializer
                    .Deserialize<IdempotencyResponse>(cachedResult!.Result);
                
                cachedResponse!.IsFromCache = true;
                
                return StatusCode(cachedResult.StatusCode, cachedResponse);
            }

            await Task.Delay(200);

            var response = new IdempotencyResponse
            {
                Message = "Pagamento processado com sucesso",
                ProcessId = $"PAY-{Guid.NewGuid().ToString()[..8]}",
                IsFromCache = false,
                Timestamp = DateTime.UtcNow
            };

            var resultJson = System.Text.Json.JsonSerializer.Serialize(response);
            await _service.StoreResultAsync(request.IdempotencyKey, resultJson, StatusCodes.Status201Created);

            return CreatedAtAction(nameof(ProcessPayment), response);
        }
    }
}

