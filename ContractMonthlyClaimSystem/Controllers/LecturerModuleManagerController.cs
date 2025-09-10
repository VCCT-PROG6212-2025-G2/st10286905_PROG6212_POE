// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68c1723e-d2a8-800b-93c7-41da82b21c0e
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "Admin,ProgramCoordinator")]
    public class LecturerModuleManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly LecturerService _lecturerService;

        public LecturerModuleManagerController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            LecturerService lecturerService)
        {
            _context = context;
            _userManager = userManager;
            _lecturerService = lecturerService;
        }

        // Show lecturers and all modules
        public async Task<IActionResult> Index()
        {
            var lecturers = await _userManager.GetUsersInRoleAsync("Lecturer");

            var lecturerVMs = new List<LecturerViewModel>();
            foreach (var lecturer in lecturers)
            {
                lecturerVMs.Add(new LecturerViewModel
                {
                    Id = lecturer.Id,
                    Name = lecturer.UserName!,
                    Modules = await _lecturerService.GetModulesAsync(lecturer.Id)
                });
            }

            var allModules = await _context.Modules.ToListAsync();

            var viewModel = new LecturerModuleManagerIndexViewModel
            {
                Lecturers = lecturerVMs,
                Modules = allModules,
                NewModule = new Module()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateModule(LecturerModuleManagerIndexViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.NewModule.Name) && !string.IsNullOrWhiteSpace(model.NewModule.Code))
            {
                _context.Modules.Add(new Module { Name = model.NewModule.Name, Code = model.NewModule.Code });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var module = await _context.Modules.FindAsync(id);
            if (module != null)
            {
                _context.Modules.Remove(module);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Show all modules for a specific lecturer
        public async Task<IActionResult> ManageLecturerModules(string id)
        {
            var lecturer = await _userManager.FindByIdAsync(id);
            if (lecturer == null) return NotFound();

            var allModules = _context.Modules.ToList();
            var assignedModules = await _lecturerService.GetModulesAsync(id);

            var viewModel = new ManageLecturerModulesViewModel
            {
                LecturerId = id,
                LecturerName = lecturer.UserName!,
                AllModules = allModules,
                AssignedModuleIds = assignedModules.Select(m => m.Id).ToList()
            };

            return View(viewModel);
        }

        // Assign a module
        [HttpPost]
        public async Task<IActionResult> AddLecturerModule(string lecturerId, int moduleId)
        {
            await _lecturerService.AddModuleAsync(lecturerId, moduleId);
            return RedirectToAction(nameof(ManageLecturerModules), new { id = lecturerId });
        }

        // Remove a module
        [HttpPost]
        public async Task<IActionResult> RemoveLecturerModule(string lecturerId, int moduleId)
        {
            await _lecturerService.RemoveModuleAsync(lecturerId, moduleId);
            return RedirectToAction(nameof(ManageLecturerModules), new { id = lecturerId });
        }
    }
}
