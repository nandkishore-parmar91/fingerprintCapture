using DPFP;
using DPFP.Processing;
using DPFP.Verification;
using FingerprintService.Data;
using FingerprintService.Models;

namespace FingerprintService.Services
{
    public class FingerprintMatchService
    {
        private readonly AppDbContext _db;

        public FingerprintMatchService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> EnrollAsync(string userId, string sampleBase64)
        {
            // 1. Deserialize Sample from frontend
            var sampleBytes = Convert.FromBase64String(sampleBase64);
            var sample = new DPFP.Sample();
            sample.DeSerialize(sampleBytes);

            // 2. Extract features
            var extractor = new FeatureExtraction();
            var feedback = DPFP.Capture.CaptureFeedback.None;
            var featureSet = new FeatureSet();
            extractor.CreateFeatureSet(
                sample,
                DataPurpose.Enrollment,
                ref feedback,
                ref featureSet
            );

            if (feedback != DPFP.Capture.CaptureFeedback.Good)
                throw new Exception($"Poor quality sample: {feedback}");

            // 3. Create enrollment template
            var enrollment = new Enrollment();
            enrollment.AddFeatures(featureSet);

            // SDK needs enough samples — keep adding until template is ready
            // For our use case (1 scan = 1 enroll), we proceed even if FeaturesNeeded > 0
            if (enrollment.Template == null)
                throw new Exception("Could not create template from sample");

            // 4. Serialize and save
            byte[]? templateBytes = null;
            enrollment.Template.Serialize(ref templateBytes);

            if (templateBytes == null)
                throw new Exception("Template serialization failed");

            // Remove existing enrollment for this user
            var existing = _db.FingerprintTemplates
                .Where(f => f.UserId == userId)
                .ToList();
            _db.FingerprintTemplates.RemoveRange(existing);

            _db.FingerprintTemplates.Add(new FingerprintTemplate
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Template = templateBytes,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<MatchResponse> MatchAsync(string sampleBase64)
        {
            // 1. Deserialize Sample
            var sampleBytes = Convert.FromBase64String(sampleBase64);
            var sample = new DPFP.Sample();
            sample.DeSerialize(sampleBytes);

            // 2. Extract features for verification
            var extractor = new FeatureExtraction();
            var feedback = DPFP.Capture.CaptureFeedback.None;
            var featureSet = new FeatureSet();
            extractor.CreateFeatureSet(
                sample,
                DataPurpose.Verification,
                ref feedback,
                ref featureSet
            );

            if (feedback != DPFP.Capture.CaptureFeedback.Good)
                return new MatchResponse { Matched = false };

            // 3. Match against all enrolled
            var verifier = new Verification();
            var allEnrolled = _db.FingerprintTemplates.ToList();

            foreach (var enrolled in allEnrolled)
            {
                var template = new DPFP.Template();
                template.DeSerialize(enrolled.Template);

                var result = new Verification.Result();
                verifier.Verify(featureSet, template, ref result);

                if (result.Verified)
                {
                    return new MatchResponse
                    {
                        Matched = true,
                        UserId = enrolled.UserId
                    };
                }
            }

            return new MatchResponse { Matched = false };
        }
    }
}