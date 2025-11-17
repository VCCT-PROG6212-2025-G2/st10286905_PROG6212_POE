using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerController(
        ILecturerClaimService claimService,
        IUserService userService
    ) : Controller
    {
        private readonly ILecturerClaimService _claimService = claimService;
        private readonly IUserService _userService = userService;

        public async Task<IActionResult> Index()
        {
            var lecturer = await _userService.GetUserAsync(User.Identity?.Name);

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
            var lecturer = await _userService.GetUserAsync(User.Identity?.Name);

            // Get modules taught by lecturer.
            var modules = await _claimService.GetModulesForLecturerAsync(lecturer.Id);

            var vm = new CreateClaimViewModel { Modules = modules };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(CreateClaimViewModel model)
        {
            var lecturer = await _userService.GetUserAsync(User.Identity?.Name);

            if (!ModelState.IsValid)
            {
                // Repopulate modules
                model.Modules = await _claimService.GetModulesForLecturerAsync(lecturer.Id);
                return View(model);
            }

            // Validate uploaded files
            // AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/s/t_68f4c1d5f7e08191b41d09967a63a506
            long maxFileSize = 10 * 1024 * 1024; // 10 MB
            string[] permittedExtensions = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".md"];

            if (model.Files is not null && model.Files.Count > 0)
            {
                foreach (var file in model.Files)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("Files", $"File '{file.FileName}' has an unsupported type.");
                        continue;
                    }

                    if (file.Length > maxFileSize)
                    {
                        ModelState.AddModelError("Files", $"File '{file.FileName}' exceeds the 10 MB size limit.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    model.Modules = await _claimService.GetModulesForLecturerAsync(lecturer.Id);
                    return View(model);
                }
            }

            var claim = await _claimService.CreateClaimAsync(lecturer.Id, model);
            await _claimService.AddFilesToClaimAsync(claim, model.Files);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ClaimDetails(int id)
        {
            var lecturer = await _userService.GetUserAsync(User.Identity?.Name);

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
            var lecturer = await _userService.GetUserAsync(User.Identity?.Name);

            var file = await _claimService.GetFileAsync(id, lecturer.Id); 
            if (file == null)
                return NotFound();

            return File(file.Value.FileStream, file.Value.ContentType, file.Value.FileName);
        }
    }
}
