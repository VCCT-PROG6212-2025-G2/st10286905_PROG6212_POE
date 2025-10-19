using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IModuleService
    {
        Task<List<Module>> GetModulesAsync();
        Task<List<Module>> GetModulesForLecturerAsync(string lecturerId);
        Task AddModuleAsync(Module module);
        Task AddModuleAsync(string name, string code);
        Task RemoveModuleAsync(int moduleId);
        Task AddLecturerModuleAsync(string lecturerId, int moduleId);
        Task RemoveLecturerModuleAsync(string lecturerId, int moduleId);
    }
}
