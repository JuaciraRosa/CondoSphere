using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CondoSphere.Controllers
{
    [Authorize(Roles = "Administrator,Manager")]
    public class CompaniesController : Controller
    {
        private readonly ICompanyRepository _companyRepo;

        public CompaniesController(ICompanyRepository companyRepo)
        {
            _companyRepo = companyRepo;
        }

        // GET: /Companies
        public async Task<IActionResult> Index()
        {
            var companies = await _companyRepo.GetAllAsync(); // lista simples
            return View(companies);
        }

        // GET: /Companies/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var company = await _companyRepo.GetWithCondominiumsAsync(id); // inclui Condominiums
            if (company == null) return NotFound();
            return View(company);
        }

        // GET: /Companies/Create
        public IActionResult Create() => View();

        // POST: /Companies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Company company)
        {
            if (!ModelState.IsValid) return View(company);

            await _companyRepo.AddAsync(company);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Companies/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var company = await _companyRepo.GetByIdAsync(id);
            if (company == null) return NotFound();
            return View(company);
        }

        // POST: /Companies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Company company)
        {
            if (id != company.Id) return NotFound();
            if (!ModelState.IsValid) return View(company);

            _companyRepo.Update(company);
            await _companyRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Companies/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var company = await _companyRepo.GetByIdAsync(id);
            if (company == null) return NotFound();
            return View(company);
        }

        // POST: /Companies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _companyRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

