using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface ILecturerClaimService
    {
        Task<List<ContractClaim>> GetClaimsForLecturerAsync(int lecturerId);
        Task<ContractClaim?> GetClaimAsync(int claimId, int lecturerId);
        Task<List<UploadedFile>?> GetClaimFilesAsync(ContractClaim claim);
        Task<List<Module>> GetModulesForLecturerAsync(int lecturerId);
        Task<decimal?> GetLecturerHourlyRateAsync(int lecturerId, int moduleId);
        Task<ContractClaim> CreateClaimAsync(int lecturerId, CreateClaimViewModel model);
        Task AddFilesToClaimAsync(ContractClaim claim, List<IFormFile> files);
        Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(int fileId, int lecturerId);
    }
}
