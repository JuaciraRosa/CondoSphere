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
        private readonly ICompanyRepository _companyRepo;

        public UsersController(IUserRepository userRepo, ICompanyRepository companyRepo)
        {
            _userRepo = userRepo;
            _companyRepo = companyRepo;
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

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            var companies = await _companyRepo.GetAllAsync();
            ViewBag.CompanyId = new SelectList(companies, "Id", "Name");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (ModelState.IsValid)
            {
                await _userRepo.AddAsync(user);
                await _userRepo.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var companies = await _companyRepo.GetAllAsync();
            ViewBag.CompanyId = new SelectList(companies, "Id", "Name", user.CompanyId);
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            var companies = await _companyRepo.GetAllAsync();
            ViewBag.CompanyId = new SelectList(companies, "Id", "Name", user.CompanyId);

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var companies = await _companyRepo.GetAllAsync();
            ViewBag.CompanyId = new SelectList(companies, "Id", "Name", user.CompanyId);

            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _userRepo.DeleteAsync(id);
            await _userRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
