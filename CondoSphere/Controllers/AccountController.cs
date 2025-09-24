using CondoSphere.Data;
using CondoSphere.Messaging;
using CondoSphere.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Text.RegularExpressions;

namespace CondoSphere.Controllers
{

    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext db,
            IWebHostEnvironment env,
             IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _env = env;
            _emailSender = emailSender;
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
            if (user == null) return NotFound();

            // Detecta se é um POST do formulário de avatar (multipart com ficheiro)
            bool isPhotoUpload = Request.HasFormContentType && Request.Form.Files?.Count > 0 && avatar != null && avatar.Length > 0;
            if (isPhotoUpload)
            {
                // Evita validação de FullName quando o post é só da foto
                ModelState.Remove(nameof(ProfileViewModel.FullName));
            }

            // Atualiza dados básicos SOMENTE se veio no POST (não sobrescreve com null)
            if (!isPhotoUpload) // post do formulário "Basic Information"
            {
                if (model.FullName != null) // veio no form
                {
                    var trimmed = model.FullName.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        user.FullName = trimmed;
                    }
                    // se veio vazio, mantém o valor atual do banco
                }
            }

            // Upload de foto (opcional)
            if (isPhotoUpload)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsRoot);

                var safeNameNoExt = Path.GetFileNameWithoutExtension(avatar!.FileName);
                var ext = Path.GetExtension(avatar.FileName);
                var fileName = $"{user.Id}_{safeNameNoExt}{ext}";
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
                // Garante que mostramos o nome atual se o post era só foto
                if (string.IsNullOrWhiteSpace(model.FullName))
                    model.FullName = user.FullName ?? "";
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

            // URI otpauth (Google/Microsoft Authenticator)
            var issuer = "CondoSphere";
            var email = user.Email ?? user.UserName ?? "user";
            var otpauth = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
                          $"?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

            // QR como Data URL (png base64)
            var qrDataUrl = GenerateQrPngDataUrl(otpauth);

            ViewBag.Enabled = is2faEnabled;
            ViewBag.Key = key;
            ViewBag.KeyFormatted = FormatKey(key);
            ViewBag.OtpAuthUri = otpauth;
            ViewBag.QrDataUrl = qrDataUrl;

            return View();
        }

        // helpers
        private static string GenerateQrPngDataUrl(string content)
        {
            var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var pngQr = new PngByteQRCode(data);
            var bytes = pngQr.GetGraphic(220); // tamanho do QR
            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }

        private static string FormatKey(string key)
        {
            // agrupa em blocos de 4: XXXX XXXX XXXX...
            return Regex.Replace(key.ToUpperInvariant(), ".{4}", "$0 ").Trim();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactor(string code)
        {
            var user = await _userManager.GetUserAsync(User);

            code = code?.Replace(" ", "").Replace("-", "");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

            if (!isValid)
            {
                TempData["Error"] = "Invalid verification code.";
                return RedirectToAction(nameof(TwoFactor));
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);

            // gera os 10 códigos e mostra de imediato
            var codes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ToArray();
            TempData["RecoveryCodes"] = string.Join(";", codes);

            // atualiza o cookie de auth com o novo estado (boa prática)
            await _signInManager.RefreshSignInAsync(user);

            return RedirectToAction(nameof(ShowRecoveryCodes));
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
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                TempData["Error"] = "Ative o 2FA antes de gerar códigos.";
                return RedirectToAction(nameof(TwoFactor));
            }

            var codes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ToArray();
            TempData["RecoveryCodes"] = string.Join(";", codes);
            return RedirectToAction(nameof(ShowRecoveryCodes));
        }

        [HttpGet]
        public IActionResult ShowRecoveryCodes()
        {
            var raw = TempData["RecoveryCodes"] as string;
            if (string.IsNullOrEmpty(raw)) return RedirectToAction(nameof(TwoFactor));
            var codes = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
            return View(model: codes); // IEnumerable<string>
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





    }
}
