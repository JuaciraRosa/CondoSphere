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
    public class MeetingsController : Controller
    {
        private readonly IMeetingRepository _meetingRepo;

        public MeetingsController(IMeetingRepository meetingRepo)
        {
            _meetingRepo = meetingRepo;
        }

        public async Task<IActionResult> Index()
        {
            var meetings = await _meetingRepo.GetAllAsync();
            return View(meetings);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Meeting meeting)
        {
            if (ModelState.IsValid)
            {
                await _meetingRepo.AddAsync(meeting);
                return RedirectToAction(nameof(Index));
            }
            return View(meeting);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var meeting = await _meetingRepo.GetByIdAsync(id);
            if (meeting == null) return NotFound();
            return View(meeting);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Meeting meeting)
        {
            if (id != meeting.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _meetingRepo.Update(meeting);
                return RedirectToAction(nameof(Index));
            }
            return View(meeting);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var meeting = await _meetingRepo.GetByIdAsync(id);
            if (meeting == null) return NotFound();
            return View(meeting);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _meetingRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
