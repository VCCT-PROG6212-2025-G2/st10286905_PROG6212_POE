using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Services
{
    public class RoleService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager
    ) : IRoleService
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;

        public async Task<List<(AppUser User, IList<string> Roles)>> GetUsersWithRolesAsync()
        {
            var results = new List<(AppUser User, IList<string> Roles)>();
            
            var users = await _userManager.Users.ToListAsync();
            foreach (var user in users)
                results.Add((user, await _userManager.GetRolesAsync(user)));

            return results;
        }

        public async Task<List<IdentityRole>> GetRolesAsync() =>
            await _roleManager.Roles.ToListAsync();

        public async Task CreateRoleAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentNullException($"{nameof(roleName)} cannot be empty");

            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        public async Task DeleteRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
                await _roleManager.DeleteAsync(role);
        }

        public async Task UpdateUserRolesAsync(string userId, IEnumerable<string> selectedRoles)
        {
            var user =
                await _userManager.FindByIdAsync(userId)
                ?? throw new InvalidOperationException($"No user found with ID: {userId}");

            var currentRoles = await _userManager.GetRolesAsync(user);
            var toAdd = selectedRoles.Except(currentRoles);
            var toRemove = currentRoles.Except(selectedRoles);

            if(toAdd.Any())
                await _userManager.AddToRolesAsync(user, toAdd);

            if(toRemove.Any())
                await _userManager.RemoveFromRolesAsync(user, toRemove);
        }
    }
}
