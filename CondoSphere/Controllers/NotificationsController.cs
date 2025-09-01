using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CondoSphere.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationRepository _repository;
        private readonly ICondominiumRepository _condos;

        public NotificationsController(INotificationRepository repository,
                                       ICondominiumRepository condos)
        {
            _repository = repository;
            _condos = condos;
        }

        // helper: carrega o dropdown
        private async Task LoadCondominiumsAsync(int? selectedId = null)
        {
            var items = (await _condos.GetAllWithCompanyAsync())
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Company != null ? $"{c.Name} ({c.Company.Name})" : c.Name
                })
                .ToList();

            ViewBag.CondominiumId = new SelectList(items, "Value", "Text", selectedId);
        }

        public async Task<IActionResult> Index()
        {
            var notifications = await _repository.GetAllDetailedAsync();
            return View(notifications);
        }

        // GET: Notifications/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var notification = (await _repository.GetAllDetailedAsync())
                .FirstOrDefault(n => n.Id == id);
            if (notification == null) return NotFound();
            return View(notification);
        }

        // CREATE
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCondominiumsAsync();
            return View(new Notification { SentAt = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Notification notification)
        {
            if (!ModelState.IsValid)
            {
                await LoadCondominiumsAsync(notification.CondominiumId);
                return View(notification);
            }

            await _repository.AddAsync(notification);
            return RedirectToAction(nameof(Index));
        }

        // EDIT
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var notification = await _repository.GetByIdAsync(id);
            if (notification == null) return NotFound();

            await LoadCondominiumsAsync(notification.CondominiumId);
            return View(notification);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Notification notification)
        {
            if (id != notification.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadCondominiumsAsync(notification.CondominiumId);
                return View(notification);
            }

            _repository.Update(notification);
            await _repository.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var n = (await _repository.GetAllDetailedAsync())
                .FirstOrDefault(x => x.Id == id);
            if (n == null) return NotFound();
            return View(n);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _repository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
