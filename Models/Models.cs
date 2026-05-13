namespace FingerprintService.Models
{
    public class FingerprintTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public byte[] Template { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class EnrollRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;  // ← changed
    }

    public class MatchRequest
    {
        public string SessionId { get; set; } = string.Empty;  // ← changed
    }

    public class MatchResponse
    {
        public bool Matched { get; set; }
        public string? UserId { get; set; }
    }
}