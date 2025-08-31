using CondoSphere.Data.Interfaces;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [Route("api/payment-receipts")]
    [ApiController]
    [Authorize]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PaymentReceiptsApiController : ControllerBase
    {
        private readonly IPaymentRepository _payments;
        public PaymentReceiptsApiController(IPaymentRepository payments)
        {
            _payments = payments;
        }

        // GET api/payment-receipts/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var bytes = await new ReceiptPdfService(_payments).GenerateAsync(id);
            return File(bytes, "application/pdf", $"recibo_{id:D6}.pdf");
        }
    }
}
