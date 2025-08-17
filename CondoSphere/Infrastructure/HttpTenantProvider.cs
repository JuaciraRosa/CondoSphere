using System.Security.Claims;

namespace CondoSphere.Infrastructure
{
    public class HttpTenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _ctx;
        public HttpTenantProvider(IHttpContextAccessor ctx) => _ctx = ctx;

        public int? CompanyId =>
            int.TryParse(_ctx.HttpContext?.User?.FindFirst("company_id")?.Value, out var id) ? id : null;

        public string? UserId =>
            _ctx.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
