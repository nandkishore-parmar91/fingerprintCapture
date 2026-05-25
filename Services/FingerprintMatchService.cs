

using SourceAFIS;
using FingerprintService.Models;

namespace FingerprintService.Services
{
    public class FingerprintMatchService
    {
        // CREATE TEMPLATE
        public EnrollResponse CreateTemplate(string pngBase64)
        {
            try
            {
                var imageBytes =
                    Convert.FromBase64String(pngBase64);

                var fingerprintImage =
                    new FingerprintImage(
                        imageBytes,
                        new FingerprintImageOptions
                        {
                            Dpi = 500
                        });

                var template =
                    new FingerprintTemplate(fingerprintImage);

                // QUALITY CHECK
                var matcher =
                    new FingerprintMatcher(template);

                var selfScore =
                    matcher.Match(template);

                Console.WriteLine(
                    $"Self quality score: {selfScore}"
                );

                if (selfScore < 40)
                {
                    return new EnrollResponse
                    {
                        Success = false,
                        Template = string.Empty,
                        Message =
                            $"Poor fingerprint quality. Score: {selfScore:F2}"
                    };
                }

                var templateBytes =
                    template.ToByteArray();

                var templateBase64 =
                    Convert.ToBase64String(templateBytes);

                return new EnrollResponse
                {
                    Success = true,
                    Template = templateBase64
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"❌ CreateTemplate error: {ex.Message}"
                );

                throw new Exception(
                    $"Failed to create template: {ex.Message}"
                );
            }
        }

        // DUPLICATE CHECK
        public object IsDuplicateFingerprint(
            string newTemplateBase64,
            List<FingerprintTemplateData> existingTemplates
        )
        {
            try
            {
                // FIRST ENROLLMENT
                if (
                    existingTemplates == null ||
                    existingTemplates.Count == 0
                )
                {
                    return new
                    {
                        IsDuplicate = false
                    };
                }

                var newTemplateBytes =
                    Convert.FromBase64String(
                        newTemplateBase64
                    );

                var newTemplate =
                    new FingerprintTemplate(
                        newTemplateBytes
                    );

                foreach (var item in existingTemplates)
                {
                    try
                    {
                        var existingBytes =
                            Convert.FromBase64String(
                                item.Template
                            );

                        var existingTemplate =
                            new FingerprintTemplate(
                                existingBytes
                            );

                        var matcher =
                            new FingerprintMatcher(
                                existingTemplate
                            );

                        var score =
                            matcher.Match(newTemplate);

                        Console.WriteLine(
                            $"Duplicate score for {item.UserId}: {score}"
                        );

                        // MATCH FOUND
                        if (score >= 10)
                        {
                            return new
                            {
                                IsDuplicate = true,
                                MatchedUserId = item.UserId,
                                Score = score
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"⚠️ Error checking duplicate for {item.UserId}: {ex.Message}"
                        );
                    }
                }

                return new
                {
                    IsDuplicate = false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"❌ Duplicate check error: {ex.Message}"
                );

                throw new Exception(
                    $"Duplicate check failed: {ex.Message}"
                );
            }
        }

        // MATCH
        public MatchResponse MatchTemplate(
            string pngBase64,
            List<StoredTemplate> templates
        )
        {
            try
            {
                var imageBytes =
                    Convert.FromBase64String(pngBase64);

                var fingerprintImage =
                    new FingerprintImage(
                        imageBytes,
                        new FingerprintImageOptions
                        {
                            Dpi = 500
                        });

                var probeTemplate =
                    new FingerprintTemplate(
                        fingerprintImage
                    );

                var matcher =
                    new FingerprintMatcher(
                        probeTemplate
                    );

                double bestScore = 0;
                string? bestUserId = null;

                foreach (var stored in templates)
                {
                    try
                    {
                        var templateBytes =
                            Convert.FromBase64String(
                                stored.Template
                            );

                        var candidateTemplate =
                            new FingerprintTemplate(
                                templateBytes
                            );

                        var score =
                            matcher.Match(
                                candidateTemplate
                            );

                        Console.WriteLine(
                            $"Score for {stored.UserId}: {score}"
                        );

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestUserId = stored.UserId;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"⚠️ Match error for {stored.UserId}: {ex.Message}"
                        );
                    }
                }

                if (
                    bestScore >= 40 &&
                    bestUserId != null
                )
                {
                    return new MatchResponse
                    {
                        Matched = true,
                        UserId = bestUserId
                    };
                }

                return new MatchResponse
                {
                    Matched = false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"❌ MatchTemplate error: {ex.Message}"
                );

                throw new Exception(
                    $"Failed to match template: {ex.Message}"
                );
            }
        }
    }
}