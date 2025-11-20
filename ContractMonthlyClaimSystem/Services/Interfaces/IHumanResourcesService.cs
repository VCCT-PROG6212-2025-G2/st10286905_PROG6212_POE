using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IHumanResourcesService
    {
        public Task<LecturerDetails?> GetLecturerDetailsAsync(int id);
        public Task SetLecturerDetailsAsync(LecturerDetails details);
        public Task<int> AutoReviewClaimsForReviewersAsync();
        public Task<List<ContractClaim>> GetApprovedClaimsAsync();
        public Task<int> ProcessApprovedClaimInvoicesAsync();
        public Task<List<ClaimInvoiceDocument>> GetProcessedClaimInvoicesAsync();
        public Task<(Stream FileStream, string ContentType, string FileName)?> GetClaimInvoicePdfAsync(int claimId);
        public Task<(Stream FileStream, string ContentType, string FileName)?> GenerateClaimInvoicePdfAsync(int claimId);
    }
}
