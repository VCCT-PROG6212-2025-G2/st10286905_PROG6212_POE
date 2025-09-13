// AI Disclosure: ChatGPT assisted creating this class. Link: https://chatgpt.com/share/68c04c01-77a4-800b-ac30-db12e569f8af
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ContractMonthlyClaimSystem.Data
{
    public class DatabaseSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DatabaseSeeder(
            RoleManager<IdentityRole> roleManager,
            UserManager<AppUser> userManager,
            ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        public async Task Seed()
        {
            await SeedRolesAndUsersAsync();
            await SeedLecturerModulesAsync();
        }

        public async Task SeedRolesAndUsersAsync()
        {
            // Create roles if they don't exist
            string[] roles = { "Admin", "Lecturer", "ProgramCoordinator", "AcademicManager" };
            foreach (var role in roles)
            {
                var foundRole = await _roleManager.FindByNameAsync(role);
                if (foundRole == null)
                    await _roleManager.CreateAsync(new IdentityRole(role));
            }

            // Helper lambda to create a user and add them to a given role, with option to delete first if exists
            async Task CreateUserWithRole(string email, string password, string role, bool deleteIfExists = false)
            {
                var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
                IdentityResult res;
                var foundUser = await _userManager.FindByEmailAsync(email);
                if (foundUser != null && deleteIfExists)
                {
                    res = await _userManager.DeleteAsync(foundUser);
                    if (res == IdentityResult.Success)
                        Console.WriteLine($"Deleted user: {email}");
                }
                if (foundUser == null || deleteIfExists)
                {
                    res = await _userManager.CreateAsync(user, password);
                    if (res == IdentityResult.Success)
                        Console.WriteLine($"Created user: {email}");
                }
                user = await _userManager.FindByEmailAsync(email);
                if (!await _userManager.IsInRoleAsync(user, role))
                {
                    res = await _userManager.AddToRoleAsync(user, role);
                    if (res == IdentityResult.Success)
                        Console.WriteLine($"Added user: {email} to role: {role}");
                }
            }

            // Create users with roles
            await CreateUserWithRole("admin@cmcs.app", "Admin!!1", "Admin");
            await CreateUserWithRole("lecturer@cmcs.app", "Lecturer!!1", "Lecturer");
            await CreateUserWithRole("programcoordinator@cmcs.app", "ProgramCoordinator!!1", "ProgramCoordinator");
            await CreateUserWithRole("academicmanager@cmcs.app", "AcademicManager!!1", "AcademicManager");
        }

        public async Task SeedLecturerModulesAsync()
        {
            Module[] modules =
            [
                new Module { Name = "Programming 2B", Code = "PROG6212" },
                new Module { Name = "Cloud Development B", Code = "CLDV6212" },
                new Module { Name = "Information Systems 2C", Code = "INSY7213" }
            ];

            var lecturerUser = await _userManager.FindByNameAsync("lecturer@cmcs.app");

            foreach (var module in modules)
            {
                if (!await _context.Modules.Where(m => m.Name == module.Name && m.Code == module.Code).AnyAsync())
                {
                    _context.Modules.Add(module);
                    await _context.SaveChangesAsync();
                }
                var foundModule = await _context.Modules.Where(m => m.Name == module.Name && m.Code == module.Code).FirstAsync();

                var lecturerModule = new LecturerModule
                {
                    LecturerUserId = lecturerUser.Id,
                    ModuleId = foundModule.Id
                };
                if (!await _context.LecturerModules.ContainsAsync(lecturerModule))
                    _context.LecturerModules.Add(lecturerModule);
            }

            await _context.SaveChangesAsync();
        }
    }
}
