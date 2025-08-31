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
        private readonly IUserRepository _userRepo; // ainda usamos para Query() em checks

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
            // carrega users + company
            var users = await _userManager.Users.Include(u => u.Company).ToListAsync();

            // carrega roles de cada user
            var model = new List<User>();
            foreach (var u in users)
            {
                // opcionalmente, se você exibe u.Role (enum) na table,
                // sincronize com o role real caso queira:
                var roles = await _userManager.GetRolesAsync(u);
                if (roles.Count > 0)
                {
                    if (Enum.TryParse<UserRole>(roles[0], out var r))
                        u.Role = r;
                }
                model.Add(u);
            }
            return View(model);
        }

        // GET: Users/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var user = await _userManager.Users
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

            if (ModelState.IsValid)
            {
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

                // Se não passar senha, define uma default temporária
                var initialPassword = string.IsNullOrWhiteSpace(password) ? "ChangeMe123$" : password;

                var result = await _userManager.CreateAsync(newUser, initialPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);

                    var companies = await _companyRepo.GetAllAsync();
                    ViewBag.CompanyId = new SelectList(companies, "Id", "Name", user.CompanyId);
                    return View(user);
                }

                // Adiciona o Role
                await _userManager.AddToRoleAsync(newUser, user.Role.ToString());

                return RedirectToAction(nameof(Index));
            }

            var companiesList = await _companyRepo.GetAllAsync();
            ViewBag.CompanyId = new SelectList(companiesList, "Id", "Name", user.CompanyId);
            return View(user);
        }


        // GET: Users/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var user = await _userManager.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            // hidrata enum a partir do role real
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
            // não validar navegação que não vem no POST
            ModelState.Remove("Company");
            ModelState.Remove("OwnedUnits");

            if (id != user.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var companies = await _companyRepo.GetAllAsync();
                ViewBag.CompanyId = new SelectList(companies, "Id", "Name", user.CompanyId);
                return View(user);
            }

            var dbUser = await _userManager.FindByIdAsync(id);
            if (dbUser == null) return NotFound();

            // atualiza campos permitidos
            dbUser.FullName = user.FullName?.Trim() ?? dbUser.FullName;
            dbUser.Email = user.Email?.Trim() ?? dbUser.Email;
            dbUser.UserName = dbUser.Email; // se essa é sua regra
            dbUser.CompanyId = user.CompanyId;
            dbUser.IsActive = user.IsActive;

            // sincroniza Role
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
                var companies = await _companyRepo.GetAllAsync();
                ViewBag.CompanyId = new SelectList(companies, "Id", "Name", user.CompanyId);
                return View(user);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var user = await _userManager.Users
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
            // se tiver Units relacionadas, faz soft delete
            var hasUnits = await _userRepo.Query()
                .Where(u => u.Id == id)
                .Select(u => u.OwnedUnits.Any())
                .FirstOrDefaultAsync();

            if (hasUnits)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound();
                user.IsActive = false;
                await _userManager.UpdateAsync(user);
                TempData["Success"] = "Usuário possui unidades vinculadas. A conta foi desativada.";
                return RedirectToAction(nameof(Index));
            }

            var dbUser = await _userManager.FindByIdAsync(id);
            if (dbUser == null) return NotFound();

            var result = await _userManager.DeleteAsync(dbUser);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Não foi possível excluir o usuário.";
            }
            else
            {
                TempData["Success"] = "Usuário excluído com sucesso.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
