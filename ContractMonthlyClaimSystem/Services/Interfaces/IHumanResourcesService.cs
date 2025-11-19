using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IHumanResourcesService
    {
        public Task<LecturerDetails?> GetLecturerDetailsAsync(int id);
        public Task SetLecturerDetailsAsync(LecturerDetails details);
        public Task AutoReviewClaimsForReviewersAsync();
        public Task<List<ContractClaim>> GetApprovedClaimsAsync();
        public Task<(string FileName, MemoryStream FileStream, string ContentType)?> GenerateClaimInvoicePdfAsync(int claimId);
    }
}
