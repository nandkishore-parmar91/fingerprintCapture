namespace FingerprintService.Models
{
    public class EnrollRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string PngBase64 { get; set; } = string.Empty;
    }

    public class EnrollResponse
    {
        public bool Success { get; set; }
        public string Template { get; set; } = string.Empty;
    }

    public class StoredTemplate
    {
        public string UserId { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
    }

    public class MatchRequest
    {
        public string PngBase64 { get; set; } = string.Empty;
        public List<StoredTemplate> Templates { get; set; } = new();
    }

    public class MatchResponse
    {
        public bool Matched { get; set; }
        public string? UserId { get; set; }
    }
}