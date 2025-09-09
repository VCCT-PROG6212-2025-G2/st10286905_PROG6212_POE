using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;

namespace ContractMonthlyClaimSystem.Areas.Identity.Data
{
    public class DatabaseSeeder
    {
        public static async Task SeedRolesAndUsers(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            // Create roles if they don't exist
            string[] roles = { "Admin", "Lecturer", "ProgramCoordinator", "AcademicManager" };
            foreach (var role in roles)
            {
                var foundRole = await roleManager.FindByNameAsync(role);
                if (foundRole == null)
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            async Task CreateUserWithRole(string email, string password, string role, bool deleteIfExists = false)
            {
                var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
                IdentityResult res;
                var foundUser = await userManager.FindByEmailAsync(email);
                if (foundUser != null && deleteIfExists)
                {
                    res = await userManager.DeleteAsync(foundUser);
                    if (res == IdentityResult.Success)
                        Console.WriteLine($"Deleted user: {email}");
                }
                if (foundUser == null || deleteIfExists)
                {
                    res = await userManager.CreateAsync(user, password);
                    if (res == IdentityResult.Success)
                        Console.WriteLine($"Created user: {email}");
                }
                res = await userManager.AddToRoleAsync(user, role);
                if (res == IdentityResult.Success)
                    Console.WriteLine($"Added user: {email} to role: {role}");
            }

            await CreateUserWithRole("admin@cmcs.app", "Admin!!1", "Admin", deleteIfExists: true);
            await CreateUserWithRole("lecturer@cmcs.app", "Lecturer!!1", "Lecturer");
            await CreateUserWithRole("programcoordinator@cmcs.app", "ProgramCoordinator!!1", "ProgramCoordinator");
            await CreateUserWithRole("academicmanager@cmcs.app", "AcademicManager!!1", "AcademicManager");
        }
    }
}
