using CondoSphere.API.Models.Dtos;
using CondoSphere.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/2fa")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TwoFactorApiController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public TwoFactorApiController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("bootstrap")]
        public async Task<ActionResult<BootstrapDto>> Bootstrap()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            var enabled = await _userManager.GetTwoFactorEnabledAsync(user);

            // chave do autenticador (gera se não existir)
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

            // QR (data URL)
            var qrDataUrl = GenerateQrPngDataUrl(otpauth);

            return Ok(new BootstrapDto
            {
                Enabled = enabled,
                Key = key,
                KeyFormatted = FormatKey(key),
                OtpAuthUri = otpauth,
                QrPngDataUrl = qrDataUrl
            });
        }

        [HttpPost("enable")]
        public async Task<IActionResult> Enable([FromBody] CodeDto dto)
        {
            var user = await _userManager.FindByIdAsync(UserId);
            var code = (dto?.Code ?? "").Replace(" ", "").Replace("-", "");
            var ok = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

            if (!ok) return BadRequest(new { error = "invalid_code" });

            await _userManager.SetTwoFactorEnabledAsync(user, true);

            // gera 10 códigos e devolve
            var codes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ToArray();
            return Ok(new { recoveryCodes = codes });
        }

        [HttpPost("disable")]
        public async Task<IActionResult> Disable()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            return Ok(new { ok = true });
        }

        [HttpPost("recovery/generate")]
        public async Task<IActionResult> GenerateRecovery()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (!await _userManager.GetTwoFactorEnabledAsync(user))
                return BadRequest(new { error = "2fa_not_enabled" });

            var codes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ToArray();
            return Ok(new { recoveryCodes = codes });
        }

        // helpers
        private static string GenerateQrPngDataUrl(string content)
        {
            var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var pngQr = new PngByteQRCode(data);
            var bytes = pngQr.GetGraphic(220);
            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }
        private static string FormatKey(string key) =>
            Regex.Replace(key.ToUpperInvariant(), ".{4}", "$0 ").Trim();

      
    }
}
