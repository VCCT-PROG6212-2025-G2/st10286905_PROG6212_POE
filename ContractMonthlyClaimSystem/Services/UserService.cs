// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/690a140c-df58-800b-8dda-d684e3acea06

using System.Security.Claims;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models.Auth;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Services
{
    public class UserService(AppDbContext context, IPasswordHasher passwordHasher) : IUserService
    {
        private readonly AppDbContext _context = context;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;

        public async Task<AppUser?> GetUserAsync(int id) =>
            await _context
                .Users.Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .SingleOrDefaultAsync(u => u.Id == id);

        public async Task<AppUser?> GetUserAsync(string? username) =>
            username == null
                ? null
                : await _context
                    .Users.Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .SingleOrDefaultAsync(u => u.UserName == username);

        public async Task<List<AppUser>> GetAllUsersAsync() => await _context.Users.ToListAsync();

        public async Task<List<AppUser>> GetAllUsersInRoleAsync(string roleName) =>
            await _context
                .Users.Where(u => u.UserRoles.Select(ur => ur.Role.Name).Contains(roleName))
                .ToListAsync();

        public async Task<AppUser?> AuthenticateAsync(string username, string password)
        {
            var user = await GetUserAsync(username);

            if (
                user == null
                || !_passwordHasher.Verify(password, user.PasswordHash, user.PasswordSalt)
            )
                return null;

            return user;
        }

        public async Task<AppUser?> RegisterAsync(string username, string password)
        {
            if (await GetUserAsync(username) != null)
                return null; // User exists, so return null

            var (hash, salt) = _passwordHasher.HashPassword(password);

            var user = new AppUser
            {
                UserName = username,
                PasswordHash = hash,
                PasswordSalt = salt,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<AppUser?> RegisterAsync(
            string username,
            string password,
            string? email = null,
            string? firstName = null,
            string? lastName = null,
            string? roleName = null
        )
        {
            if (await GetUserAsync(username) != null)
                return null; // User exists, so return null

            var (hash, salt) = _passwordHasher.HashPassword(password);

            var user = new AppUser
            {
                UserName = username,
                PasswordHash = hash,
                PasswordSalt = salt,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
            };

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                var role =
                    await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
                    ?? new AppRole { Name = roleName };
                user.UserRoles.Add(new AppUserRole { User = user, Role = role });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<AppUser?> RegisterAsync(
            string username,
            string password,
            string? email = null,
            string? firstName = null,
            string? lastName = null,
            List<string?>? roleNames = null
        )
        {
            if (await GetUserAsync(username) != null)
                return null; // User exists, so return null

            var (hash, salt) = _passwordHasher.HashPassword(password);

            var user = new AppUser
            {
                UserName = username,
                PasswordHash = hash,
                PasswordSalt = salt,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
            };

            if (roleNames != null && roleNames.Count != 0)
            {
                foreach (var roleName in roleNames)
                {
                    if (!string.IsNullOrWhiteSpace(roleName))
                    {
                        var role =
                            await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
                            ?? new AppRole { Name = roleName };
                        user.UserRoles.Add(new AppUserRole { User = user, Role = role });
                    }
                }
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public ClaimsPrincipal BuildClaimsPrincipal(AppUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            return new ClaimsPrincipal(identity);
        }

        public async Task<AppUser?> ChangePasswordAsync(
            string username,
            string oldPassword,
            string newPassword
        )
        {
            var user = await AuthenticateAsync(username, oldPassword);
            if (user == null)
                return null; // Invalid user/password

            var (hash, salt) = _passwordHasher.HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<AppUser?> UpdateDetailsAsync(
            string username,
            string password,
            string? email = null,
            string? firstName = null,
            string? lastName = null
        )
        {
            var user = await AuthenticateAsync(username, password);
            if (user == null)
                return null; // Invalid user/password

            // Update given user information
            if (!string.IsNullOrWhiteSpace(email))
                user.Email = email;
            if (!string.IsNullOrWhiteSpace(firstName))
                user.FirstName = firstName;
            if (!string.IsNullOrWhiteSpace(lastName))
                user.LastName = lastName;

            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        { // AI Disclosure: ChatGPT assisted here. Link: https://chatgpt.com/s/t_690b508eb6548191b108b15228f4dec1
            var user = await GetUserAsync(id);
            if (user == null)
                return false;

            if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                _context.UserRoles.RemoveRange(user.UserRoles);
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return true;
            }

            // Immediately and efficiently delete user's UserRoles from db
            await _context.UserRoles.Where(ur => ur.UserId == user.Id).ExecuteDeleteAsync();
            // Update context state to detached for deleted UserRoles
            foreach (var ur in user.UserRoles)
                _context.Entry(ur).State = EntityState.Detached;

            // Immediately and efficiently delete user from db
            await _context.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
            // Update context state to detached for deleted user
            _context.Entry(user).State = EntityState.Detached;

            return true;
        }

        public async Task<bool> DeleteUserAsync(string username)
        { // AI Disclosure: ChatGPT assisted here. Link: https://chatgpt.com/s/t_690b508eb6548191b108b15228f4dec1
            var user = await GetUserAsync(username);
            if (user == null)
                return false;

            if(_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                _context.UserRoles.RemoveRange(user.UserRoles);
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return true;
            }

            // Immediately and efficiently delete user's UserRoles from db
            await _context.UserRoles.Where(ur => ur.UserId == user.Id).ExecuteDeleteAsync();
            // Update context state to detached for deleted UserRoles
            foreach (var ur in user.UserRoles)
                _context.Entry(ur).State = EntityState.Detached;

            // Immediately and efficiently delete user from db
            await _context.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
            // Update context state to detached for deleted user
            _context.Entry(user).State = EntityState.Detached;

            return true;
        }

        public async Task<bool> IsUserInRoleAsync(int userId, string roleName) =>
            await _context
                .Users.Where(u =>
                    u.Id == userId && u.UserRoles.Select(ur => ur.Role.Name).Contains(roleName)
                )
                .AnyAsync();

        public async Task<List<AppUser>> GetUsersInRoleAsync(string roleName) =>
            await _context
                .Users.Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Select(ur => ur.Role.Name).Contains(roleName))
                .ToListAsync();

        public async Task AddUserToRoleAsync(int userId, string roleName)
        {
            var user = await GetUserAsync(userId);
            if (user == null || user.UserRoles.Any(ur => ur.Role.Name == roleName))
                return; // User does not exist or is already in this role.

            var role =
                await _context.Roles.Where(r => r.Name == roleName).FirstOrDefaultAsync()
                ?? new AppRole { Name = roleName };

            user.UserRoles.Add(new AppUserRole { User = user, Role = role });
            await _context.SaveChangesAsync();
        }

        public async Task AddUserToRolesAsync(int userId, IEnumerable<string> roleNames)
        {
            foreach (var role in roleNames)
                await AddUserToRoleAsync(userId, role);
        }

        public async Task RemoveUserFromRoleAsync(int userId, string roleName)
        {
            var user = await GetUserAsync(userId);
            if (user == null)
                return; // User does not exist

            var userRole = user.UserRoles.Where(ur => ur.Role.Name == roleName).FirstOrDefault();
            if (userRole == null)
                return; // User is not in this role

            user.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveUserFromRolesAsync(int userId, IEnumerable<string> roleNames)
        {
            foreach (var role in roleNames)
                await RemoveUserFromRoleAsync(userId, role);
        }
    }
}
