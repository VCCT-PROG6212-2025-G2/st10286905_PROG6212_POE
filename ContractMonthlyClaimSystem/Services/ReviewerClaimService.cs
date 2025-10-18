using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Services
{
    public class ReviewerClaimService(
        ApplicationDbContext context,
        IWebHostEnvironment env,
        UserManager<AppUser> userManager,
        IFileEncryptionService encryptionService
    ) : IReviewerClaimService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly IFileEncryptionService _encryptionService = encryptionService;

        public async Task<List<ContractClaim>> GetClaimsAsync() =>
            await _context
                .ContractClaims.Include(c => c.Module)
                .Include(c => c.LecturerUser)
                .Include(c => c.ProgramCoordinatorUser)
                .Include(c => c.AcademicManagerUser)
                .ToListAsync();

        public async Task<ContractClaim?> GetClaimAsync(int claimId) =>
            await _context
                .ContractClaims.Include(c => c.Module)
                .Include(c => c.LecturerUser)
                .Include(c => c.ProgramCoordinatorUser)
                .Include(c => c.AcademicManagerUser)
                .Where(c => c.Id == claimId)
                .FirstOrDefaultAsync();

        public async Task<List<UploadedFile>?> GetClaimFilesAsync(ContractClaim claim) =>
            await (
                from d in _context.ContractClaimsDocuments
                where d.ContractClaimId == claim.Id
                select d.UploadedFile
            ).ToListAsync();

        public async Task<bool> ReviewClaim(
            int claimId,
            string userId,
            bool accept,
            string? comment
        )
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false; // Invalid user
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("ProgramCoordinator") && !roles.Contains("AcademicManager"))
                return false; // User must be either a ProgramCoordinator or AcademicManager to review claims.

            var claim = await _context.ContractClaims.FindAsync(claimId);
            if (claim == null)
                return false; // Invalid claim

            // Prevent changing someone elses review
            if (
                (
                    roles.Contains("ProgramCoordinator")
                    && claim.ProgramCoordinatorUserId != null
                    && claim.ProgramCoordinatorUserId != user.Id
                )
                || (
                    roles.Contains("AcademicManager")
                    && claim.AcademicManagerUserId != null
                    && claim.AcademicManagerUserId != user.Id
                )
            )
                return false;

            if (roles.Contains("ProgramCoordinator"))
            { // User is ProgramCoordinator, so set ProgramCoordinator values
                claim.ProgramCoordinatorUserId = user.Id;
                claim.ProgramCoordinatorDecision = accept
                    ? ClaimDecision.ACCEPTED
                    : ClaimDecision.REJECTED;
                claim.ProgramCoordinatorComment = comment;
            }
            if (roles.Contains("AcademicManager"))
            { // User is AcademicManager, so set AcademicManager values
                claim.AcademicManagerUserId = user.Id;
                claim.AcademicManagerDecision = accept
                    ? ClaimDecision.ACCEPTED
                    : ClaimDecision.REJECTED;
                claim.AcademicManagerComment = comment;
            }
            // Technically a user could hypothetically be both roles,
            // in which case both roles' values will be set.

            if (
                claim.ProgramCoordinatorDecision == ClaimDecision.PENDING
                || claim.AcademicManagerDecision == ClaimDecision.PENDING
            ) // If either decision is still pending, update status to PENDING_CONFIRM
                claim.ClaimStatus = ClaimStatus.PENDING_CONFIRM;
            else // Otherwise update status to whether both accepted or not
                claim.ClaimStatus =
                    claim.ProgramCoordinatorDecision == ClaimDecision.ACCEPTED
                    && claim.AcademicManagerDecision == ClaimDecision.ACCEPTED
                        ? ClaimStatus.ACCEPTED
                        : ClaimStatus.REJECTED;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<(
            string FileName,
            MemoryStream FileStream,
            string ContentType
        )?> GetFileAsync(int fileId)
        {
            var file = await _context.UploadedFiles.FindAsync(fileId);
            if (file == null)
                return null;

            var filePath = Path.Combine(_env.WebRootPath, file.FilePath);
            if (!System.IO.File.Exists(filePath))
                return null;

            var output = new MemoryStream();
            await _encryptionService.DecryptToStreamAsync(filePath, output);
            output.Position = 0;

            var contentType = "application/octet-stream";

            return (file.FileName, output, contentType);
        }
    }
}
