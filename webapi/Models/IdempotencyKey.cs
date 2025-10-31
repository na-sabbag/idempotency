namespace webapi.Models
{
    /// <summary>
    /// Padrão 1: Chave de Idempotência
    /// Armazena requisições usando uma chave única gerada pelo cliente
    /// </summary>
    public class IdempotencyKey
    {
        public string Key { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int StatusCode { get; set; }
    }

    public class IdempotencyRequest
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    public class IdempotencyResponse
    {
        public string Message { get; set; } = string.Empty;
        public string ProcessId { get; set; } = string.Empty;
        public bool IsFromCache { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

