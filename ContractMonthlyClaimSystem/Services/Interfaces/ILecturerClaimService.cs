using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface ILecturerClaimService
    {
        Task<List<ContractClaim>> GetClaimsForLecturerAsync(string lecturerId);
        Task<ContractClaim?> GetClaimAsync(int claimId, string lecturerId);
        Task<List<UploadedFile>?> GetClaimFilesAsync(ContractClaim claim);
        Task<List<Module>> GetModulesForLecturerAsync(string lecturerId);
        Task<ContractClaim> CreateClaimAsync(string lecturerId, CreateClaimViewModel model);
        Task AddFilesToClaimAsync(ContractClaim claim, List<IFormFile> files);
        Task<(string FileName, MemoryStream FileStream, string ContentType)?> GetFileAsync(int fileId, string lecturerId);
    }
}
