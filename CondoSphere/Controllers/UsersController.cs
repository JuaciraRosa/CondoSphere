using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CondoSphere.Data;
using CondoSphere.Data.Interfaces;

namespace CondoSphere.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepo;

        public UsersController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userRepo.GetAllAsync();
            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
            if (ModelState.IsValid)
            {
                await _userRepo.AddAsync(user);
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _userRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
