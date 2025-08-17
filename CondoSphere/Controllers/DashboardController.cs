using CondoSphere.Data.Interfaces;
using CondoSphere.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ICompanyRepository _companyRepo;
        private readonly ICondominiumRepository _condoRepo;
        private readonly IUserRepository _userRepo;

        public DashboardController(ICompanyRepository companyRepo, ICondominiumRepository condoRepo, IUserRepository userRepo)
        {
            _companyRepo = companyRepo;
            _condoRepo = condoRepo;
            _userRepo = userRepo;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                TotalCompanies = (await _companyRepo.GetAllAsync()).Count(),
                TotalCondominiums = (await _condoRepo.GetAllAsync()).Count(),
                TotalUsers = (await _userRepo.GetAllAsync()).Count()
            };
            return View(model);
        }
    }
}
