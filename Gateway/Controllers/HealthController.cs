using Microsoft.AspNetCore.Mvc;

namespace GateWay.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private static DateTime? _startTime;

        public HealthController()
        {
            _startTime ??= DateTime.UtcNow;
        }

        [HttpGet]
        public IActionResult Get()
        {
            // Fail health checks for first 10 seconds
            if ((DateTime.UtcNow - _startTime)?.TotalSeconds < 10)
            {
                return StatusCode(503, "Starting up");
            }
            return Ok("Healthy");
        }
    }
}