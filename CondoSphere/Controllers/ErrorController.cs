using CondoSphere.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CondoSphere.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;
        public ErrorController(ILogger<ErrorController> logger) => _logger = logger;

        [Route("Error/500")]
        public IActionResult Error500()
        {
            var feat = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (feat != null) _logger.LogError(feat.Error, "Unhandled at {Path}", feat.Path);

            Response.StatusCode = 500;
            return View(); // procura Views/Error/Error500.cshtml
        }

        [Route("Error/404")]
        public IActionResult Error404()
        {
            Response.StatusCode = 404;
            return View(); // procura Views/Error/Error404.cshtml
        }

        [Route("Error/403")]
        public IActionResult Error403()
        {
            Response.StatusCode = 403;
            return View(); // procura Views/Error/Error403.cshtml
        }

        // genérico para outros códigos (401, 408, 502, etc.)
        [Route("Error/{code:int}")]
        public IActionResult ErrorGeneric(int code)
        {
            Response.StatusCode = code;
            return View("ErrorGeneric", code); // procura Views/Error/Generic.cshtml
        }
    }

}
