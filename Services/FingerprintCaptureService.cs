using DPFP;
using DPFP.Capture;
using DPFP.Processing;
using DPFP.Verification;
using FingerprintService.Data;
using FingerprintService.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FingerprintService.Services
{
    public class CaptureSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string Status { get; set; } = "waiting"; // waiting, captured, failed
        public Sample? Sample { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class FingerprintCaptureService : IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private Capture? _capture;
        private readonly Dictionary<string, CaptureSession> _sessions = new();
        private CaptureSession? _activeSession;
        private readonly object _lock = new();

        public FingerprintCaptureService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

//         public string StartCapture()
// {
//     lock (_lock)
//     {
//         StopCapture();

//         var session = new CaptureSession();
//         _activeSession = session;
//         _sessions[session.SessionId] = session;

//         // Clean old sessions
//         var old = _sessions
//             .Where(s => s.Value.CreatedAt < DateTime.UtcNow.AddMinutes(-2))
//             .Select(s => s.Key)
//             .ToList();
//         foreach (var key in old)
//             _sessions.Remove(key);

//         try
//         {
//             _capture = new Capture();
//             _capture.EventHandler = new CaptureEventHandler(this);
//             _capture.StartCapture();
//             Console.WriteLine($"✅ Capture started. Session: {session.SessionId}");
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"❌ Capture init failed: {ex.Message}");
//             _activeSession.Status = "failed";
//             throw new Exception($"Failed to initialize fingerprint reader: {ex.Message}");
//         }

//         return session.SessionId;
//     }
// }
private Thread? _staThread;
private System.Windows.Forms.ApplicationContext? _appContext;

public string StartCapture()
{
    lock (_lock)
    {
        StopCapture();

        var session = new CaptureSession();
        _activeSession = session;
        _sessions[session.SessionId] = session;

        var old = _sessions
            .Where(s => s.Value.CreatedAt < DateTime.UtcNow.AddMinutes(-2))
            .Select(s => s.Key)
            .ToList();
        foreach (var key in old)
            _sessions.Remove(key);

        var initialized = new ManualResetEventSlim(false);
        string? initError = null;

        _staThread = new Thread(() =>
        {
            try
            {
                Console.WriteLine($"✅ yha tak code work kar rha hy {session.SessionId}");
                _capture = new DPFP.Capture.Capture();
                _capture.EventHandler = new CaptureEventHandler(this);
                _capture.StartCapture();
                Console.WriteLine($"✅ Capture started. Session: {session.SessionId}");

                initialized.Set(); // signal success

                // Run message pump for SDK events
                _appContext = new System.Windows.Forms.ApplicationContext();
                System.Windows.Forms.Application.Run(_appContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ STA thread error: {ex.Message}");
                initError = ex.Message;
                initialized.Set(); // signal failure
            }
        });

        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.IsBackground = true;
        _staThread.Start();

        // Wait up to 3 seconds for initialization
        initialized.Wait(3000);

        if (initError != null)
        {
            _activeSession.Status = "failed";
            throw new Exception($"Failed to initialize: {initError}");
        }

        return session.SessionId;
    }
}

public void StopCapture()
{
    try
    {
        _capture?.StopCapture();
        _capture = null;
    }
    catch { }

    try
    {
        _appContext?.ExitThread();
        _appContext = null;
    }
    catch { }
}

        public CaptureSession? GetSession(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public void OnSampleCaptured(Sample sample)
        {
            lock (_lock)
            {
                if (_activeSession != null)
                {
                    _activeSession.Sample = sample;
                    _activeSession.Status = "captured";
                    Console.WriteLine($"Sample captured for session: {_activeSession.SessionId}");
                    StopCapture();
                }
            }
        }

        public void OnCaptureFailed()
        {
            lock (_lock)
            {
                if (_activeSession != null)
                {
                    _activeSession.Status = "failed";
                    Console.WriteLine($"Capture failed for session: {_activeSession.SessionId}");
                }
            }
        }

        public async Task<bool> EnrollAsync(string userId, string sessionId)
        {
            var session = GetSession(sessionId);
            if (session?.Sample == null)
                throw new Exception("No captured sample found for this session");

            // Extract features
            var extractor = new FeatureExtraction();
            var feedback = CaptureFeedback.None;
            var featureSet = new FeatureSet();
            extractor.CreateFeatureSet(
                session.Sample,
                DataPurpose.Enrollment,
                ref feedback,
                ref featureSet
            );

            if (feedback != CaptureFeedback.Good)
                throw new Exception($"Poor quality sample: {feedback}");

            // Create template
            var enrollment = new Enrollment();
            enrollment.AddFeatures(featureSet);

            if (enrollment.Template == null)
                throw new Exception("Could not create enrollment template");

            byte[]? templateBytes = null;
            enrollment.Template.Serialize(ref templateBytes);

            if (templateBytes == null)
                throw new Exception("Template serialization failed");

            // Create scope to use DbContext
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Remove existing enrollment for this user
            var existing = db.FingerprintTemplates
                .Where(f => f.UserId == userId)
                .ToList();
            db.FingerprintTemplates.RemoveRange(existing);

            db.FingerprintTemplates.Add(new FingerprintTemplate
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Template = templateBytes,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<MatchResponse> MatchAsync(string sessionId)
        {
            var session = GetSession(sessionId);
            if (session?.Sample == null)
                throw new Exception("No captured sample found for this session");

            // Extract features for verification
            var extractor = new FeatureExtraction();
            var feedback = CaptureFeedback.None;
            var featureSet = new FeatureSet();
            extractor.CreateFeatureSet(
                session.Sample,
                DataPurpose.Verification,
                ref feedback,
                ref featureSet
            );

            if (feedback != CaptureFeedback.Good)
                return new MatchResponse { Matched = false };

            // Create scope to use DbContext
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var verifier = new Verification();
            var allEnrolled = db.FingerprintTemplates.ToList();

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

        public void Dispose()
        {
            StopCapture();
        }
    }

    // Capture event handler
    public class CaptureEventHandler : DPFP.Capture.EventHandler
    {
        private readonly FingerprintCaptureService _service;

        public CaptureEventHandler(FingerprintCaptureService service)
        {
            _service = service;
        }

        public void OnComplete(object capture, string readerSerialNumber, Sample sample)
        {
            _service.OnSampleCaptured(sample);
        }

        public void OnFingerGone(object capture, string readerSerialNumber)
        {
            Console.WriteLine("Finger removed");
        }

        public void OnFingerTouch(object capture, string readerSerialNumber)
        {
            Console.WriteLine("Finger detected");
        }

        public void OnReaderConnect(object capture, string readerSerialNumber)
        {
            Console.WriteLine($"Reader connected: {readerSerialNumber}");
        }

        public void OnReaderDisconnect(object capture, string readerSerialNumber)
        {
            Console.WriteLine($"Reader disconnected: {readerSerialNumber}");
        }

        public void OnSampleQuality(object capture, string readerSerialNumber, CaptureFeedback captureFeedback)
        {
            Console.WriteLine($"Sample quality: {captureFeedback}");
            if (captureFeedback != CaptureFeedback.Good)
                _service.OnCaptureFailed();
        }
    }
}