using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CondoSphere.Data;
using CondoSphere.Models;
using CondoSphere.Data.Interfaces;

namespace CondoSphere.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly INotificationRepository _notificationRepo;

        public NotificationsController(INotificationRepository notificationRepo)
        {
            _notificationRepo = notificationRepo;
        }

        public async Task<IActionResult> Index()
        {
            var notifications = await _notificationRepo.GetAllAsync();
            return View(notifications);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Notification notification)
        {
            if (ModelState.IsValid)
            {
                await _notificationRepo.AddAsync(notification);
                return RedirectToAction(nameof(Index));
            }
            return View(notification);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var notification = await _notificationRepo.GetByIdAsync(id);
            if (notification == null) return NotFound();
            return View(notification);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Notification notification)
        {
            if (id != notification.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _notificationRepo.Update(notification);
                return RedirectToAction(nameof(Index));
            }
            return View(notification);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _notificationRepo.GetByIdAsync(id);
            if (notification == null) return NotFound();
            return View(notification);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _notificationRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
