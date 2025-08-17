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
        private readonly INotificationRepository _repository;

        public NotificationsController(INotificationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            var notifications = await _repository.GetAllAsync();
            return View(notifications);
        }
        // GET: Notifications/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var notification = await _repository.GetByIdAsync(id);
            if (notification == null)
                return NotFound();

            return View(notification);
        }


        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Notification notification)
        {
            if (ModelState.IsValid)
            {
                await _repository.AddAsync(notification);
                return RedirectToAction(nameof(Index));
            }
            return View(notification);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var notification = await _repository.GetByIdAsync(id);
            if (notification == null) return NotFound();
            return View(notification);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Notification notification)
        {
            if (id != notification.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _repository.Update(notification);
                await _repository.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(notification);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _repository.GetByIdAsync(id);
            if (notification == null) return NotFound();
            return View(notification);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _repository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
