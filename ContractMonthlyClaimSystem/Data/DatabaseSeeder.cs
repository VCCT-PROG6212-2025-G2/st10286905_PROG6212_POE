// AI Disclosure: ChatGPT assisted creating this class. Link: https://chatgpt.com/share/68c04c01-77a4-800b-ac30-db12e569f8af
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.Auth;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Data
{
    public class DatabaseSeeder(IUserService userService, AppDbContext context)
    {
        private readonly IUserService _userService = userService;
        private readonly AppDbContext _context = context;

        public async Task SeedAsync()
        {
            await SeedUsersWithRoles();
            await SeedLecturerModulesAsync();
        }

        public async Task SeedUsersWithRoles()
        {
            // Create users with roles
            await _userService.RegisterAsync(
                username: "admin",
                password: "Admin!!1",
                email: "admin@cmcs.app",
                firstName: "Ad",
                lastName: "min",
                roleName: "Admin"
            );
            await _userService.RegisterAsync(
                username: "humanresources",
                password: "HumanResources!!1",
                email: "humanresources@cmcs.app",
                firstName: "Human",
                lastName: "Resources",
                roleName: "HR"
            );
            await _userService.RegisterAsync(
                username: "lecturer",
                password: "Lecturer!!1",
                email: "lecturer@cmcs.app",
                firstName: "Lecturer",
                lastName: "Example",
                roleName: "Lecturer"
            );
            await _userService.RegisterAsync(
                username: "programcoordinator",
                password: "ProgramCoordinator!!1",
                email: "programcoordinator@cmcs.app",
                firstName: "Program",
                lastName: "Coordinator",
                roleName: "ProgramCoordinator"
            );
            await _userService.RegisterAsync(
                username: "academicmanager",
                password: "AcademicManager!!1",
                email: "academicmanager@cmcs.app",
                firstName: "Academic",
                lastName: "Manager",
                roleName: "AcademicManager"
            );
        }

        public async Task SeedLecturerModulesAsync()
        {
            Module[] modules =
            [
                new Module { Name = "Programming 2B", Code = "PROG6212" },
                new Module { Name = "Cloud Development B", Code = "CLDV6212" },
                new Module { Name = "Information Systems 2C", Code = "INSY7213" },
                new Module { Name = "Cyber Security 1337E", Code = "CSEC1337" },
            ];

            var lecturerUser = await _userService.GetUserAsync("lecturer");

            int i = 0;
            foreach (var module in modules)
            {
                if (
                    !await _context
                        .Modules.Where(m => m.Name == module.Name && m.Code == module.Code)
                        .AnyAsync()
                )
                {
                    _context.Modules.Add(module);
                    await _context.SaveChangesAsync();
                }
                var foundModule = await _context
                    .Modules.Where(m => m.Name == module.Name && m.Code == module.Code)
                    .FirstAsync();

                var lecturerModule = new LecturerModule
                {
                    LecturerUserId = lecturerUser.Id,
                    ModuleId = foundModule.Id,
                    HourlyRate =
                        250m
                        + 50m * i++ * (foundModule.Code != "CSEC1337" ? 1 : 0)
                        + (foundModule.Code == "CSEC1337" ? 9001 : 0),
                };
                if (!await _context.LecturerModules.ContainsAsync(lecturerModule))
                    _context.LecturerModules.Add(lecturerModule);
            }

            await _context.SaveChangesAsync();
        }
    }
}
