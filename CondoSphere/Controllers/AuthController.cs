using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using CondoSphere.Models.Account;
using CondoSphere.Services;
using CondoSphere.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace CondoSphere.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ISystemSettingsService _systemSettingsService;

        public AuthController(SignInManager<User> signInManager, UserManager<User> userManager, IEmailSender emailSender, ISystemSettingsService systemSettingsService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _systemSettingsService = systemSettingsService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
            => View(new LoginViewModel { ReturnUrl = returnUrl });

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            var loginKey = string.IsNullOrWhiteSpace(model.EmailOrUser)
                ? (Request.Form["Email"].ToString() ?? "")
                : model.EmailOrUser;

            if (string.IsNullOrWhiteSpace(loginKey) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Credenciais inválidas.");
                return View(model);
            }

            // procura por email OU username
            var user = await _userManager.FindByEmailAsync(loginKey)
                       ?? await _userManager.FindByNameAsync(loginKey);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Credenciais inválidas.");
                return View(model);
            }

            // senha provisória expirada ⇒ BLOQUEIA login
            if (user.MustChangePassword &&
                user.TempPasswordExpiresAt.HasValue &&
                user.TempPasswordExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                ModelState.AddModelError(string.Empty,
                    "A sua palavra-passe provisória expirou. Clique em 'Forgot Password?' para definir uma nova.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "A sua conta está desativada. Contacte o administrador.");
                return View(model);
            }


            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Se ainda precisa trocar (não expirou), força a página de troca
                if (user.MustChangePassword)
                    return RedirectToAction("ChangePassword", "Auth", new { returnUrl });

                return LocalRedirect(returnUrl ?? "/");
            }

            if (result.RequiresTwoFactor)
                return RedirectToAction(nameof(LoginWith2fa),
                    new { returnUrl, rememberMe = model.RememberMe });

            if (result.IsLockedOut)
                return View("Lockout");

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Conta não autorizada. Verifique a confirmação de e-mail.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Tentativa de login inválida.");
            return View(model);
        }



        // GET: /Auth/Lockout
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            // Opcional: podes mostrar duração/restante via TempData (ver exemplo no POST de Login)
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
        {
            // só chega aqui após a 1ª etapa do login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                return RedirectToAction(nameof(Login), new { returnUrl });

            var vm = new LoginWith2faViewModel
            {
                RememberMe = rememberMe,
                ReturnUrl = returnUrl
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });

            var code = model.TwoFactorCode?.Replace(" ", "").Replace("-", "");
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                code!, model.RememberMe, model.RememberMachine);

            if (result.Succeeded)
                return LocalRedirect(model.ReturnUrl ?? "/");

            if (result.IsLockedOut)
                return View("Lockout");

            ModelState.AddModelError(string.Empty, "Código 2FA inválido.");
            return View(model);
        }


        // ---------- 2FA via CÓDIGO DE RECUPERAÇÃO ----------
        [HttpGet]
        public IActionResult LoginWithRecoveryCode(string? returnUrl = null)
            => View(new LoginWithRecoveryCodeViewModel { ReturnUrl = returnUrl });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(model.RecoveryCode!);

            if (result.Succeeded)
                return LocalRedirect(model.ReturnUrl ?? "/");

            if (result.IsLockedOut)
                return View("Lockout");

            ModelState.AddModelError(string.Empty, "Código de recuperação inválido.");
            return View(model);
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // resposta neutra p/ evitar enumeração de contas
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                TempData["Success"] = "If that account exists, you will receive an email to reset the password.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            // gera token e URL (token/email codificados)
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var urlToken = System.Net.WebUtility.UrlEncode(token);
            var urlEmail = System.Net.WebUtility.UrlEncode(user.Email);
            var callbackUrl = Url.Action("ResetPassword", "Auth",
                new { token = urlToken, email = urlEmail }, protocol: Request.Scheme);

            // lê parâmetros globais (templates + switches)
            var s = await _systemSettingsService.GetCurrentAsync();

            // se envio estiver desligado, não manda e-mail mas mantém resposta neutra
            if (!(s.EmailsEnabled && s.PasswordResetEmailEnabled))
            {
                TempData["Success"] = "If that account exists, you will receive an email to reset the password.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var subj = s.PasswordResetEmailSubject ?? "CondoSphere - Reset your password";
            var defaultHtml = $@"
<p>Hello {System.Net.WebUtility.HtmlEncode(user.Email)},</p>
<p>Click the link below to reset your password (valid for 4 days):</p>
<p><a href=""{callbackUrl}"">Reset Password</a></p>
<p>If you did not request this, you can ignore this email.</p>";

            var html = string.IsNullOrWhiteSpace(s.PasswordResetEmailHtml)
                ? defaultHtml
                : _systemSettingsService.RenderTemplate(
                    s.PasswordResetEmailHtml,
                    new Dictionary<string, string>
                    {
                        ["User.FullName"] = user.FullName ?? user.Email ?? "",
                        ["User.Email"] = user.Email ?? "",
                        ["ResetUrl"] = callbackUrl,
                        ["Company.Name"] = s.CompanyDisplayName ?? "CondoSphere"
                    });

            await _emailSender.SendAsync(user.Email!, subj, html);

            TempData["Success"] = "Check your email for password reset instructions.";
            return RedirectToAction(nameof(ForgotPassword));
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                // sem querystring válida → volta p/ forgot
                TempData["Error"] = "Link inválido ou incompleto. Solicite novamente a recuperação de senha.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            // tenta Base64Url; se falhar, usa fallback
            string fixedToken = token;
            try { fixedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(fixedToken)); }
            catch
            {
                fixedToken = fixedToken.Replace(" ", "+");
                fixedToken = WebUtility.UrlDecode(fixedToken);
            }

            string fixedEmail = email;
            try { fixedEmail = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(fixedEmail)); }
            catch { fixedEmail = WebUtility.UrlDecode(fixedEmail); }

            return View(new ResetPasswordViewModel { Token = fixedToken, Email = fixedEmail });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Normaliza token novamente (defensivo)
            model.Token ??= "";
            try { model.Token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token)); }
            catch
            {
                model.Token = model.Token.Replace(" ", "+");
                model.Token = WebUtility.UrlDecode(model.Token);
            }

            // 🔒 Validação de e-mail NO SERVIDOR (evita depender do client-side)
            if (string.IsNullOrWhiteSpace(model.Email) || !new EmailAddressAttribute().IsValid(model.Email))
            {
                ModelState.AddModelError(string.Empty, "Link inválido. Solicite novamente a recuperação de senha.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            // Não revela se existe
            if (user == null)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            var resetResult = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (!resetResult.Succeeded)
            {
                foreach (var e in resetResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            user.MustChangePassword = false;
            user.TempPasswordExpiresAt = null;
            if (!user.EmailConfirmed) user.EmailConfirmed = true;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = "Palavra-passe alterada com sucesso.";
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        //[HttpGet]
        //[AllowAnonymous]
        //public IActionResult ResetPassword(string token, string email)
        //{
        //    // tokens em links por email frequentemente chegam com ' ' no lugar de '+'
        //    // e/ou precisam de URL decode
        //    var fixedToken = (token ?? "")
        //        .Replace(" ", "+");                      // corrige espaços
        //    fixedToken = WebUtility.UrlDecode(fixedToken); // decode final

        //    return View(new ResetPasswordViewModel { Token = fixedToken, Email = email });
        //}

        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //        return View(model);

        //    // Normaliza de novo caso o form/cliente tenha alterado o valor
        //    model.Token = (model.Token ?? "").Replace(" ", "+");
        //    model.Token = WebUtility.UrlDecode(model.Token);

        //    var user = await _userManager.FindByEmailAsync(model.Email);
        //    // Para não revelar se o email existe:
        //    if (user == null)
        //        return RedirectToAction(nameof(ResetPasswordConfirmation));

        //    var resetResult = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        //    if (!resetResult.Succeeded)
        //    {
        //        // Mostra exatamente o porquê (token inválido, expirado, password policy, etc.)
        //        foreach (var e in resetResult.Errors)
        //            ModelState.AddModelError(string.Empty, e.Description);
        //        return View(model);
        //    }

        //    // Flags pós-reset
        //    user.MustChangePassword = false;
        //    user.TempPasswordExpiresAt = null;
        //    if (!user.EmailConfirmed) user.EmailConfirmed = true;

        //    var updateResult = await _userManager.UpdateAsync(user);
        //    if (!updateResult.Succeeded)
        //    {
        //        foreach (var e in updateResult.Errors)
        //            ModelState.AddModelError(string.Empty, e.Description);
        //        return View(model);
        //    }

        //    // Invalida sessões antigas
        //    await _userManager.UpdateSecurityStampAsync(user);

        //    TempData["Success"] = "Palavra-passe alterada com sucesso.";
        //    return RedirectToAction(nameof(ResetPasswordConfirmation));
        //}

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
    }
}
