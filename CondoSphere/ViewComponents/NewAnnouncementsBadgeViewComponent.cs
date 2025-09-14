using CondoSphere.Data;
using CondoSphere.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CondoSphere.ViewComponents
{
    public class NewAnnouncementsBadgeViewComponent : ViewComponent
    {
        private readonly IAnnouncementReadService _reads;
        public NewAnnouncementsBadgeViewComponent(IAnnouncementReadService reads) => _reads = reads;

        public async Task<IViewComponentResult> InvokeAsync(int days = 3) // param ignorado; mantido p/ compat
        {
            var userId = HttpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = string.IsNullOrEmpty(userId) ? 0 : await _reads.CountUnreadAsync(userId);
            return View(count);
        }
    }

}
