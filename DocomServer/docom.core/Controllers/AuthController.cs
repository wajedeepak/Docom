using docom.core.Models;
using Microsoft.AspNetCore.Mvc;

namespace docom.core.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet("[action]")]
        public IActionResult GetRequest()
        {
            return Ok(new { Message = "Get Request successful" });
        }

        [HttpPost("[action]")]
        public IActionResult SignIn([FromBody] LoginRequest request)
        {
            var configuredUsername = _configuration["LoginCredentials:Username"];
            var configuredPassword = _configuration["LoginCredentials:Password"];

            if (request.Username == configuredUsername && request.Password == configuredPassword)
            {
                return Ok(new { Message = "Login successful" });
            }
            else
            {
                return Unauthorized(new { Message = "Invalid credentials" });
            }
        }
    }
}
