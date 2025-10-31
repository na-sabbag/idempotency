namespace webapi.Models
{
    /// <summary>
    /// Padrão 3: Idempotência Version-Based (Optimistic Locking)
    /// Cada recurso tem uma versão que deve corresponder para atualização
    /// </summary>
    public class ResourceVersion
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateResourceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int ExpectedVersion { get; set; }
    }

    public class VersionConflictResponse
    {
        public string Message { get; set; } = string.Empty;
        public int CurrentVersion { get; set; }
        public int ExpectedVersion { get; set; }
    }
}

