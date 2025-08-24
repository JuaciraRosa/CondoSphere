using CondoSphere.Data;
using CondoSphere.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Controllers
{

    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext db,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _env = env;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            string? companyName = null;
            if (user.CompanyId.HasValue)
            {
                companyName = await _db.Companies
                    .Where(c => c.Id == user.CompanyId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();
            }

            var vm = new ProfileViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                CompanyName = companyName,
                Role = roles.FirstOrDefault(),
                // inclui avatar com fallback
                ProfileImagePath = string.IsNullOrWhiteSpace(user.ProfileImagePath)
                    ? "/uploads/avatars/default.png"
                    : user.ProfileImagePath
            };

            return View(vm);
        }

        // POST: /Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Atualiza apenas o que você deseja permitir
            user.FullName = model.FullName?.Trim();

            //Se quiser permitir alterar e - mail, descomente:
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmail = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmail.Succeeded)
                {
                    foreach (var err in setEmail.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    return View(model);
                }
                var setUserName = await _userManager.SetUserNameAsync(user, model.Email);
                if (!setUserName.Succeeded)
                {
                    foreach (var err in setUserName.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    return View(model);
                }
            }

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                foreach (var err in res.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return View(model);
            }

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model, IFormFile? avatar)
        {
            var user = await _userManager.GetUserAsync(User);

            // Atualiza dados básicos
            user.FullName = model.FullName;

            // Upload de foto (opcional)
            if (avatar != null && avatar.Length > 0)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsRoot);

                var fileName = $"{user.Id}_{Path.GetFileNameWithoutExtension(avatar.FileName)}{Path.GetExtension(avatar.FileName)}";
                var filePath = Path.Combine(uploadsRoot, fileName);

                using (var fs = new FileStream(filePath, FileMode.Create))
                    await avatar.CopyToAsync(fs);

                // salva caminho relativo para servir pela web
                user.ProfileImagePath = $"/uploads/avatars/{fileName}";
            }

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
                // Repassa o path atual para não “sumir” o preview na volta
                model.ProfileImagePath = user.ProfileImagePath ?? "/uploads/avatars/default.png";
                return View("Profile", model);
            }

            TempData["Success"] = "Profile saved successfully.";
            return RedirectToAction(nameof(Profile));
        }


        // GET: /Account/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return View(model);
            }

            // Refresh sign-in to update cookies
            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(ChangePassword));
        }


        [HttpGet]
        public async Task<IActionResult> TwoFactor()
        {
            var user = await _userManager.GetUserAsync(User);
            var is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

            // pega ou reseta chave
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            // URI otpauth (escaneável no app autenticador — você pode gerar um QR numa lib, mas o texto já funciona)
            var issuer = "CondoSphere";
            var email = user.Email;
            var otpauth = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

            ViewBag.Key = key;
            ViewBag.OtpAuthUri = otpauth;
            ViewBag.Enabled = is2faEnabled;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactor(string code)
        {
            var user = await _userManager.GetUserAsync(User);

            // remove espaços e hífens
            code = code?.Replace(" ", "").Replace("-", "");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

            if (!isValid)
            {
                TempData["Error"] = "Invalid verification code.";
                return RedirectToAction(nameof(TwoFactor));
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);

            // Gera códigos de recuperação (guarde para o usuário)
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            TempData["Success"] = "2FA enabled. Save your recovery codes: " + string.Join(", ", recoveryCodes);

            return RedirectToAction(nameof(TwoFactor));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor()
        {
            var user = await _userManager.GetUserAsync(User);
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            TempData["Success"] = "2FA disabled.";
            return RedirectToAction(nameof(TwoFactor));
        }



        [HttpGet]
        public async Task<IActionResult> DownloadData()
        {
            var user = await _userManager.GetUserAsync(User);

            var claims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var logins = await _userManager.GetLoginsAsync(user);

            var data = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.IsActive,
                user.CompanyId,
                user.ProfileImagePath,
                Roles = roles,
                Claims = claims.Select(c => new { c.Type, c.Value }),
                ExternalLogins = logins.Select(l => new { l.LoginProvider, l.ProviderKey, l.ProviderDisplayName })
            };

            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", "my-data.json");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            await _signInManager.SignOutAsync();
            TempData["Success"] = "Your account was deactivated.";
            return RedirectToAction("Login", "Auth");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var res = await _userManager.DeleteAsync(user);
            if (!res.Succeeded)
            {
                TempData["Error"] = string.Join("; ", res.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Profile));
            }

            await _signInManager.SignOutAsync();
            TempData["Success"] = "Your account was deleted.";
            return RedirectToAction("Login", "Auth");
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email required.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // não revelar que o usuário não existe → UX/segurança
                TempData["Success"] = "If a valid user exists, a reset link was sent.";
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action("ResetPassword", "Account",
                new { email = user.Email, token = token }, Request.Scheme);

            // TODO: enviar e-mail. Por ora, mostramos o link (para testes)
            TempData["Success"] = "Copy this reset link (dev): " + link;
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("Login", "Auth");

            var res = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (res.Succeeded)
            {
                TempData["Success"] = "Password updated. You can login now.";
                return RedirectToAction("Login", "Auth");
            }

            TempData["Error"] = string.Join("; ", res.Errors.Select(e => e.Description));
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }


    }
}
