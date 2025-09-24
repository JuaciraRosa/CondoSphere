using CondoSphere.Data.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/units")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UnitsApiController : ControllerBase
    {
        private readonly IUnitRepository _units;
        public UnitsApiController(IUnitRepository units) => _units = units;

        // GET api/units/by-condo/5  -> ["A101","A102",...]
        [HttpGet("by-condo/{condominiumId:int}")]
        public async Task<IActionResult> GetNumbersByCondo(int condominiumId)
        {
            var numbers = await _units.GetNumbersByCondominiumIdAsync(condominiumId);
            return Ok(numbers?.ToArray() ?? Array.Empty<string>());
        }

    }
}
