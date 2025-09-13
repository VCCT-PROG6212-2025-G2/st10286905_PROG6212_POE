using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "AcademicManager")]
    public class AcademicManagerController(
        ApplicationDbContext context,
        UserManager<AppUser> userManager,
        IWebHostEnvironment env
    ) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly IWebHostEnvironment _env = env;

        public async Task<IActionResult> Index()
        {
            var claims = await _context
                .ContractClaims.Include(c => c.Module)
                .Include(c => c.LecturerUser)
                .Include(c => c.ProgramCoordinatorUser)
                .Include(c => c.AcademicManagerUser)
                .ToListAsync();

            var vm = new ReviewerClaimsViewModel
            {
                PendingClaims =
                [
                    .. claims
                        .Where(c =>
                            c.ClaimStatus <= ClaimStatus.PENDING_CONFIRM
                            && c.AcademicManagerUserId == null
                        )
                        .Select(c => new ReviewerClaimRowViewModel
                        {
                            Id = c.Id,
                            LecturerName = c.LecturerUser.UserName,
                            ModuleName = c.Module.Name,
                            HoursWorked = c.HoursWorked,
                            HourlyRate = c.HourlyRate,
                            PaymentAmount = c.HoursWorked * c.HourlyRate,
                            LecturerComment = c.LecturerComment,
                            ProgramCoordinatorName = c.ProgramCoordinatorUser?.UserName,
                            ProgramCoordinatorAccepted = c.ProgramCoordinatorAccepted,
                            ProgramCoordinatorComment = c.ProgramCoordinatorComment,
                            AcademicManagerName = c.AcademicManagerUser?.UserName,
                            AcademicManagerAccepted = c.AcademicManagerAccepted,
                            AcademicManagerComment = c.AcademicManagerComment,
                            ClaimStatus = c.ClaimStatus,
                        }),
                ],
                PendingConfirmClaims =
                [
                    .. claims
                        .Where(c =>
                            c.ClaimStatus <= ClaimStatus.PENDING_CONFIRM
                            && c.AcademicManagerUserId != null
                        )
                        .Select(c => new ReviewerClaimRowViewModel
                        {
                            Id = c.Id,
                            LecturerName = c.LecturerUser.UserName,
                            ModuleName = c.Module.Name,
                            HoursWorked = c.HoursWorked,
                            HourlyRate = c.HourlyRate,
                            PaymentAmount = c.HoursWorked * c.HourlyRate,
                            LecturerComment = c.LecturerComment,
                            ProgramCoordinatorName = c.ProgramCoordinatorUser?.UserName,
                            ProgramCoordinatorAccepted = c.ProgramCoordinatorAccepted,
                            ProgramCoordinatorComment = c.ProgramCoordinatorComment,
                            AcademicManagerName = c.AcademicManagerUser?.UserName,
                            AcademicManagerAccepted = c.AcademicManagerAccepted,
                            AcademicManagerComment = c.AcademicManagerComment,
                            ClaimStatus = c.ClaimStatus,
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
                            HoursWorked = c.HoursWorked,
                            HourlyRate = c.HourlyRate,
                            PaymentAmount = c.HoursWorked * c.HourlyRate,
                            LecturerComment = c.LecturerComment,
                            ProgramCoordinatorName = c.ProgramCoordinatorUser?.UserName,
                            ProgramCoordinatorAccepted = c.ProgramCoordinatorAccepted,
                            ProgramCoordinatorComment = c.ProgramCoordinatorComment,
                            AcademicManagerName = c.AcademicManagerUser?.UserName,
                            AcademicManagerAccepted = c.AcademicManagerAccepted,
                            AcademicManagerComment = c.AcademicManagerComment,
                            ClaimStatus = c.ClaimStatus,
                        }),
                ],
            };
            return View(vm);
        }

        public async Task<IActionResult> ClaimDetails(int id)
        {
            var claim = await _context
                .ContractClaims.Include(c => c.Module)
                .Include(c => c.LecturerUser)
                .Include(c => c.ProgramCoordinatorUser)
                .Include(c => c.AcademicManagerUser)
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync();

            if (claim == null)
                return NotFound();

            var files = await (
                from d in _context.ContractClaimsDocuments
                where d.ContractClaimId == claim.Id
                select d.UploadedFile
            ).ToListAsync();

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
                ProgramCoordinatorAccepted = claim.ProgramCoordinatorAccepted,
                ProgramCoordinatorComment = claim.ProgramCoordinatorComment,
                AcademicManagerName = claim.AcademicManagerUser?.UserName,
                AcademicManagerAccepted = claim.AcademicManagerAccepted,
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

            var claim = _context.ContractClaims.Find(id);
            if (claim == null)
                return RedirectToAction(nameof(Index)); // Invalid claim

            if (claim.AcademicManagerUserId != null && claim.AcademicManagerUserId != user.Id)
                return RedirectToAction(nameof(Index)); // Prevent changing someone elses review

            claim.AcademicManagerUserId = user.Id;
            claim.AcademicManagerAccepted = true;
            claim.AcademicManagerComment = comment;

            if (claim.ProgramCoordinatorAccepted == null || claim.AcademicManagerAccepted == null)
                claim.ClaimStatus = ClaimStatus.PENDING_CONFIRM;
            else
                claim.ClaimStatus =
                    (claim.ProgramCoordinatorAccepted ?? false)
                    && (claim.AcademicManagerAccepted ?? false)
                        ? ClaimStatus.ACCEPTED
                        : ClaimStatus.REJECTED;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int id, string? comment)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            var claim = _context.ContractClaims.Find(id);
            if (claim == null)
                return RedirectToAction(nameof(Index)); // Invalid claim

            if (claim.AcademicManagerUserId != null && claim.AcademicManagerUserId != user.Id)
                return RedirectToAction(nameof(Index)); // Prevent changing someone elses review

            claim.AcademicManagerUserId = user.Id;
            claim.AcademicManagerAccepted = false;
            claim.AcademicManagerComment = comment;

            if (claim.ProgramCoordinatorAccepted == null || claim.AcademicManagerAccepted == null)
                claim.ClaimStatus = ClaimStatus.PENDING_CONFIRM;
            else
                claim.ClaimStatus =
                    (claim.ProgramCoordinatorAccepted ?? false)
                    && (claim.AcademicManagerAccepted ?? false)
                        ? ClaimStatus.ACCEPTED
                        : ClaimStatus.REJECTED;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _context.UploadedFiles.FindAsync(id);
            if (file == null)
                return NotFound();

            var filePath = Path.Combine(_env.WebRootPath, file.FilePath);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var contentType = "application/octet-stream";
            return PhysicalFile(filePath, contentType, file.FileName);
        }
    }
}
