using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<(AppUser User, IList<string> Roles)>> GetUsersWithRolesAsync();
        Task<List<IdentityRole>> GetRolesAsync();
        Task CreateRoleAsync(string roleName);
        Task DeleteRoleAsync(string roleName);
        Task UpdateUserRolesAsync(string userId, IEnumerable<string> selectedRoles);
    }
}
