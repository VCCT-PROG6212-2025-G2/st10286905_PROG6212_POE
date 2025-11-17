using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models.Auth;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Services
{
    public class RoleService(AppDbContext context, IUserService userService) : IRoleService
    {
        private readonly AppDbContext _context = context;
        private readonly IUserService _userService = userService;

        public async Task<List<(AppUser User, IList<string> Roles)>> GetUsersWithRolesAsync()
        {
            var results = new List<(AppUser User, IList<string> Roles)>();

            var users = await _context
                .Users.Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
            foreach (var user in users)
                results.Add((user, user.UserRoles.Select(ur => ur.Role.Name).ToList()));

            return results;
        }

        public async Task<List<AppRole>> GetRolesAsync() => await _context.Roles.ToListAsync();

        public async Task CreateRoleAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentNullException($"{nameof(roleName)} cannot be empty");

            if (!await _context.Roles.AnyAsync(r => r.Name == roleName))
            {
                _context.Roles.Add(new AppRole { Name = roleName });
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteRoleAsync(string roleName)
        {
            var role = await _context.Roles.Where(r => r.Name == roleName).FirstOrDefaultAsync();
            if (role != null)
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateUserRolesAsync(int userId, IEnumerable<string> selectedRoles)
        {
            var user =
                await _context
                .Users.Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .SingleOrDefaultAsync(u => u.Id == userId)
                ?? throw new InvalidOperationException($"No user found with ID: {userId}");

            var currentRoles = user.UserRoles.Select(ur=>ur.Role.Name).ToList();
            var toAdd = selectedRoles.Except(currentRoles);
            var toRemove = currentRoles.Except(selectedRoles);

            if (toAdd.Any())
                await _userService.AddUserToRolesAsync(userId, toAdd);

            if (toRemove.Any())
                await _userService.RemoveUserFromRolesAsync(userId, toRemove);
        }
    }
}
