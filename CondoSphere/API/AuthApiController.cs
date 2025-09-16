using CondoSphere.API.Models;
using CondoSphere.API.Models.Dtos;
using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using QRCoder;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace CondoSphere.API
{
   

    namespace CondoSphere.API
    {
        [ApiController]
        [Route("api/auth")]
        public class AuthApiController : ControllerBase
        {
            private readonly UserManager<User> _userManager;
            private readonly SignInManager<User> _signInManager;
            private readonly IConfiguration _configuration;
            private readonly IWebHostEnvironment _env;

            public AuthApiController(
                UserManager<User> userManager,
                SignInManager<User> signInManager,
                IConfiguration configuration,
                IWebHostEnvironment env)
            {
                _userManager = userManager;
                _signInManager = signInManager;
                _configuration = configuration;
                _env = env;
            }

            // ===== AUTENTICAÇÃO BÁSICA =====
            [HttpPost("login")]
            [AllowAnonymous]
            public async Task<ActionResult<TokenDto>> Login([FromBody] LoginDto dto)
            {
                var key = dto.EmailOrUser?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(dto.Password))
                    return BadRequest(new { error = "Email/username and password are required" });

                var user = await _userManager.FindByEmailAsync(key)
                           ?? await _userManager.FindByNameAsync(key);

                if (user == null)
                    return Unauthorized(new { error = "invalid" });

                // ⚠️ senha provisória expirada → bloqueia login
                if (user.MustChangePassword &&
                    user.TempPasswordExpiresAt.HasValue &&
                    user.TempPasswordExpiresAt.Value <= DateTimeOffset.UtcNow)
                {
                    return Unauthorized(new { error = "expired" });
                }

                // Mantém o padrão existente (PasswordSignInAsync) p/ suportar 2FA flow
                var res = await _signInManager.PasswordSignInAsync(
                    user.UserName!, dto.Password, dto.RememberMe, lockoutOnFailure: true);

                if (res.Succeeded)
                {
                    var token = await GenerateJwtTokenAsync(user);
                    return new TokenDto { Token = token, RequiresTwoFactor = false };
                }

                if (res.RequiresTwoFactor)
                    return new TokenDto { RequiresTwoFactor = true };

                if (res.IsLockedOut)
                    return Unauthorized(new { error = "locked" });

                return Unauthorized(new { error = "invalid" });
            }


            [HttpPost("2fa")]
            [AllowAnonymous]
            public async Task<ActionResult<TokenDto>> TwoFactor([FromBody] TwoFaDto dto)
            {
                var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user == null)
                    return Unauthorized(new { error = "Two-factor authentication session not found" });

                var code = (dto.Code ?? "").Replace(" ", "").Replace("-", "");
                if (string.IsNullOrEmpty(code))
                    return BadRequest(new { error = "Code is required" });

                var res = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                    code, /* rememberMe */ true, /* rememberClient */ dto.RememberMachine);

                if (!res.Succeeded)
                    return Unauthorized(new { error = res.IsLockedOut ? "locked" : "invalid" });

                var token = await GenerateJwtTokenAsync(user);
                return new TokenDto { Token = token, RequiresTwoFactor = false };
            }

            [HttpPost("recovery")]
            [AllowAnonymous]
            public async Task<ActionResult<TokenDto>> TwoFactorRecovery([FromBody] RecoveryDto dto)
            {
                var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user == null)
                    return Unauthorized(new { error = "Two-factor authentication session not found" });

                var recoveryCode = (dto.RecoveryCode ?? "").Trim();
                if (string.IsNullOrEmpty(recoveryCode))
                    return BadRequest(new { error = "Recovery code is required" });

                var res = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);
                if (!res.Succeeded)
                    return Unauthorized(new { error = res.IsLockedOut ? "locked" : "invalid" });

                var token = await GenerateJwtTokenAsync(user);
                return new TokenDto { Token = token, RequiresTwoFactor = false };
            }

            // ===== PERFIL DO USUÁRIO =====

            [HttpGet("profile")]
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
            public async Task<ActionResult<ProfileDto>> GetProfile()
            {
                var u = await _userManager.GetUserAsync(User);
                if (u == null) return Unauthorized();

                return new ProfileDto
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    FullName = u.FullName,
                    ProfileImageUrl = string.IsNullOrWhiteSpace(u.ProfileImagePath)
                        ? "/uploads/avatars/default.png"
                        : u.ProfileImagePath,
                    TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(u)
                };
            }

            [HttpPost("profile")]
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
            public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateDto dto)
            {
                var u = await _userManager.GetUserAsync(User);
                if (u == null) return Unauthorized();

                if (!string.IsNullOrWhiteSpace(dto.FullName))
                    u.FullName = dto.FullName.Trim();

                var res = await _userManager.UpdateAsync(u);
                if (!res.Succeeded)
                    return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

                return NoContent();
            }

            [HttpPost("avatar")]
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
            [RequestSizeLimit(5_000_000)]
            public async Task<ActionResult<string>> UploadAvatar(IFormFile file)
            {
                var u = await _userManager.GetUserAsync(User);
                if (u == null) return Unauthorized();
                if (file == null || file.Length == 0) return BadRequest("nofile");

                var folder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(folder);
                var fileName = $"{u.Id}_{Path.GetFileName(file.FileName)}";
                var full = Path.Combine(folder, fileName);

                await using (var fs = System.IO.File.Create(full))
                    await file.CopyToAsync(fs);

                u.ProfileImagePath = $"/uploads/avatars/{fileName}";
                await _userManager.UpdateAsync(u);
                return Ok(u.ProfileImagePath);
            }

            // ===== CONFIGURAÇÃO 2FA =====

            [HttpGet("2fa/setup")]
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
            public async Task<ActionResult<TwoFactorSetupDto>> TwoFactorSetup()
            {
                var u = await _userManager.GetUserAsync(User);
                if (u == null) return Unauthorized();

                var already = await _userManager.GetTwoFactorEnabledAsync(u);

                var key = await _userManager.GetAuthenticatorKeyAsync(u);
                if (string.IsNullOrEmpty(key))
                {
                    await _userManager.ResetAuthenticatorKeyAsync(u);
                    key = await _userManager.GetAuthenticatorKeyAsync(u);
                }

                var issuer = "CondoSphere";
                var email = u.Email ?? u.UserName ?? "user";
                var otpauth = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
                              $"?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

                return new TwoFactorSetupDto
                {
                    AlreadyEnabled = already,
                    SecretKey = Regex.Replace(key.ToUpperInvariant(), ".{4}", "$0 ").Trim(),
                    OtpAuthUri = otpauth,
                    QrPngBase64 = QrAsBase64(otpauth)
                };
            }

            [HttpPost("2fa/enable")]
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
            public async Task<ActionResult<string[]>> EnableTwoFactor([FromBody] TwoFactorCodeDto dto)
            {
                var u = await _userManager.GetUserAsync(User);
                if (u == null) return Unauthorized();

                var code = dto.Code?.Replace(" ", "").Replace("-", "");
                var ok = await _userManager.VerifyTwoFactorTokenAsync(
                    u, _userManager.Options.Tokens.AuthenticatorTokenProvider, code!);

                if (!ok) return BadRequest("invalid_code");

                await _userManager.SetTwoFactorEnabledAsync(u, true);
                var codes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(u, 10)).ToArray();
                return Ok(codes);
            }

            [HttpPost("2fa/disable")]
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
            public async Task<IActionResult> DisableTwoFactor()
            {
                var u = await _userManager.GetUserAsync(User);
                if (u == null) return Unauthorized();

                await _userManager.SetTwoFactorEnabledAsync(u, false);
                return NoContent();
            }

            [HttpPost("2fa/recovery-codes")]
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
            public async Task<ActionResult<string[]>> GenerateRecoveryCodes()
            {
                var u = await _userManager.GetUserAsync(User);
                if (u == null) return Unauthorized();

                var codes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(u, 10)).ToArray();
                return Ok(codes);
            }

            // ===== MÉTODOS PRIVADOS =====

            private async Task<string> GenerateJwtTokenAsync(User user)
            {
                var jwtKey = _configuration["Jwt:Key"];
                var jwtIssuer = _configuration["Jwt:Issuer"];
                var jwtAudience = _configuration["Jwt:Audience"];

                if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
                    throw new InvalidOperationException("JWT configuration is incomplete");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var roles = await _userManager.GetRolesAsync(user);

                var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? "")
            };

                foreach (var role in roles)
                    claims.Add(new Claim(ClaimTypes.Role, role));

                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(7),
                    signingCredentials: creds);

                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            private static string QrAsBase64(string content)
            {
                var gen = new QRCodeGenerator();
                using var data = gen.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                var png = new PngByteQRCode(data).GetGraphic(220);
                return Convert.ToBase64String(png);
            }
        }

    }
}
