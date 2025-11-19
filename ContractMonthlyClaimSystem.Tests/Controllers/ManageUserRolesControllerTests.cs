// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68f5452c-2788-800b-bbbc-175029690cfd

using ContractMonthlyClaimSystem.Controllers;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.Auth;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ContractMonthlyClaimSystem.Tests.Controllers
{
    public class ManageUserRolesControllerTests
    {
        private readonly Mock<IRoleService> _roleServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly ManageUserRolesController _controller;

        public ManageUserRolesControllerTests()
        {
            _roleServiceMock = new Mock<IRoleService>();
            _userServiceMock = new Mock<IUserService>();

            _controller = new ManageUserRolesController(
                _roleServiceMock.Object,
                _userServiceMock.Object
            );
        }

        // ---------------------------------------------------------
        // INDEX
        // ---------------------------------------------------------
        [Fact]
        public async Task Index_ReturnsViewWithUserRoles()
        {
            var usersWithRoles = new List<(AppUser User, IList<string> Roles)>
            {
                (new AppUser { Id = 1, UserName = "user1@cmcs.app" }, new List<string> { "Lecturer" }),
                (new AppUser { Id = 2, UserName = "user2@cmcs.app" }, new List<string> { "Admin" })
            };

            var allRoles = new List<AppRole>
            {
                new() { Id = 1, Name = "Admin" },
                new() { Id = 2, Name = "Lecturer" }
            };

            _roleServiceMock.Setup(s => s.GetUsersWithRolesAsync()).ReturnsAsync(usersWithRoles);
            _roleServiceMock.Setup(s => s.GetRolesAsync()).ReturnsAsync(allRoles);

            var result = await _controller.Index() as ViewResult;

            Assert.NotNull(result);

            var model = Assert.IsType<List<UserRolesViewModel>>(result.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal(allRoles, result.ViewData["AllRoles"]);
        }

        // ---------------------------------------------------------
        // MANAGE (GET)
        // ---------------------------------------------------------
        [Fact]
        public async Task Manage_Get_ReturnsView_WhenUserExists()
        {
            var user = new AppUser { Id = 10, UserName = "testuser" };

            var allRoles = new List<AppRole>
            {
                new() { Id = 1, Name = "Admin" },
                new() { Id = 2, Name = "Lecturer" }
            };

            _userServiceMock.Setup(s => s.GetUserAsync(10)).ReturnsAsync(user);
            _roleServiceMock.Setup(s => s.GetRolesAsync()).ReturnsAsync(allRoles);

            _userServiceMock.Setup(s => s.IsUserInRoleAsync(10, "Admin")).ReturnsAsync(true);
            _userServiceMock.Setup(s => s.IsUserInRoleAsync(10, "Lecturer")).ReturnsAsync(false);

            var result = await _controller.Manage(10) as ViewResult;

            Assert.NotNull(result);

            var model = Assert.IsType<ManageUserRolesViewModel>(result.Model);
            Assert.Equal(10, model.UserId);
            Assert.Equal("testuser", model.UserName);
            Assert.Equal(2, model.Roles.Count);

            Assert.True(model.Roles.First(r => r.RoleName == "Admin").Selected);
            Assert.False(model.Roles.First(r => r.RoleName == "Lecturer").Selected);
        }

        [Fact]
        public async Task Manage_Get_ReturnsNotFound_WhenUserMissing()
        {
            _userServiceMock.Setup(s => s.GetUserAsync(999)).ReturnsAsync((AppUser?)null);

            var result = await _controller.Manage(999);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // MANAGE (POST)
        // ---------------------------------------------------------
        [Fact]
        public async Task Manage_Post_UpdatesRoles_AndRedirects()
        {
            var model = new ManageUserRolesViewModel
            {
                UserId = 10,
                Roles =
                [
                    new RoleSelectionViewModel { RoleName = "Admin", Selected = false },
                    new RoleSelectionViewModel { RoleName = "Lecturer", Selected = true }
                ]
            };

            var user = new AppUser { Id = 10, UserName = "user" };

            _userServiceMock.Setup(s => s.GetUserAsync(10)).ReturnsAsync(user);

            _roleServiceMock
                .Setup(s => s.UpdateUserRolesAsync(10, It.IsAny<IEnumerable<string>>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Manage(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageUserRolesController.Index), result.ActionName);

            _roleServiceMock.Verify(
                s => s.UpdateUserRolesAsync(
                    10,
                    It.Is<IEnumerable<string>>(roles => roles.Contains("Lecturer") && !roles.Contains("Admin"))
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task Manage_Post_ReturnsNotFound_WhenUserMissing()
        {
            var model = new ManageUserRolesViewModel { UserId = 404 };

            _userServiceMock.Setup(s => s.GetUserAsync(404)).ReturnsAsync((AppUser?)null);

            var result = await _controller.Manage(model);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // ADD ROLE
        // ---------------------------------------------------------
        [Fact]
        public async Task AddRole_CreatesRole_AndRedirects()
        {
            _roleServiceMock.Setup(s => s.CreateRoleAsync("Lecturer")).Returns(Task.CompletedTask);

            var result = await _controller.AddRole("Lecturer") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageUserRolesController.Index), result.ActionName);

            _roleServiceMock.Verify(s => s.CreateRoleAsync("Lecturer"), Times.Once);
        }

        [Fact]
        public async Task AddRole_DoesNothing_WhenNameIsEmpty()
        {
            var result = await _controller.AddRole("") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageUserRolesController.Index), result.ActionName);

            _roleServiceMock.Verify(s => s.CreateRoleAsync(It.IsAny<string>()), Times.Never);
        }

        // ---------------------------------------------------------
        // DELETE ROLE
        // ---------------------------------------------------------
        [Fact]
        public async Task DeleteRole_DeletesRole_AndRedirects()
        {
            _roleServiceMock.Setup(s => s.DeleteRoleAsync("Lecturer")).Returns(Task.CompletedTask);

            var result = await _controller.DeleteRole("Lecturer") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageUserRolesController.Index), result.ActionName);

            _roleServiceMock.Verify(s => s.DeleteRoleAsync("Lecturer"), Times.Once);
        }
    }
}
