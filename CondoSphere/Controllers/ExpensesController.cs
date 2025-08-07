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
    public class ExpensesController : Controller
    {
        private readonly IExpenseRepository _expenseRepo;

        public ExpensesController(IExpenseRepository expenseRepo)
        {
            _expenseRepo = expenseRepo;
        }

        public async Task<IActionResult> Index()
        {
            var expenses = await _expenseRepo.GetAllAsync();
            return View(expenses);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Expense expense)
        {
            if (ModelState.IsValid)
            {
                await _expenseRepo.AddAsync(expense);
                return RedirectToAction(nameof(Index));
            }
            return View(expense);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var expense = await _expenseRepo.GetByIdAsync(id);
            if (expense == null) return NotFound();
            return View(expense);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Expense expense)
        {
            if (id != expense.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _expenseRepo.Update(expense);
                await _expenseRepo.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(expense);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _expenseRepo.GetByIdAsync(id);
            if (expense == null) return NotFound();
            return View(expense);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _expenseRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
