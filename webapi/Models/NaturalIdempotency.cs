namespace webapi.Models
{
    /// <summary>
    /// Padrão 2: Idempotência Natural
    /// Operações baseadas em estado final (PUT, DELETE)
    /// </summary>
    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateUserProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
    }
}

