using Microsoft.AspNetCore.Mvc;

namespace PeKeyMngr.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok();
        }
    }
}
