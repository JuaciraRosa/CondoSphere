using CondoSphere.Data.Interfaces;
using CondoSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.Controllers
{
    [Authorize]
    public class PaymentReceiptsController : Controller
    {
        private readonly IPaymentRepository _payments;
        public PaymentReceiptsController(IPaymentRepository payments)
        {
            _payments = payments;
        }

        // GET /PaymentReceipts/Download/{id}
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var bytes = await new ReceiptPdfService(_payments).GenerateAsync(id);
            return File(bytes, "application/pdf", $"recibo_{id:D6}.pdf");
        }
    }
}
