// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68c1723e-d2a8-800b-93c7-41da82b21c0e
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class ManageLecturerModulesController(IUserService userService, IModuleService moduleService)
        : Controller
    {
        private readonly IUserService _userService = userService;
        private readonly IModuleService _moduleService = moduleService;

        // Show lecturers and all modules
        public async Task<IActionResult> Index()
        {
            var lecturers = await _userService.GetUsersInRoleAsync("Lecturer");

            var lecturerVMs = new List<LecturerViewModel>();
            foreach (var lecturer in lecturers)
            {
                lecturerVMs.Add(
                    new LecturerViewModel
                    {
                        Id = lecturer.Id,
                        Name = $"{lecturer.FirstName} {lecturer.LastName}",
                        Modules = await _moduleService.GetModulesForLecturerAsync(lecturer.Id),
                    }
                );
            }

            var allModules = await _moduleService.GetModulesAsync();

            var viewModel = new ManageModulesIndexViewModel
            {
                Lecturers = lecturerVMs,
                Modules = allModules,
                NewModule = new Module(),
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateModule(ManageModulesIndexViewModel model)
        {
            await _moduleService.AddModuleAsync(model.NewModule);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteModule(int id)
        {
            await _moduleService.RemoveModuleAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // Show all modules for a specific lecturer
        public async Task<IActionResult> ManageLecturerModules(int id)
        {
            var lecturer = await _userService.GetUserAsync(id);
            if (lecturer == null)
                return NotFound();

            var allModules = await _moduleService.GetModulesAsync();
            var lecturerModules = await _moduleService.GetLecturerModulesAsync(lecturer.Id);

            var viewModel = new ManageLecturerModulesViewModel
            {
                LecturerId = id,
                LecturerName = $"{lecturer.FirstName} {lecturer.LastName}",
                AllModules = allModules,
                AssignedModuleIds = [.. lecturerModules.Select(lm => lm.ModuleId)],
                AssignedModulesDetailed =
                [
                    .. lecturerModules.Select(lm => new AssignedLecturerModuleViewModel
                    {
                        ModuleId = lm.ModuleId,
                        ModuleName = lm.Module.Name,
                        HourlyRate = lm.HourlyRate,
                    }),
                ],
            };

            return View(viewModel);
        }

        // Assign a module
        [HttpPost]
        public async Task<IActionResult> AddLecturerModule(
            int lecturerId,
            int moduleId,
            decimal hourlyRate
        )
        {
            await _moduleService.AddLecturerModuleAsync(lecturerId, moduleId, hourlyRate);
            return RedirectToAction(nameof(ManageLecturerModules), new { id = lecturerId });
        }

        // Remove a module
        [HttpPost]
        public async Task<IActionResult> RemoveLecturerModule(int lecturerId, int moduleId)
        {
            await _moduleService.RemoveLecturerModuleAsync(lecturerId, moduleId);
            return RedirectToAction(nameof(ManageLecturerModules), new { id = lecturerId });
        }

        // Update a lecturer module's hourly rate
        [HttpPost]
        public async Task<IActionResult> UpdateLecturerModuleHourlyRate(
            int lecturerId,
            int moduleId,
            decimal hourlyRate
        )
        {
            await _moduleService.UpdateLecturerModuleHourlyRate(lecturerId, moduleId, hourlyRate);
            return RedirectToAction(nameof(ManageLecturerModules), new { id = lecturerId });
        }
    }
}
