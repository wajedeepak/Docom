using docom.core.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace docom.core.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class SessionManagerController : ControllerBase
    {
        private readonly ILogger<SessionManagerController> _logger;
        private readonly ISessionManagerService _sessionManager;
        public SessionManagerController(ILogger<SessionManagerController> logger, ISessionManagerService sessionManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;
        }

        [HttpPost("[action]")]
        public ActionResult GetSessionStatus()
        {
            var result = _sessionManager.GetSessionStatus();
            return Ok(result);
        }


        [HttpPost("[action]")]
        public ActionResult StartSession()
        {
            var result = _sessionManager.StartSession();
            return Ok(result);
        }

        [HttpPost("[action]")]
        public ActionResult PauseSession()
        {
            var result = _sessionManager.PauseSession();
            return Ok(result);
        }

        [HttpPost("[action]")]
        public ActionResult EndSession()
        {
            var result = _sessionManager.EndSession();
            return Ok(result);
        }

        [HttpPost("[action]")]
        public ActionResult GetNumber([FromQuery] string patientName, [FromQuery] string patientContact)
        {
            var result = _sessionManager.GetNumber(patientName, patientContact);
            return Ok(result);
        }

        [HttpPost("[action]")]
        public ActionResult MoveNumber()
        {
            var result = _sessionManager.MoveNumber();
            return Ok(result);
        }

        [HttpPost("[action]")]
        public ActionResult GetTotalNumbers()
        {
            var result = _sessionManager.GetTotalNumbers();
            return Ok(result);
        }

        [HttpPost("[action]")]
        public ActionResult GetCurrentNumber()
        {
            var result = _sessionManager.GetCurrentNumber();
            return Ok(result);
        }
    }
}
