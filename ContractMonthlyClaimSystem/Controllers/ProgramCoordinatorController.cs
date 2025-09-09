using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "ProgramCoordinator")]
    public class ProgramCoordinatorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ProgramCoordinatorController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ManageLecturers()
        {
            var lecturers = await _userManager.GetUsersInRoleAsync("Lecturer");

            List<LecturerViewModel> lecturerViewModels = lecturers.Select(
                lecturer => new LecturerViewModel
                {
                    Id = lecturer.Id,
                    Name = lecturer.UserName,
                    Modules = [.. from lm in _context.LecturerModules where lm.LecturerUserId == lecturer.Id select lm.Module]
                }).ToList();
            return View(lecturerViewModels);
        }
    }
}
