using ContractMonthlyClaimSystem.Models.Auth;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<(AppUser User, IList<string> Roles)>> GetUsersWithRolesAsync();
        Task<List<AppRole>> GetRolesAsync();
        Task CreateRoleAsync(string roleName);
        Task DeleteRoleAsync(string roleName);
        Task UpdateUserRolesAsync(int userId, IEnumerable<string> selectedRoles);
    }
}
