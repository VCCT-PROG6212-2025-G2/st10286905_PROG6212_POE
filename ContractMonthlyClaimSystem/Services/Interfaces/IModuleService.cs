using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IModuleService
    {
        Task<List<Module>> GetModulesAsync();
        Task<List<Module>> GetModulesForLecturerAsync(int lecturerId);
        Task<List<LecturerModule>> GetLecturerModulesAsync(int lecturerId);
        Task AddModuleAsync(Module module);
        Task AddModuleAsync(string name, string code);
        Task RemoveModuleAsync(int moduleId);
        Task AddLecturerModuleAsync(int lecturerId, int moduleId, decimal hourlyRate);
        Task RemoveLecturerModuleAsync(int lecturerId, int moduleId);
        Task UpdateLecturerModuleHourlyRate(int lecturerId, int moduleId, decimal hourlyRate);
    }
}
