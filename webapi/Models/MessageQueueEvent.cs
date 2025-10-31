namespace webapi.Models
{
    /// <summary>
    /// Model para simular eventos de mensageria
    /// Utiliza Content-Based Deduplication
    /// </summary>
    public class MessageQueueEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; } = 0;
    }

    public class PublishEventRequest
    {
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }

    public class EventProcessingResult
    {
        public string EventId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool WasDuplicate { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}

