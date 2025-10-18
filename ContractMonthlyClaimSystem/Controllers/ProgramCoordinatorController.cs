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
    [Authorize(Roles = "ProgramCoordinator")]
    public class ProgramCoordinatorController(
        IReviewerClaimService reviewerClaimService,
        UserManager<AppUser> userManager
    ) : Controller
    {
        private readonly IReviewerClaimService _reviewerClaimService = reviewerClaimService;
        private readonly UserManager<AppUser> _userManager = userManager;

        public async Task<IActionResult> Index()
        {
            var claims = await _reviewerClaimService.GetClaimsAsync();

            var vm = new ReviewerClaimsViewModel
            {
                PendingClaims =
                [
                    .. claims
                        .Where(c =>
                            c.ClaimStatus <= ClaimStatus.PENDING_CONFIRM
                            && c.ProgramCoordinatorUserId == null
                        )
                        .Select(c => new ReviewerClaimRowViewModel
                        {
                            Id = c.Id,
                            LecturerName = c.LecturerUser.UserName,
                            ModuleName = c.Module.Name,
                            PaymentAmount = c.HoursWorked * c.HourlyRate,
                            ProgramCoordinatorDecision = c.ProgramCoordinatorDecision,
                            AcademicManagerDecision = c.AcademicManagerDecision,
                            ClaimStatus = c.ClaimStatus
                        }),
                ],
                PendingConfirmClaims =
                [
                    .. claims
                        .Where(c =>
                            c.ClaimStatus <= ClaimStatus.PENDING_CONFIRM
                            && c.ProgramCoordinatorUserId != null
                        )
                        .Select(c => new ReviewerClaimRowViewModel
                        {
                            Id = c.Id,
                            LecturerName = c.LecturerUser.UserName,
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
                        .Select(c => new ReviewerClaimRowViewModel
                        {
                            Id = c.Id,
                            LecturerName = c.LecturerUser.UserName,
                            ModuleName = c.Module.Name,
                            PaymentAmount = c.HoursWorked * c.HourlyRate,
                            ProgramCoordinatorDecision = c.ProgramCoordinatorDecision,
                            AcademicManagerDecision = c.AcademicManagerDecision,
                            ClaimStatus = c.ClaimStatus
                        }),
                ],
            };
            return View(vm);
        }

        public async Task<IActionResult> ClaimDetails(int id)
        {
            var claim = await _reviewerClaimService.GetClaimAsync(id);

            if (claim == null)
                return NotFound();

            var files = await _reviewerClaimService.GetClaimFilesAsync(claim);

            var vm = new ReviewerClaimDetailsViewModel
            {
                Id = claim.Id,
                LecturerName = claim.LecturerUser.UserName,
                ModuleName = claim.Module.Name,
                HoursWorked = claim.HoursWorked,
                HourlyRate = claim.HourlyRate,
                PaymentAmount = claim.HoursWorked * claim.HourlyRate,
                LecturerComment = claim.LecturerComment,
                ProgramCoordinatorName = claim.ProgramCoordinatorUser?.UserName,
                ProgramCoordinatorDecision = claim.ProgramCoordinatorDecision,
                ProgramCoordinatorComment = claim.ProgramCoordinatorComment,
                AcademicManagerName = claim.AcademicManagerUser?.UserName,
                AcademicManagerDecision = claim.AcademicManagerDecision,
                AcademicManagerComment = claim.AcademicManagerComment,
                ClaimStatus = claim.ClaimStatus,
                Files = files,
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptClaim(int id, string? comment)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            var res = await _reviewerClaimService.ReviewClaim(id, user.Id, accept: true, comment);
            if (res == false)
                return RedirectToAction(nameof(Index)); // Invalid claim

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int id, string? comment)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            var res = await _reviewerClaimService.ReviewClaim(id, user.Id, accept: false, comment);
            if (res == false)
                return RedirectToAction(nameof(Index)); // Invalid claim

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _reviewerClaimService.GetFileAsync(id);
            if (file == null)
                return NotFound();

            return File(file.Value.FileStream, file.Value.ContentType, file.Value.FileName);
        }
    }
}
