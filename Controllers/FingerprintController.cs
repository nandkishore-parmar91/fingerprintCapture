using Microsoft.AspNetCore.Mvc;
using FingerprintService.Models;
using FingerprintService.Services;

namespace FingerprintService.Controllers
{
    [ApiController]
    [Route("api/fingerprint")]
    public class FingerprintController : ControllerBase
    {
        private readonly FingerprintCaptureService _service;

        public FingerprintController(FingerprintCaptureService service)
        {
            _service = service;
        }

        // Start capture → returns sessionId
       [HttpPost("start-capture")]
public IActionResult StartCapture()
{
    try
    {
        var sessionId = _service.StartCapture();
        return Ok(new { success = true, sessionId });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Start capture error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return StatusCode(500, new { message = ex.Message, detail = ex.StackTrace });
    }
}

[HttpGet("readers")]
public IActionResult GetReaders()
{
    try
    {
        var readers = new DPFP.Capture.ReadersCollection();
        var list = new List<object>();
        for (int i = 0; i < readers.Count; i++)
        {
            list.Add(new
            {
                index = i,
                serial = readers[i].SerialNumber,
                product = readers[i].ProductName
            });
        }
        return Ok(new { count = readers.Count, readers = list });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = ex.Message });
    }
}
        // Poll for result
        [HttpGet("capture-result/{sessionId}")]
        public IActionResult GetCaptureResult(string sessionId)
        {
            try
            {
                var session = _service.GetSession(sessionId);
                if (session == null)
                    return NotFound(new { message = "Session not found" });

                return Ok(new
                {
                    sessionId = session.SessionId,
                    status = session.Status // "waiting", "captured", "failed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Enroll using captured session
        [HttpPost("enroll")]
        public async Task<IActionResult> Enroll([FromBody] EnrollRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId) ||
                    string.IsNullOrEmpty(request.SessionId))
                    return BadRequest(new { message = "Missing userId or sessionId" });

                await _service.EnrollAsync(request.UserId, request.SessionId);
                return Ok(new { success = true, message = "Enrolled successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Match using captured session
        [HttpPost("match")]
        public async Task<IActionResult> Match([FromBody] MatchRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SessionId))
                    return BadRequest(new { message = "SessionId is required" });

                var result = await _service.MatchAsync(request.SessionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok", time = DateTime.UtcNow });
        }
    }
}