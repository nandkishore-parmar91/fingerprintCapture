using Microsoft.AspNetCore.Mvc;
using FingerprintService.Models;
using FingerprintService.Services;

namespace FingerprintService.Controllers
{
    [ApiController]
    [Route("api/fingerprint")]
    public class FingerprintController : ControllerBase
    {
        private readonly FingerprintMatchService _service;

        public FingerprintController(FingerprintMatchService service)
        {
            _service = service;
        }

        [HttpPost("enroll")]
        public IActionResult Enroll([FromBody] EnrollRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PngBase64))
                    return BadRequest(new { message = "PngBase64 is required" });

                Console.WriteLine($"Enrolling userId: {request.UserId}");
                var result = _service.CreateTemplate(request.PngBase64);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Enroll error: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("match")]
        public IActionResult Match([FromBody] MatchRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PngBase64))
                    return BadRequest(new { message = "PngBase64 is required" });

                if (request.Templates == null || request.Templates.Count == 0)
                    return BadRequest(new { message = "No templates provided" });

                Console.WriteLine($"Matching against {request.Templates.Count} templates");
                var result = _service.MatchTemplate(request.PngBase64, request.Templates);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Match error: {ex.Message}");
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