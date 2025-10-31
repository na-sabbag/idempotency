using Microsoft.AspNetCore.Mvc;
using webapi.Models;
using webapi.Services;

namespace webapi.Controllers
{
    /// <summary>
    /// Padrão 4: Idempotência Token-Based
    /// Servidor gera token único que só pode ser usado uma vez
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TokenBasedController(
        ITokenBasedIdempotencyService service,
        ILogger<TokenBasedController> logger) : ControllerBase
    {
        private readonly ITokenBasedIdempotencyService _service = service;
        private readonly ILogger<TokenBasedController> _logger = logger;

        /// <summary>
        /// Gerar token de idempotência
        /// </summary>
        /// <remarks>
        /// Similar a CSRF tokens, muito usado em formulários de pagamento.
        /// 
        /// Fluxo:
        /// 1. Cliente solicita um token
        /// 2. Servidor gera token único com validade
        /// 3. Cliente usa o token em UMA operação
        /// 4. Token é consumido e não pode ser reutilizado
        /// 
        /// Caso de uso típico: Formulários de pagamento
        /// - Previne dupla submissão
        /// - Token expira em 15 minutos
        /// - Token só pode ser usado uma vez
        /// </remarks>
        [HttpPost("token/generate")]
        [ProducesResponseType(typeof(TokenGenerationResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TokenGenerationResponse>> GenerateToken()
        {
            var token = await _service.GenerateTokenAsync();

            var response = new TokenGenerationResponse
            {
                Token = token.Token,
                ExpiresAt = token.ExpiresAt,
                Message = "Token gerado com sucesso. Use-o uma única vez em até 15 minutos."
            };

            return Ok(response);
        }

        /// <summary>
        /// Processar pagamento usando token
        /// </summary>
        /// <remarks>
        /// Teste:
        /// 1. POST /api/tokenbased/token/generate - obtenha um token
        /// 2. POST /api/tokenbased/payment com o token - sucesso
        /// 3. POST /api/tokenbased/payment com o MESMO token - erro (já usado)
        /// 4. Gere um novo token e repita
        /// 
        /// Isso previne dupla submissão de pagamentos!
        /// </remarks>
        [HttpPost("payment")]
        [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaymentResponse>> ProcessPayment(
            [FromBody] TokenBasedRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { Message = "Token é obrigatório" });
            }

            var (valid, error) = await _service.ValidateAndConsumeTokenAsync(request.Token);

            if (!valid)
            {
                _logger.LogWarning("Token inválido ou já usado: {Token}", request.Token);
                return BadRequest(new { Message = error });
            }

            _logger.LogInformation(
                "Processando pagamento de {Amount:C} com token {Token}",
                request.Amount, request.Token);

            await Task.Delay(150);

            var response = new PaymentResponse
            {
                TransactionId = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                Message = "Pagamento processado com sucesso",
                Amount = request.Amount,
                ProcessedAt = DateTime.UtcNow
            };

            return Ok(response);
        }

        /// <summary>
        /// Processar pedido usando token (exemplo alternativo)
        /// </summary>
        [HttpPost("order")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ProcessOrder([FromBody] TokenBasedRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { Message = "Token é obrigatório" });
            }

            var (valid, error) = await _service.ValidateAndConsumeTokenAsync(request.Token);

            if (!valid)
            {
                return BadRequest(new { Message = error });
            }

            await Task.Delay(100);

            return Ok(new
            {
                OrderId = $"ORD-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                Message = "Pedido criado com sucesso",
                request.Description,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}

