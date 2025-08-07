using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CondoSphere.Controllers
{
    public class CompaniesController : Controller
    {
        private readonly ICompanyRepository _companyRepository;

        public CompaniesController(ICompanyRepository companyRepository)
        {
            _companyRepository = companyRepository;
        }

        // GET: Companies
        public async Task<IActionResult> Index()
        {
            var companies = await _companyRepository.GetAllAsync();
            return View(companies);
        }

        // GET: Companies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Companies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Company company)
        {
            if (ModelState.IsValid)
            {
                await _companyRepository.AddAsync(company);
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        // GET: Companies/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var company = await _companyRepository.GetByIdAsync(id);
            if (company == null)
                return NotFound();

            return View(company);
        }

        // POST: Companies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Company company)
        {
            if (id != company.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                _companyRepository.Update(company);
                await _companyRepository.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(company);
        }

        // GET: Companies/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var company = await _companyRepository.GetByIdAsync(id);
            if (company == null)
                return NotFound();

            return View(company);
        }

        // POST: Companies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _companyRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

