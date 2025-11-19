using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IReviewerClaimService
    {
        public Task<List<ContractClaim>> GetClaimsAsync();
        public Task<ContractClaim?> GetClaimAsync(int claimId);
        public Task<List<UploadedFile>?> GetClaimFilesAsync(ContractClaim claim);
        public Task<bool> ReviewClaimAsync(int claimId, int userId, bool accept, string? comment);
        Task<(string FileName, MemoryStream FileStream, string ContentType)?> GetFileAsync(
            int fileId
        );
        public Task AddAutoReviewRuleAsync(AutoReviewRule rule);
        public Task<List<AutoReviewRule>> GetAutoReviewRulesForUserAsync(int userId);
        public Task<AutoReviewRule?> GetAutoReviewRule(int ruleId);
        public Task UpdateAutoReviewRuleAsync(int ruleId, int userId, AutoReviewRule rule);
        public Task RemoveAutoReviewRuleAsync(int ruleId, int userId);
        public Task<(int pending, int reviewed)> AutoReviewPendingClaimsAsync(int userId);
    }
}
