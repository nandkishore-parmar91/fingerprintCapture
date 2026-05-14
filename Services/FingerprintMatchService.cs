using SourceAFIS;
using FingerprintService.Models;

namespace FingerprintService.Services
{
    public class FingerprintMatchService
    {

public EnrollResponse CreateTemplate(string pngBase64)
{
    try
    {
        var imageBytes = Convert.FromBase64String(pngBase64);
        var fingerprintImage = new FingerprintImage(imageBytes, new FingerprintImageOptions { Dpi = 500 });
        var template = new FingerprintTemplate(fingerprintImage);

        // ✅ Quality check — match template against itself
        var matcher = new FingerprintMatcher(template);
        var selfScore = matcher.Match(template);
        Console.WriteLine($"Self score (quality check): {selfScore}");

        if (selfScore < 40)
        {
            return new EnrollResponse
            {
                Success = false,
                Template = string.Empty,
                Message = $"Poor fingerprint quality. Score: {selfScore:F2}. Please try again."
            };
        }

        var templateBytes = template.ToByteArray();
        var templateBase64 = Convert.ToBase64String(templateBytes);
        Console.WriteLine($"✅ Template created successfully. Size: {templateBytes.Length} bytes");

        return new EnrollResponse
        {
            Success = true,
            Template = templateBase64
        };
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ CreateTemplate error: {ex.Message}");
        throw new Exception($"Failed to create template: {ex.Message}");
    }
}

        // PNG Base64 + stored templates → matched userId
        public MatchResponse MatchTemplate(string pngBase64, List<StoredTemplate> templates)
        {
            try
            {
                // Convert base64 PNG to bytes
                var imageBytes = Convert.FromBase64String(pngBase64);

                // Create probe template from PNG
                var fingerprintImage = new FingerprintImage(imageBytes,new FingerprintImageOptions { Dpi = 500 });
                var probeTemplate = new FingerprintTemplate(fingerprintImage);

                // Create matcher with probe
                var matcher = new FingerprintMatcher(probeTemplate);

                double bestScore = 0;
                string? bestUserId = null;

                // Match against all stored templates
                foreach (var stored in templates)
                {
                    try
                    {
                        var templateBytes = Convert.FromBase64String(stored.Template);
                        var candidateTemplate = new FingerprintTemplate(templateBytes);

                        var score = matcher.Match(candidateTemplate);
                        Console.WriteLine($"Score for {stored.UserId}: {score}");

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestUserId = stored.UserId;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error matching template for {stored.UserId}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Best score: {bestScore} for userId: {bestUserId}");

                // Threshold — SourceAFIS recommends 40
                if (bestScore >= 40 && bestUserId != null)
                {
                    return new MatchResponse
                    {
                        Matched = true,
                        UserId = bestUserId
                    };
                }
                
                return new MatchResponse { Matched = false };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MatchTemplate error: {ex.Message}");
                throw new Exception($"Failed to match template: {ex.Message}");
            }
        }

        
    }
}