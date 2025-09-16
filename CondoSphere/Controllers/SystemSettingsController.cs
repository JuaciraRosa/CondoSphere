using CondoSphere.Models;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class SystemSettingsController : Controller
    {
        private readonly ISystemSettingsService _svc;
        public SystemSettingsController(ISystemSettingsService svc) { _svc = svc; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var s = await _svc.GetCurrentAsync();
            return View(s);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var s = await _svc.GetCurrentAsync();
            return View(s);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SystemSettings model)
        {
            if (!ModelState.IsValid) return View(model);
            await _svc.UpdateAsync(model);
            TempData["Success"] = "Parâmetros atualizados.";
            return RedirectToAction(nameof(Index));
        }
    }
}
