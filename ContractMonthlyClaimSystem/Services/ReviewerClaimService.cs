using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Services
{
    public class ReviewerClaimService(
        AppDbContext context,
        IUserService userService,
        IFileService fileService
    ) : IReviewerClaimService
    {
        private readonly AppDbContext _context = context;
        private readonly IUserService _userService = userService;
        private readonly IFileService _fileService = fileService;

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

        public async Task<bool> ReviewClaimAsync(
            int claimId,
            int userId,
            bool accept,
            string? comment
        )
        {
            var user = await _userService.GetUserAsync(userId);
            if (user == null)
                return false; // Invalid user
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            if (!roles.Contains("ProgramCoordinator") && !roles.Contains("AcademicManager"))
                return false; // User must be either a ProgramCoordinator or AcademicManager to review claims.

            var claim = await _context.ContractClaims.FindAsync(claimId);
            if (claim == null)
                return false; // Invalid claim

            // Prevent changing someone elses review
            if (
                (
                    roles.Contains("ProgramCoordinator")
                    && !roles.Contains("AcademicManager")
                    && claim.ProgramCoordinatorUserId != null
                    && claim.ProgramCoordinatorUserId != user.Id
                )
                || (
                    roles.Contains("AcademicManager")
                    && !roles.Contains("ProgramCoordinator")
                    && claim.AcademicManagerUserId != null
                    && claim.AcademicManagerUserId != user.Id
                )
                || (
                    roles.Contains("ProgramCoordinator")
                    && roles.Contains("AcademicManager")
                    && claim.ProgramCoordinatorUserId != null
                    && claim.ProgramCoordinatorUserId != user.Id
                    && claim.AcademicManagerUserId != null
                    && claim.AcademicManagerUserId != user.Id
                )
            )
                return false;

            if (
                roles.Contains("ProgramCoordinator")
                && ( // Either has not yet been reviewed, or reviewed by same user
                    claim.ProgramCoordinatorUserId == null || claim.ProgramCoordinatorUserId == user.Id
                )
            )
            { // User is ProgramCoordinator, so set ProgramCoordinator values
                claim.ProgramCoordinatorUserId = user.Id;
                claim.ProgramCoordinatorDecision = accept
                    ? ClaimDecision.VERIFIED
                    : ClaimDecision.REJECTED;
                claim.ProgramCoordinatorComment = comment;
            }
            if (
                roles.Contains("AcademicManager")
                && ( // Either has not yet been reviewed, or reviewed by same user
                    claim.AcademicManagerUserId == null || claim.AcademicManagerUserId == user.Id
                )
            )
            { // User is AcademicManager, so set AcademicManager values
                claim.AcademicManagerUserId = user.Id;
                claim.AcademicManagerDecision = accept
                    ? ClaimDecision.APPROVED
                    : ClaimDecision.REJECTED;
                claim.AcademicManagerComment = comment;
            }
            // Technically a user could hypothetically be both roles,
            // in which case both roles' values will be set if they haven't already been set or were set by same user.

            if (
                claim.ProgramCoordinatorDecision == ClaimDecision.PENDING
                || claim.AcademicManagerDecision == ClaimDecision.PENDING
            ) // If either decision is still pending, update status to PENDING_CONFIRM
                claim.ClaimStatus = ClaimStatus.PENDING_CONFIRM;
            else // Otherwise update status to whether both accepted or not
                claim.ClaimStatus =
                    claim.ProgramCoordinatorDecision == ClaimDecision.VERIFIED
                    && claim.AcademicManagerDecision == ClaimDecision.APPROVED
                        ? ClaimStatus.ACCEPTED
                        : ClaimStatus.REJECTED;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(
            int fileId
        ) => await _fileService.GetFileAsync(fileId);

        public async Task AddAutoReviewRuleAsync(AutoReviewRule rule)
        {
            _context.AutoReviewRules.Add(rule);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AutoReviewRule>> GetAutoReviewRulesForUserAsync(int userId) =>
            await _context.AutoReviewRules.Where(arr => arr.ReviewerId == userId).ToListAsync();

        public async Task<AutoReviewRule?> GetAutoReviewRule(int ruleId) =>
            await _context.AutoReviewRules.FindAsync(ruleId);

        public async Task UpdateAutoReviewRuleAsync(int ruleId, int userId, AutoReviewRule rule)
        {
            if (rule == null || rule.ReviewerId != userId)
                return;

            _context.Update(rule);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAutoReviewRuleAsync(int ruleId, int userId)
        {
            var rule = await _context.AutoReviewRules.FindAsync(ruleId);
            if (rule == null || rule.ReviewerId != userId)
                return;
            _context.AutoReviewRules.Remove(rule);
            await _context.SaveChangesAsync();
        }

        public async Task<(int pending, int reviewed)> AutoReviewPendingClaimsAsync(int userId)
        {
            var res = (pending: -1, reviewed: 0);
            var user = await _userService.GetUserAsync(userId);
            if (user == null)
                return res; // Invalid user

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            if (!roles.Contains("ProgramCoordinator") && !roles.Contains("AcademicManager"))
                return res; // User must be either a ProgramCoordinator or AcademicManager to review claims.

            // Set res.pending to -2 to differentiate between invalid auth and other early returns
            res.pending = -2;

            var rules = await GetAutoReviewRulesForUserAsync(userId);
            if (rules.Count == 0)
                return res; // No rules to follow
            // Sort rules in order of priority, so highest priority runs last and overwrites lower priority rules.
            rules = [.. rules.OrderBy(r => r.Priority)];

            var pendingClaims = (await GetClaimsAsync()).Where(c =>
                c.ClaimStatus <= ClaimStatus.PENDING_CONFIRM // Status is either PENDING_CONFIRM or less (PENDING)
                && ( // Claim has not yet been reviewed by a relevant role
                    (c.ProgramCoordinatorUserId == null && roles.Contains("ProgramCoordinator"))
                    || (c.AcademicManagerUserId == null && roles.Contains("AcademicManager"))
                )
            );
            res.pending = pendingClaims.Count();

            if (res.pending == 0)
                return res; // Nothing to review

            var reviewedIds = new List<int>();

            foreach (var rule in rules)
            {
                if (rule.AutoDecision == ClaimDecision.PENDING)
                    continue; // Skip any pending rules, review is either accept = true/false

                foreach (var claim in pendingClaims)
                {
                    var val = rule.ComparisonVar switch
                    { // Set relevant val
                        AutoReviewComparisonVar.HOURLY_RATE => claim.HourlyRate,
                        AutoReviewComparisonVar.HOURS_WORKED => claim.HoursWorked,
                        AutoReviewComparisonVar.PAYMENT_TOTAL => claim.HourlyRate
                            * claim.HoursWorked,
                        _ => decimal.MinValue,
                    };
                    if (val == decimal.MinValue)
                        continue;

                    var condition = rule.ComparisonOp switch
                    { // Evaluate relevant comparison
                        AutoReviewComparisonOp.EQUAL => val == rule.ComparisonValue,
                        AutoReviewComparisonOp.NOT_EQUAL => val != rule.ComparisonValue,
                        AutoReviewComparisonOp.LESS_THAN => val < rule.ComparisonValue,
                        AutoReviewComparisonOp.LESS_THAN_OR_EQUAL => val <= rule.ComparisonValue,
                        AutoReviewComparisonOp.GREATER_THAN => val > rule.ComparisonValue,
                        AutoReviewComparisonOp.GREATER_THAN_OR_EQUAL => val >= rule.ComparisonValue,
                        _ => false,
                    };
                    if (!condition)
                        continue; // Skip reviewing false evaluations

                    // Finally, if we made it this far, we review the claim.
                    // Note that claims may be reviewed more than once, which is intended behavior and
                    // allows the priority system to actually work and overwrite lower priority rules.
                    await ReviewClaimAsync(
                        claim.Id,
                        user.Id,
                        accept: rule.AutoDecision != ClaimDecision.REJECTED,
                        comment: rule.AutoComment
                            ?? $"Automatically {rule.AutoDecision} claim because {rule.ComparisonVar} = '{val}' is {rule.ComparisonOp} to '{rule.ComparisonValue}'"
                    );

                    if (!reviewedIds.Contains(claim.Id))
                        reviewedIds.Add(claim.Id);
                }
            }

            res.reviewed = reviewedIds.Count;
            return res;
        }
    }
}
