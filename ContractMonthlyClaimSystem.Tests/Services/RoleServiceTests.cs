// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68f5452c-2788-800b-bbbc-175029690cfd

using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models.Auth;
using ContractMonthlyClaimSystem.Services;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContractMonthlyClaimSystem.Tests.Services
{
    public class RoleServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private RoleService GetService(AppDbContext context, Mock<IUserService>? mockUserService = null)
        {
            return new RoleService(context, mockUserService?.Object ?? Mock.Of<IUserService>());
        }

        // -----------------------------------------------------------
        // GetUsersWithRolesAsync
        // -----------------------------------------------------------
        [Fact]
        public async Task GetUsersWithRolesAsync_ReturnsUsersWithTheirRoles()
        {
            var context = GetDbContext();

            var role1 = new AppRole { Id = 1, Name = "Admin" };
            var role2 = new AppRole { Id = 2, Name = "Lecturer" };

            var user = new AppUser { Id = 1, UserName = "john" };
            user.UserRoles.Add(new AppUserRole { UserId = 1, RoleId = 1, Role = role1 });
            user.UserRoles.Add(new AppUserRole { UserId = 1, RoleId = 2, Role = role2 });

            context.Roles.AddRange(role1, role2);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = GetService(context);

            var result = await service.GetUsersWithRolesAsync();

            Assert.Single(result);
            Assert.Equal(2, result[0].Roles.Count);
            Assert.Contains("Admin", result[0].Roles);
            Assert.Contains("Lecturer", result[0].Roles);
        }

        // -----------------------------------------------------------
        // GetRolesAsync
        // -----------------------------------------------------------
        [Fact]
        public async Task GetRolesAsync_ReturnsAllRoles()
        {
            var context = GetDbContext();

            context.Roles.Add(new AppRole { Name = "Admin" });
            context.Roles.Add(new AppRole { Name = "Manager" });
            await context.SaveChangesAsync();

            var service = GetService(context);

            var roles = await service.GetRolesAsync();

            Assert.Equal(2, roles.Count);
        }

        // -----------------------------------------------------------
        // CreateRoleAsync
        // -----------------------------------------------------------
        [Fact]
        public async Task CreateRoleAsync_AddsNewRole_WhenNotExists()
        {
            var context = GetDbContext();
            var service = GetService(context);

            await service.CreateRoleAsync("Admin");

            Assert.Single(context.Roles);
            Assert.Equal("Admin", context.Roles.First().Name);
        }

        [Fact]
        public async Task CreateRoleAsync_DoesNothing_WhenRoleAlreadyExists()
        {
            var context = GetDbContext();

            context.Roles.Add(new AppRole { Name = "Admin" });
            await context.SaveChangesAsync();

            var service = GetService(context);

            await service.CreateRoleAsync("Admin");

            Assert.Single(context.Roles);
        }

        [Fact]
        public async Task CreateRoleAsync_Throws_WhenNameIsEmpty()
        {
            var context = GetDbContext();
            var service = GetService(context);

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateRoleAsync(" "));
        }

        // -----------------------------------------------------------
        // DeleteRoleAsync
        // -----------------------------------------------------------
        [Fact]
        public async Task DeleteRoleAsync_RemovesRole_WhenExists()
        {
            var context = GetDbContext();
            context.Roles.Add(new AppRole { Name = "Admin" });
            await context.SaveChangesAsync();

            var service = GetService(context);

            await service.DeleteRoleAsync("Admin");

            Assert.Empty(context.Roles);
        }

        [Fact]
        public async Task DeleteRoleAsync_DoesNothing_WhenRoleDoesNotExist()
        {
            var context = GetDbContext();
            var service = GetService(context);

            await service.DeleteRoleAsync("Unknown");

            Assert.Empty(context.Roles);
        }

        // -----------------------------------------------------------
        // UpdateUserRolesAsync
        // -----------------------------------------------------------
        [Fact]
        public async Task UpdateUserRolesAsync_CallsAddUserToRoles_WhenRolesToAddExist()
        {
            var context = GetDbContext();

            var user = new AppUser { Id = 1, UserName = "john" };
            user.UserRoles.Add(new AppUserRole
            {
                Role = new AppRole { Name = "Lecturer" }
            });

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var userServiceMock = new Mock<IUserService>();
            var service = GetService(context, userServiceMock);

            var newRoles = new[] { "Lecturer", "Admin" };

            await service.UpdateUserRolesAsync(1, newRoles);

            userServiceMock.Verify(
                x => x.AddUserToRolesAsync(1, It.Is<IEnumerable<string>>(r => r.Contains("Admin"))),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateUserRolesAsync_CallsRemoveUserFromRoles_WhenRolesToRemoveExist()
        {
            var context = GetDbContext();

            var roleLecturer = new AppRole { Name = "Lecturer" };
            var roleAdmin = new AppRole { Name = "Admin" };

            var user = new AppUser { Id = 1, UserName = "john" };
            user.UserRoles.Add(new AppUserRole { Role = roleLecturer });
            user.UserRoles.Add(new AppUserRole { Role = roleAdmin });

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var userServiceMock = new Mock<IUserService>();
            var service = GetService(context, userServiceMock);

            var newRoles = new[] { "Lecturer" }; // Admin should be removed

            await service.UpdateUserRolesAsync(1, newRoles);

            userServiceMock.Verify(
                x => x.RemoveUserFromRolesAsync(1, It.Is<IEnumerable<string>>(r => r.Contains("Admin"))),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateUserRolesAsync_NoOps_WhenRolesSame()
        {
            var context = GetDbContext();

            var user = new AppUser { Id = 1, UserName = "john" };
            user.UserRoles.Add(new AppUserRole { Role = new AppRole { Name = "Admin" } });

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var userServiceMock = new Mock<IUserService>();
            var service = GetService(context, userServiceMock);

            await service.UpdateUserRolesAsync(1, new[] { "Admin" });

            userServiceMock.Verify(x => x.AddUserToRolesAsync(It.IsAny<int>(), It.IsAny<IEnumerable<string>>()), Times.Never);
            userServiceMock.Verify(x => x.RemoveUserFromRolesAsync(It.IsAny<int>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUserRolesAsync_Throws_WhenUserNotFound()
        {
            var context = GetDbContext();
            var service = GetService(context, new Mock<IUserService>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateUserRolesAsync(999, new[] { "Admin" })
            );
        }
    }
}
