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
using Microsoft.AspNetCore.Authorization;

namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Administrator,Manager")]
    public class ExpensesController : Controller
    {
        private readonly IExpenseRepository _expenseRepo;
        private readonly ICondominiumRepository _condoRepo;

        public ExpensesController(IExpenseRepository expenseRepo, ICondominiumRepository condoRepo)
        {
            _expenseRepo = expenseRepo;
            _condoRepo = condoRepo;
        }

        public async Task<IActionResult> Index()
        {
            var expenses = await _expenseRepo.GetAllDetailedAsync(); // inclui Condominium
            return View(expenses);
        }

        public async Task<IActionResult> Details(int id)
        {
            var expense = await _expenseRepo.GetByIdDetailedAsync(id);
            if (expense == null) return NotFound();
            return View(expense);
        }

        public async Task<IActionResult> Create()
        {
            await LoadCondominiumsSelectAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense)
        {
            if (ModelState.IsValid)
            {
                await _expenseRepo.AddAsync(expense);
                return RedirectToAction(nameof(Index));
            }
            await LoadCondominiumsSelectAsync(expense.CondominiumId);
            return View(expense);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var expense = await _expenseRepo.GetByIdAsync(id);
            if (expense == null) return NotFound();

            await LoadCondominiumsSelectAsync(expense.CondominiumId);
            return View(expense);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Expense expense)
        {
            if (id != expense.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _expenseRepo.Update(expense);
                await _expenseRepo.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await LoadCondominiumsSelectAsync(expense.CondominiumId);
            return View(expense);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _expenseRepo.GetByIdDetailedAsync(id);
            if (expense == null) return NotFound();
            return View(expense);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _expenseRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCondominiumsSelectAsync(int? selectedId = null)
        {
            var condos = await _condoRepo.GetAllAsync();
            ViewBag.CondominiumId = new SelectList(condos, "Id", "Name", selectedId);
        }
    }
}
