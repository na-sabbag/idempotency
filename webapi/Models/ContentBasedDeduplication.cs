namespace webapi.Models
{
    /// <summary>
    /// Padrão 6: Desduplicação Content-Based
    /// Hash do conteúdo para detectar operações duplicadas
    /// </summary>
    public class ContentBasedOperation
    {
        public string Hash { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }

    public class ContentBasedRequest
    {
        public string EventType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    public class ContentBasedResponse
    {
        public bool Processed { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public bool IsDuplicate { get; set; }
        public DateTime? FirstProcessedAt { get; set; }
    }
}

