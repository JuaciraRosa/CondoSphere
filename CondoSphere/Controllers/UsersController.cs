using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using CondoSphere.Models;
using CondoSphere.Services;
using CondoSphere.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ICompanyRepository _companyRepo;
        private readonly IUserRepository _userRepo;
        private readonly IEmailSender _emailSender;
        private readonly ISystemSettingsService _systemSettingsService;

        public UsersController(
            IUserRepository userRepo,
            ICompanyRepository companyRepo,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
             ISystemSettingsService systemSettingsService)
        {
            _userRepo = userRepo;
            _companyRepo = companyRepo;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _systemSettingsService = systemSettingsService;
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
            return View(new CreateUserViewModel { IsActive = true });
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel vm)
        {
          
            ModelState.Remove("OwnedUnits");

            // helper para recarregar dropdown e voltar à view com erros
            async Task<IActionResult> ReturnViewAsync()
            {
                var companiesReload = await _companyRepo.GetAllAsync();
                ViewBag.CompanyId = new SelectList(companiesReload, "Id", "Name", vm.CompanyId);
                return View(vm);
            }

            // Validações adicionais (além do [Compare] no VM)
            if (!string.Equals(vm.Password, vm.ConfirmPassword, StringComparison.Ordinal))
                ModelState.AddModelError("ConfirmPassword", "As palavras-passe não coincidem.");

            if (!vm.CompanyId.HasValue)
                ModelState.AddModelError("CompanyId", "Selecione a empresa.");

            if (!ModelState.IsValid)
                return await ReturnViewAsync();

            var newUser = new User
            {
                Email = vm.Email.Trim(),
                UserName = vm.Email.Trim(),
                FullName = vm.FullName.Trim(),
                CompanyId = vm.CompanyId,
                IsActive = vm.IsActive,
                EmailConfirmed = true,
                ProfileImagePath = "/images/default-user.png",
                Role = vm.Role
            };
            // cria o utilizador com a password validada
            var createRes = await _userManager.CreateAsync(newUser, vm.Password);
            if (!createRes.Succeeded)
            {
                foreach (var e in createRes.Errors)
                    ModelState.AddModelError("", e.Description);
                return await ReturnViewAsync();
            }

            // marca como provisória (4 dias)
            newUser.MustChangePassword = true;
            newUser.TempPasswordExpiresAt = DateTimeOffset.UtcNow.AddDays(4);
            await _userManager.UpdateAsync(newUser);

            // gera resetUrl (token/email codificados)
            var token = await _userManager.GeneratePasswordResetTokenAsync(newUser);
            var urlToken = System.Net.WebUtility.UrlEncode(token);
            var urlEmail = System.Net.WebUtility.UrlEncode(newUser.Email);
            var resetUrl = Url.Action("ResetPassword", "Auth",
                new { token = urlToken, email = urlEmail }, protocol: Request.Scheme);

            // e-mail de boas-vindas (templates + switches)
            var settings = await _systemSettingsService.GetCurrentAsync();
            var subject = settings.WelcomeUserEmailSubject ?? "Bem-vindo(a)";

            var defaultHtml = $@"
<p>Olá {System.Net.WebUtility.HtmlEncode(newUser.FullName ?? newUser.Email ?? "Utilizador")},</p>
<p>A sua conta no <strong>CondoSphere</strong> foi criada.</p>
<p><strong>Palavra-passe provisória (válida por 4 dias):</strong> <code>{System.Net.WebUtility.HtmlEncode(vm.Password)}</code></p>
<p>Por favor, para sua segurança, altere a sua palavra-passe neste link:</p>
<p><a href=""{resetUrl}"" target=""_blank"" rel=""noopener"">Alterar palavra-passe</a></p>
<p>Cumprimentos,<br/>CondoSphere</p>";

            var body = string.IsNullOrWhiteSpace(settings.WelcomeUserEmailHtml)
                ? defaultHtml
                : _systemSettingsService.RenderTemplate(
                    settings.WelcomeUserEmailHtml,
                    new Dictionary<string, string>
                    {
                        ["User.FullName"] = newUser.FullName ?? newUser.Email ?? "Utilizador",
                        ["User.Email"] = newUser.Email ?? "",
                        ["TempPassword"] = vm.Password,
                        ["ResetUrl"] = resetUrl,
                        ["Company.Name"] = settings.CompanyDisplayName ?? "CondoSphere"
                    });

            if (settings.EmailsEnabled && settings.WelcomeEmailEnabled)
            {
                await _emailSender.SendAsync(newUser.Email!, subject, body);
            }
          
            var roleName = vm.Role.ToString();
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

            // Role atual
            var roles = await _userManager.GetRolesAsync(user);
            var roleEnum = UserRole.Resident;
            if (roles.Count > 0 && Enum.TryParse<UserRole>(roles[0], out var parsed))
                roleEnum = parsed;

            // Carrega empresas para o dropdown
            var companies = await _companyRepo.GetAllAsync();
            ViewBag.CompanyId = new SelectList(companies, "Id", "Name", user.CompanyId);

            // Monta a ViewModel de edição
            var vm = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName ?? "",
                CompanyId = user.CompanyId,
                Role = roleEnum,
                IsActive = user.IsActive
            };

            return View(vm);
        }

        // POST: Users/Edit/{id}
        // POST: Users/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var companiesReload = await _companyRepo.GetAllAsync();
                ViewBag.CompanyId = new SelectList(companiesReload, "Id", "Name", vm.CompanyId);
                return View(vm);
            }

            var dbUser = await _userManager.FindByIdAsync(id);
            if (dbUser == null) return NotFound();

          
            if (!string.Equals(dbUser.Email, vm.Email, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Email", "O email não pode ser alterado aqui.");
                var companiesReload = await _companyRepo.GetAllAsync();
                ViewBag.CompanyId = new SelectList(companiesReload, "Id", "Name", vm.CompanyId);
                return View(vm);
            }

         
            dbUser.FullName = vm.FullName?.Trim() ?? dbUser.FullName;
            dbUser.CompanyId = vm.CompanyId;
            dbUser.IsActive = vm.IsActive;

         
            var targetRole = vm.Role.ToString();
            if (!await _roleManager.RoleExistsAsync(targetRole))
                await _roleManager.CreateAsync(new IdentityRole(targetRole));

            var currentRoles = await _userManager.GetRolesAsync(dbUser);
            if (!currentRoles.Contains(targetRole))
            {
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(dbUser, currentRoles);
                await _userManager.AddToRoleAsync(dbUser, targetRole);
            }

            dbUser.Role = vm.Role; 

            var res = await _userManager.UpdateAsync(dbUser);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
                var companiesReload = await _companyRepo.GetAllAsync();
                ViewBag.CompanyId = new SelectList(companiesReload, "Id", "Name", vm.CompanyId);
                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }



        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var me = _userManager.GetUserId(User);
            if (id == me)
            {
                TempData["Error"] = "Não pode desativar a sua própria conta.";
                return RedirectToAction(nameof(Index));
            }

            var u = await _userManager.FindByIdAsync(id);
            if (u == null) return NotFound();

            if (!u.IsActive)
            {
                TempData["Success"] = "Este utilizador já está inativo.";
                return RedirectToAction(nameof(Index));
            }

            u.IsActive = false;
            var res = await _userManager.UpdateAsync(u);
            TempData[res.Succeeded ? "Success" : "Error"] =
                res.Succeeded ? "Utilizador desativado." : string.Join("; ", res.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var u = await _userManager.FindByIdAsync(id);
            if (u == null) return NotFound();

            if (u.IsActive)
            {
                TempData["Success"] = "Este utilizador já está ativo.";
                return RedirectToAction(nameof(Index));
            }

            u.IsActive = true;
            var res = await _userManager.UpdateAsync(u);
            TempData[res.Succeeded ? "Success" : "Error"] =
                res.Succeeded ? "Utilizador ativado." : string.Join("; ", res.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var hasUnits = await _userRepo.Query()
                    .Where(u => u.Id == id)
                    .Select(u => u.OwnedUnits.Any())
                    .FirstOrDefaultAsync();

                if (hasUnits)
                {
                    var u = await _userManager.FindByIdAsync(id);
                    if (u == null)
                    {
                        TempData["Error"] = "User not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    u.IsActive = false;
                    await _userManager.UpdateAsync(u);
                    TempData["Success"] = "User has linked units. The account was deactivated.";
                    return RedirectToAction(nameof(Index));
                }

                var dbUser = await _userManager.FindByIdAsync(id);
                if (dbUser == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userManager.DeleteAsync(dbUser);
                TempData[result.Succeeded ? "Success" : "Error"] =
                    result.Succeeded ? "User deleted successfully." : "User could not be deleted.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "User could not be deleted due to related records.";
            }
            catch
            {
                TempData["Error"] = "An unexpected error occurred while deleting the user.";
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
