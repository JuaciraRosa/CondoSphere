using CondoSphere.Data.Interfaces;
using CondoSphere.Messaging;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize(Roles = "Administrator,Manager")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class NotificationsApiController : ControllerBase
    {
        private readonly IEmailSender _email;
        private readonly HtmlEncoder _enc;

        public NotificationsApiController(IEmailSender email, HtmlEncoder enc)
        {
            _email = email;
            _enc = enc;
        }

        // POST api/notify/payment-received
        [HttpPost("payment-received")]
        public async Task<IActionResult> PaymentReceived([FromBody] PaymentReceivedDto dto)
        {
            var subject = $"Pagamento recebido #{dto.PaymentId}";
            var body = $@"<h3>Pagamento confirmado</h3>
                          <p>Pagamento <strong>#{_enc.Encode(dto.PaymentId.ToString())}</strong> no valor de <strong>{dto.Amount:C}</strong> foi confirmado.</p>";
            await _email.SendAsync(dto.To, subject, body);
            return Ok(new { ok = true });
        }

        // POST api/notify/meeting-doc
        [HttpPost("meeting-doc")]
        public async Task<IActionResult> MeetingDoc([FromBody] MeetingDocDto dto)
        {
            var subject = $"Nova ata disponível — Reunião #{dto.MeetingId}";
            var safeUrl = _enc.Encode(dto.DocumentUrl ?? "#");
            var body = $@"<h3>Documento disponível</h3>
                          <p>Foi publicada a ata da reunião #{_enc.Encode(dto.MeetingId.ToString())}.</p>
                          <p><a href=""{safeUrl}"">Abrir documento</a></p>";
            await _email.SendAsync(dto.To, subject, body);
            return Ok(new { ok = true });
        }

        // POST api/notify/occurrence-status
        [HttpPost("occurrence-status")]
        public async Task<IActionResult> OccurrenceStatus([FromBody] OccurrenceStatusDto dto)
        {
            var subject = $"Ocorrência atualizada — {dto.Status}";
            var body = $@"<h3>Estado atualizado</h3>
                          <p>A sua ocorrência <strong>{_enc.Encode(dto.Title ?? dto.OccurrenceId)}</strong> agora está com estado <strong>{_enc.Encode(dto.Status)}</strong>.</p>";
            await _email.SendAsync(dto.To, subject, body);
            return Ok(new { ok = true });
        }
    }

    public class PaymentReceivedDto
    {
        public string To { get; set; }
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
    }

    public class MeetingDocDto
    {
        public string To { get; set; }
        public int MeetingId { get; set; }
        public string DocumentUrl { get; set; }
    }

    public class OccurrenceStatusDto
    {
        public string To { get; set; }
        public string OccurrenceId { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
    }
}


