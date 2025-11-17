using System.Security.Claims;
using ContractMonthlyClaimSystem.Models.Auth;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IUserService
    {
        Task<AppUser?> GetUserAsync(int id);
        Task<AppUser?> GetUserAsync(string? username);
        Task<List<AppUser>> GetAllUsersAsync();
        Task<List<AppUser>> GetAllUsersInRoleAsync(string roleName);
        Task<AppUser?> AuthenticateAsync(string username, string password);
        Task<AppUser?> RegisterAsync(string username, string password);
        Task<AppUser?> RegisterAsync(
            string username,
            string password,
            string? email = null,
            string? firstName = null,
            string? lastName = null,
            string? roleName = null
        );
        Task<AppUser?> RegisterAsync(
            string username,
            string password,
            string? email = null,
            string? firstName = null,
            string? lastName = null,
            List<string?>? roleNames = null
        );
        ClaimsPrincipal BuildClaimsPrincipal(AppUser user);
        Task<AppUser?> ChangePasswordAsync(string username, string oldPassword, string newPassword);
        Task<AppUser?> UpdateDetailsAsync(
            string username,
            string password,
            string? email = null,
            string? firstName = null,
            string? lastName = null
        );
        Task<bool> IsUserInRoleAsync(int userId, string roleName);
        Task<List<AppUser>> GetUsersInRoleAsync(string roleName);
        Task AddUserToRoleAsync(int userId, string roleName);
        Task AddUserToRolesAsync(int userId, IEnumerable<string> roleNames);
        Task RemoveUserFromRoleAsync(int userId, string roleName);
        Task RemoveUserFromRolesAsync(int userId, IEnumerable<string> roleNames);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> DeleteUserAsync(string username);
    }
}
