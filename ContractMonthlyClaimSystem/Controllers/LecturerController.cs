using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerController(
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
            // Get lecturer user from HttpContext. Ref: https://stackoverflow.com/a/42493106
            var lecturer = await _userManager.GetUserAsync(HttpContext.User);

            var claims = await _context
                .ContractClaims.Include(c => c.Module)
                .Where(c => c.LecturerUserId == lecturer.Id)
                .ToListAsync();

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
            var modules = await (
                from lm in _context.LecturerModules
                where lm.LecturerUserId == lecturer.Id
                select lm.Module
            ).ToListAsync();

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
                model.Modules = await (
                    from lm in _context.LecturerModules
                    where lm.LecturerUserId == lecturer.Id
                    select lm.Module
                ).ToListAsync();
                return View(model);
            }

            var claim = new ContractClaim
            {
                LecturerUserId = lecturer.Id,
                ModuleId = model.ModuleId,
                HoursWorked = model.HoursWorked,
                HourlyRate = model.HourlyRate,
                LecturerComment = model.LecturerComment,
            };

            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            // AI Disclaimer: I made use of ChatGPT to assist with upload functionality. Link: https://chatgpt.com/share/68cac0b4-34b8-800b-b47c-a65ef55ad8e5
            if (model.Files != null && model.Files.Any())
            {
                foreach (var file in model.Files)
                {
                    if (file.Length > 0)
                    {
                        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                        Directory.CreateDirectory(uploadsDir);

                        // Ensure file name is unique and sanitized/secure
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadsDir, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await file.CopyToAsync(stream);

                        var uploadedFile = new UploadedFile
                        {
                            FileName = file.FileName,
                            FilePath = $"uploads/{uniqueFileName}",
                            FileSize = file.Length,
                            UploadedOn = DateTime.Now,
                        };
                        _context.UploadedFiles.Add(uploadedFile);
                        await _context.SaveChangesAsync();

                        var claimDoc = new ContractClaimDocument
                        {
                            ContractClaimId = claim.Id,
                            UploadedFileId = uploadedFile.Id,
                        };
                        _context.ContractClaimsDocuments.Add(claimDoc);
                    }
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ClaimDetails(int id)
        {
            var lecturer = await _userManager.GetUserAsync(HttpContext.User);

            var claim = await _context
                .ContractClaims.Include(c => c.Module)
                .FirstOrDefaultAsync(c => c.Id == id && c.LecturerUserId == lecturer.Id);

            if (claim == null)
                return NotFound();

            var files = await (
                from d in _context.ContractClaimsDocuments
                where d.ContractClaimId == claim.Id
                select d.UploadedFile
            ).ToListAsync();

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

            var file = await (
                from d in _context.ContractClaimsDocuments
                where d.ContractClaim.LecturerUserId == lecturer.Id && d.UploadedFileId == id
                select d.UploadedFile
            ).FirstOrDefaultAsync();
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
