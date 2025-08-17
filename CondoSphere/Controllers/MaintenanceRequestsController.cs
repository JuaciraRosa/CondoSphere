using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.Controllers
{
    public class MaintenanceRequestsController : Controller
    {
        private readonly IMaintenanceRequestRepository _repository;

        public MaintenanceRequestsController(IMaintenanceRequestRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _repository.GetAllAsync();
            return View(requests);
        }

        // GET: MaintenanceRequests/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var request = await _repository.GetByIdAsync(id);
            if (request == null)
                return NotFound();

            return View(request);
        }


        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(MaintenanceRequest request)
        {
            if (ModelState.IsValid)
            {
                await _repository.AddAsync(request);
                return RedirectToAction(nameof(Index));
            }
            return View(request);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var request = await _repository.GetByIdAsync(id);
            if (request == null) return NotFound();
            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, MaintenanceRequest request)
        {
            if (id != request.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _repository.Update(request);
                await _repository.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(request);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var request = await _repository.GetByIdAsync(id);
            if (request == null) return NotFound();
            return View(request);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _repository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
