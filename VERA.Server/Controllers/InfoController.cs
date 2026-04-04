using Microsoft.AspNetCore.Mvc;
using VERA.Shared;
using VERA.Shared.Dto;

namespace VERA.Server.Controllers
{
    [ApiController]
    [Route("api/info")]
    public class InfoController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetInfo()
        {
            var serverVersion = typeof(InfoController).Assembly
                .GetName().Version?.ToString(3) ?? AppVersion.Current;

            return Ok(new ServerInfoResponse(serverVersion, AppVersion.MinClientVersion));
        }
    }
}
