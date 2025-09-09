using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Security.Claims;

namespace CondoSphere.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;

        public AuthController(SignInManager<User> signInManager, UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
            => View(new LoginViewModel { ReturnUrl = returnUrl });

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // fallback: se a view manda "Email", usa-o quando EmailOrUser vier vazio
            var loginKey = string.IsNullOrWhiteSpace(model.EmailOrUser)
                ? (Request.Form["Email"].ToString() ?? "")
                : model.EmailOrUser;

            if (string.IsNullOrWhiteSpace(loginKey) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Credenciais inválidas.");
                return View(model);
            }

            // procurar por email OU username
            var user = await _userManager.FindByEmailAsync(loginKey)
                       ?? await _userManager.FindByNameAsync(loginKey);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Credenciais inválidas.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
                return LocalRedirect(model.ReturnUrl ?? "/");

            if (result.RequiresTwoFactor)
                return RedirectToAction(nameof(LoginWith2fa),
                    new { returnUrl = model.ReturnUrl, rememberMe = model.RememberMe });

            if (result.IsLockedOut)
                return View("Lockout");

            if (result.IsNotAllowed)
            {
                // caso Identity exija email confirmado, etc.
                ModelState.AddModelError(string.Empty, "Conta não autorizada. Verifique a confirmação de e-mail.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View(model);
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
    

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
    }
}
