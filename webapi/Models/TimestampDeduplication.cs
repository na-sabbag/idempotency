namespace webapi.Models
{
    /// <summary>
    /// Padrão 5: Desduplicação Timestamp-Based
    /// Ignora operações com timestamp anterior à última processada
    /// </summary>
    public class TimestampOperation
    {
        public string ResourceId { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }

    public class TimestampRequest
    {
        public string ResourceId { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class TimestampResponse
    {
        public bool Accepted { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? LastTimestamp { get; set; }
        public DateTime? ReceivedTimestamp { get; set; }
    }
}

