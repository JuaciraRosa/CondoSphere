using CondoSphere.API.Models;
using CondoSphere.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QRCoder;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace CondoSphere.API
{
    [Route("api/auth")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public AuthApiController(UserManager<User> userManager,
                              SignInManager<User> signInManager,
                              ApplicationDbContext db,
                              IConfiguration config,
                              IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _config = config;
            _env = env;
        }



        // ============ LOGIN ============
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email)
                       ?? await _userManager.FindByNameAsync(model.Email);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials (user=null)." });

            if (!user.IsActive)
                return Unauthorized(new { message = "Invalid credentials (inactive)." });

            var passwordOk = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordOk)
                return Unauthorized(new { message = "Invalid credentials (password=false)." });

            var twoFaEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (twoFaEnabled)
                return Ok(new { requires2FA = true });

            var token = await GenerateJwtToken(user);
            return Ok(new { token });
        }



        // ============ LOGIN 2FA ============
        [HttpPost("login-2fa")]
        [AllowAnonymous]
        public async Task<IActionResult> Login2FA([FromBody] Login2FADto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email)
                       ?? await _userManager.FindByNameAsync(model.Email);

            if (user == null) return Unauthorized();

            var valid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, model.Code);

            if (!valid) return Unauthorized(new { message = "Invalid 2FA code." });

            var token = await GenerateJwtToken(user);
            return Ok(new { token });
        }

     
        [HttpGet("twofactor/status")]
        public async Task<IActionResult> TwoFactorStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            var enabled = await _userManager.GetTwoFactorEnabledAsync(user!);
            return Ok(new { enabled });
        }


        // ============ PROFILE ============
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var companyName = user.CompanyId.HasValue
                ? await _db.Companies.Where(c => c.Id == user.CompanyId.Value)
                    .Select(c => c.Name).FirstOrDefaultAsync()
                : null;

            var dto = new ProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                CompanyName = companyName,
                Role = roles.FirstOrDefault(),
                ProfileImagePath = string.IsNullOrWhiteSpace(user.ProfileImagePath)
                    ? "/uploads/avatars/default.png"
                    : user.ProfileImagePath
            };
            return Ok(dto);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto model, IFormFile? avatar)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(model.FullName))
                user.FullName = model.FullName.Trim();

            if (!string.IsNullOrWhiteSpace(model.Email) &&
                !string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmail = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmail.Succeeded) return BadRequest(setEmail.Errors);
                var setUserName = await _userManager.SetUserNameAsync(user, model.Email);
                if (!setUserName.Succeeded) return BadRequest(setUserName.Errors);
            }

            if (avatar != null && avatar.Length > 0)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsRoot);

                var ext = Path.GetExtension(avatar.FileName);
                var fileName = $"{user.Id}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsRoot, fileName);

                using var fs = new FileStream(filePath, FileMode.Create);
                await avatar.CopyToAsync(fs);

                user.ProfileImagePath = $"/uploads/avatars/{fileName}";
            }

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded) return BadRequest(res.Errors);

            return Ok(new { message = "Profile updated successfully." });
        }

        // ============ PASSWORD ============
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            await _userManager.UpdateSecurityStampAsync(user);

            return Ok(new { message = "Password changed successfully." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return NotFound();

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { message = "Password reset successfully." });
        }

        // ============ TWO FACTOR ============
        [HttpGet("twofactor/setup")]
        [Authorize]
        public async Task<IActionResult> Setup2FA()
        {
            var user = await _userManager.GetUserAsync(User);
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var issuer = "CondoSphere";
            var email = user.Email ?? user.UserName ?? "user";
            var otpauth = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
                          $"?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

            return Ok(new
            {
                key = FormatKey(key),
                otpauthUri = otpauth,
                qrCodeBase64 = GenerateQrPngBase64(otpauth)
            });
        }

        [HttpPost("twofactor/enable")]
        [Authorize]
        public async Task<IActionResult> Enable2FA([FromBody] Verify2FADto model)
        {
            var user = await _userManager.GetUserAsync(User);
            var code = model.Code?.Replace(" ", "").Replace("-", "");
            var valid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

            if (!valid) return BadRequest(new { message = "Invalid verification code." });

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var recoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ToArray();

            return Ok(new { message = "2FA enabled.", recoveryCodes });
        }

        [HttpPost("twofactor/disable")]
        [Authorize]
        public async Task<IActionResult> Disable2FA()
        {
            var user = await _userManager.GetUserAsync(User);
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            return Ok(new { message = "2FA disabled." });
        }

        [HttpPost("twofactor/recovery")]
        [Authorize]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (!await _userManager.GetTwoFactorEnabledAsync(user))
                return BadRequest(new { message = "Enable 2FA first." });

            var codes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ToArray();
            return Ok(new { recoveryCodes = codes });
        }

        // ============ ACCOUNT STATUS ============
        [HttpPost("deactivate")]
        [Authorize]
        public async Task<IActionResult> Deactivate()
        {
            var user = await _userManager.GetUserAsync(User);
            user.IsActive = false;
            await _userManager.UpdateAsync(user);
            return Ok(new { message = "Account deactivated." });
        }

        [HttpDelete("delete")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            var res = await _userManager.DeleteAsync(user);
            if (!res.Succeeded) return BadRequest(res.Errors);
            return Ok(new { message = "Account deleted." });
        }

        // ============ DOWNLOAD DATA ============
        [HttpGet("download-data")]
        [Authorize]
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

            return Ok(data);
        }

        // ============ HELPERS ============
        private async Task<string> GenerateJwtToken(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.UserName ?? "")
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string FormatKey(string key)
            => Regex.Replace(key.ToUpperInvariant(), ".{4}", "$0 ").Trim();

        private static string GenerateQrPngBase64(string content)
        {
            var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var pngQr = new PngByteQRCode(data);
            var bytes = pngQr.GetGraphic(220);
            return Convert.ToBase64String(bytes);
        }
    }
}

