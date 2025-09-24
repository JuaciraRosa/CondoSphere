using CondoSphere.Data.Interfaces;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [ApiController]
    [Route("api/payment-receipts")]
    [AllowAnonymous] 
    public class PaymentReceiptsApiController : ControllerBase
    {
        private readonly IPaymentRepository _payments;
        public PaymentReceiptsApiController(IPaymentRepository payments) => _payments = payments;

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var bytes = await new ReceiptPdfService(_payments).GenerateAsync(id);
            return File(bytes, "application/pdf", $"recibo_{id:D6}.pdf");
        }
    }

}
