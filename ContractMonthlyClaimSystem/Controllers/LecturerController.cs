using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerController(
        ILecturerClaimService claimService,
        UserManager<AppUser> userManager
    ) : Controller
    {
        private readonly ILecturerClaimService _claimService = claimService;
        private readonly UserManager<AppUser> _userManager = userManager;

        public async Task<IActionResult> Index()
        {
            // Get lecturer user from HttpContext. Ref: https://stackoverflow.com/a/42493106
            var lecturer = await _userManager.GetUserAsync(HttpContext.User);

            var claims = await _claimService.GetClaimsForLecturerAsync(lecturer.Id);

            var vm = new LecturerClaimsViewModel
            {
                PendingClaims =
                [
                    .. claims
                        .Where(c => c.ClaimStatus <= ClaimStatus.PENDING_CONFIRM)
                        .Select(c => new LecturerClaimRowViewModel
                        {
                            Id = c.Id,
                            ModuleName = c.Module.Name,
                            PaymentAmount = c.HoursWorked * c.HourlyRate,
                            ProgramCoordinatorDecision = c.ProgramCoordinatorDecision,
                            AcademicManagerDecision = c.AcademicManagerDecision,
                            ClaimStatus = c.ClaimStatus
                        }),
                ],

                CompletedClaims =
                [
                    .. claims
                        .Where(c => c.ClaimStatus > ClaimStatus.PENDING_CONFIRM)
                        .Select(c => new LecturerClaimRowViewModel
                        {
                            Id = c.Id,
                            ModuleName = c.Module.Name,
                            PaymentAmount = c.HoursWorked * c.HourlyRate,
                            ProgramCoordinatorDecision = c.ProgramCoordinatorDecision,
                            AcademicManagerDecision = c.AcademicManagerDecision,
                            ClaimStatus = c.ClaimStatus,
                        }),
                ],
            };
            return View(vm);
        }

        public async Task<IActionResult> CreateClaim()
        {
            var lecturer = await _userManager.GetUserAsync(HttpContext.User);

            // Get modules taught by lecturer.
            var modules = await _claimService.GetModulesForLecturerAsync(lecturer.Id);

            var vm = new CreateClaimViewModel { Modules = modules };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(CreateClaimViewModel model)
        {
            var lecturer = await _userManager.GetUserAsync(HttpContext.User);

            if (!ModelState.IsValid)
            {
                // Repopulate modules
                model.Modules = await _claimService.GetModulesForLecturerAsync(lecturer.Id);
                return View(model);
            }

            var claim = await _claimService.CreateClaimAsync(lecturer.Id, model);
            await _claimService.AddFilesToClaimAsync(claim, model.Files);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ClaimDetails(int id)
        {
            var lecturer = await _userManager.GetUserAsync(HttpContext.User);

            var claim = await _claimService.GetClaimAsync(id, lecturer.Id);

            if (claim == null)
                return NotFound();

            var files = await _claimService.GetClaimFilesAsync(claim);

            var vm = new LecturerClaimDetailsViewModel
            {
                Id = claim.Id,
                ModuleName = claim.Module.Name,
                HoursWorked = claim.HoursWorked,
                HourlyRate = claim.HourlyRate,
                PaymentAmount = claim.HoursWorked * claim.HourlyRate,
                LecturerComment = claim.LecturerComment,
                ProgramCoordinatorDecision = claim.ProgramCoordinatorDecision,
                ProgramCoordinatorComment = claim.ProgramCoordinatorComment,
                AcademicManagerDecision = claim.AcademicManagerDecision,
                AcademicManagerComment = claim.AcademicManagerComment,
                ClaimStatus = claim.ClaimStatus,
                Files = files,
            };
            return View(vm);
        }

        public async Task<IActionResult> DownloadFile(int id)
        {
            var lecturer = await _userManager.GetUserAsync(HttpContext.User);

            var file = await _claimService.GetFileAsync(id, lecturer.Id); 
            if (file == null)
                return NotFound();

            return PhysicalFile(file.Value.FilePath, file.Value.ContentType, file.Value.FileName);
        }
    }
}
