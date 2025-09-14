using CondoSphere.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.ViewComponents
{
   
    public class NewChatBadgeViewComponent : ViewComponent
    {
        private readonly IChatAlertService _alerts;
        public NewChatBadgeViewComponent(IChatAlertService alerts) => _alerts = alerts;

        public async Task<IViewComponentResult> InvokeAsync(int days = 3)
        {
            if (!(User.IsInRole("Administrator") || User.IsInRole("Manager")))
                return View(0);

            var count = await _alerts.CountUnseenAsync(days);
            return View(count);
        }
    }

}
