namespace webapi.Models
{
    /// <summary>
    /// Padrão 4: Idempotência Token-Based
    /// Servidor gera token único que só pode ser usado uma vez
    /// </summary>
    public class IdempotencyToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }

    public class TokenGenerationResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Message { get; set; } = "Token gerado com sucesso. Use-o uma única vez.";
    }

    public class TokenBasedRequest
    {
        public string Token { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class PaymentResponse
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}

