using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IReviewerClaimService
    {
        public Task<List<ContractClaim>> GetClaimsAsync();
        public Task<ContractClaim?> GetClaimAsync(int claimId);
        public Task<List<UploadedFile>?> GetClaimFilesAsync(ContractClaim claim);
        public Task<bool> ReviewClaim(int claimId, string userId, bool accept, string? comment);
        Task<(string FileName, MemoryStream FileStream, string ContentType)?> GetFileAsync(int fileId);
    }
}
