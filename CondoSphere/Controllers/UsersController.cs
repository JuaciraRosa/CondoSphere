using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using CondoSphere.Models;

namespace CondoSphere.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ICompanyRepository _companyRepo;
        private readonly IUserRepository _userRepo;

        public UsersController(
            IUserRepository userRepo,
            ICompanyRepository companyRepo,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userRepo = userRepo;
            _companyRepo = companyRepo;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .AsNoTracking()
                .Include(u => u.Company)
                .ToListAsync();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                if (roles.Count > 0 && Enum.TryParse<UserRole>(roles[0], out var r))
                    u.Role = r;
            }

            return View(users);
        }

        // GET: Users/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.Users
                .AsNoTracking()
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count > 0 && Enum.TryParse<UserRole>(roles[0], out var r))
                user.Role = r;

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
        public async Task<IActionResult> Create(User user, string? password)
        {
            ModelState.Remove("Company");
            ModelState.Remove("OwnedUnits");

            if (!ModelState.IsValid)
            {
                var companiesReload = await _companyRepo.GetAllAsync();
                ViewBag.CompanyId = new SelectList(companiesReload, "Id", "Name", user.CompanyId);
                return View(user);
            }

            var newUser = new User
            {
                Email = user.Email?.Trim(),
                UserName = user.Email?.Trim(),
                FullName = user.FullName?.Trim() ?? "",
                CompanyId = user.CompanyId,
                IsActive = user.IsActive,
                EmailConfirmed = true,
                ProfileImagePath = "/images/default-user.png"
            };

            var initialPassword = string.IsNullOrWhiteSpace(password) ? "ChangeMe123$" : password;

            var createRes = await _userManager.CreateAsync(newUser, initialPassword);
            if (!createRes.Succeeded)
            {
                foreach (var e in createRes.Errors) ModelState.AddModelError("", e.Description);
                var companiesReload = await _companyRepo.GetAllAsync();
                ViewBag.CompanyId = new SelectList(companiesReload, "Id", "Name", user.CompanyId);
                return View(user);
            }

            // Garante que o role existe e atribui
            var roleName = user.Role.ToString();
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));

            await _userManager.AddToRoleAsync(newUser, roleName);

            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.Users
                .AsNoTracking()
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count > 0 && Enum.TryParse<UserRole>(roles[0], out var r))
                user.Role = r;

            var companies = await _companyRepo.GetAllAsync();
            ViewBag.CompanyId = new SelectList(companies, "Id", "Name", user.CompanyId);
            return View(user);
        }

        // POST: Users/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, User user)
        {
            ModelState.Remove("Company");
            ModelState.Remove("OwnedUnits");

            if (id != user.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var companiesReload = await _companyRepo.GetAllAsync();
                ViewBag.CompanyId = new SelectList(companiesReload, "Id", "Name", user.CompanyId);
                return View(user);
            }

            var dbUser = await _userManager.FindByIdAsync(id);
            if (dbUser == null) return NotFound();

            // Atualiza campos
            var newEmail = user.Email?.Trim();
            dbUser.FullName = user.FullName?.Trim() ?? dbUser.FullName;
            if (!string.Equals(dbUser.Email, newEmail, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(newEmail))
            {
                dbUser.Email = newEmail;
                dbUser.UserName = newEmail;
                dbUser.NormalizedEmail = newEmail.ToUpperInvariant();
                dbUser.NormalizedUserName = newEmail.ToUpperInvariant();
            }
            dbUser.CompanyId = user.CompanyId;
            dbUser.IsActive = user.IsActive;

            // Sincroniza Role
            var targetRole = user.Role.ToString();
            if (!await _roleManager.RoleExistsAsync(targetRole))
                await _roleManager.CreateAsync(new IdentityRole(targetRole));

            var currentRoles = await _userManager.GetRolesAsync(dbUser);
            if (!currentRoles.Contains(targetRole))
            {
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(dbUser, currentRoles);
                await _userManager.AddToRoleAsync(dbUser, targetRole);
            }

            var res = await _userManager.UpdateAsync(dbUser);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
                var companiesReload = await _companyRepo.GetAllAsync();
                ViewBag.CompanyId = new SelectList(companiesReload, "Id", "Name", user.CompanyId);
                return View(user);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.Users
                .AsNoTracking()
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Users/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            // Se possui unidades → desativa (soft delete)
            var hasUnits = await _userRepo.Query()
                .Where(u => u.Id == id)
                .Select(u => u.OwnedUnits.Any())
                .FirstOrDefaultAsync();

            if (hasUnits)
            {
                var u = await _userManager.FindByIdAsync(id);
                if (u == null) return NotFound();
                u.IsActive = false;
                await _userManager.UpdateAsync(u);
                TempData["Success"] = "Usuário possui unidades vinculadas. A conta foi desativada.";
                return RedirectToAction(nameof(Index));
            }

            var dbUser = await _userManager.FindByIdAsync(id);
            if (dbUser == null) return NotFound();

            var result = await _userManager.DeleteAsync(dbUser);
            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? "Usuário excluído com sucesso." : "Não foi possível excluir o usuário.";

            return RedirectToAction(nameof(Index));
        }
    }
}
