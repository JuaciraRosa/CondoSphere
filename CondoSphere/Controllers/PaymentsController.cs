using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CondoSphere.Data;
using CondoSphere.Models;
using CondoSphere.Data.Interfaces;

namespace CondoSphere.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly IPaymentRepository _paymentRepository;

        public PaymentsController(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<IActionResult> Index()
        {
            var payments = await _paymentRepository.GetAllAsync();
            return View(payments);
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            return View(payment);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Payment payment)
        {
            if (ModelState.IsValid)
            {
                await _paymentRepository.AddAsync(payment);
                return RedirectToAction(nameof(Index));
            }
            return View(payment);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null) return NotFound();
            return View(payment);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Payment payment)
        {
            if (id != payment.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _paymentRepository.Update(payment);
                await _paymentRepository.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(payment);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null) return NotFound();
            return View(payment);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _paymentRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
