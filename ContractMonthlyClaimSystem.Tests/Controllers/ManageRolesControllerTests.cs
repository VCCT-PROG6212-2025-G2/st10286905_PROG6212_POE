// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68f5452c-2788-800b-bbbc-175029690cfd

using ContractMonthlyClaimSystem.Controllers;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContractMonthlyClaimSystem.Tests.Controllers
{
    /// <summary>
    /// Unit tests for the ManageRolesController class.
    /// Each test validates correct controller logic, service interaction,
    /// and expected MVC outcomes (views, redirects, or not-found results).
    /// </summary>
    public class ManageRolesControllerTests
    {
        private readonly Mock<IRoleService> _roleServiceMock;
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly ManageRolesController _controller;

        public ManageRolesControllerTests()
        {
            // Mock role and user services for isolation.
            _roleServiceMock = new Mock<IRoleService>();

            var store = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );

            // Instantiate controller with mocks.
            _controller = new ManageRolesController(
                _roleServiceMock.Object,
                _userManagerMock.Object
            );
        }

        [Fact]
        public async Task Index_ReturnsViewWithUserRoles()
        {
            // Arrange: prepare users and roles.
            var user1 = new AppUser { Id = "U1", UserName = "user1@cmcs.app" };
            var user2 = new AppUser { Id = "U2", UserName = "user2@cmcs.app" };

            var usersWithRoles = new List<(AppUser User, IList<string> Roles)>
            {
                (user1, new List<string> { "Lecturer" }),
                (user2, new List<string> { "Admin" })
            };
            var allRoles = new List<IdentityRole>
            {
                new() { Name = "Admin" },
                new() { Name = "Lecturer" }
            };

            _roleServiceMock.Setup(s => s.GetUsersWithRolesAsync()).ReturnsAsync(usersWithRoles);
            _roleServiceMock.Setup(s => s.GetRolesAsync()).ReturnsAsync(allRoles);

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert: verify returned model and viewbag content.
            Assert.NotNull(result);
            var model = Assert.IsType<List<UserRolesViewModel>>(result.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal(allRoles, result.ViewData["AllRoles"]);
        }

        [Fact]
        public async Task Manage_Get_ReturnsViewWithUserRoles_WhenUserExists()
        {
            // Arrange
            var user = new AppUser { Id = "U1", UserName = "testuser" };
            var roles = new List<IdentityRole>
            {
                new() { Name = "Admin" },
                new() { Name = "Lecturer" }
            };

            _userManagerMock.Setup(u => u.FindByIdAsync("U1")).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(true);
            _userManagerMock.Setup(u => u.IsInRoleAsync(user, "Lecturer")).ReturnsAsync(false);
            _roleServiceMock.Setup(r => r.GetRolesAsync()).ReturnsAsync(roles);

            // Act
            var result = await _controller.Manage("U1") as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<ManageUserRolesViewModel>(result.Model);
            Assert.Equal("testuser", model.UserName);
            Assert.Equal(2, model.Roles.Count);
            Assert.True(model.Roles.First(r => r.RoleName == "Admin").Selected);
        }

        [Fact]
        public async Task Manage_Get_ReturnsNotFound_WhenUserMissing()
        {
            // Arrange: mock null result.
            _userManagerMock.Setup(u => u.FindByIdAsync("404")).ReturnsAsync((AppUser?)null);

            // Act
            var result = await _controller.Manage("404");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Manage_Post_UpdatesUserRolesAndRedirects()
        {
            // Arrange
            var model = new ManageUserRolesViewModel
            {
                UserId = "U1",
                Roles = new List<RoleSelectionViewModel>
                {
                    new() { RoleName = "Lecturer", Selected = true },
                    new() { RoleName = "Admin", Selected = false }
                }
            };
            var user = new AppUser { Id = "U1", UserName = "user" };

            _userManagerMock.Setup(u => u.FindByIdAsync("U1")).ReturnsAsync(user);
            _roleServiceMock
                .Setup(s => s.UpdateUserRolesAsync("U1", It.IsAny<IEnumerable<string>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Manage(model) as RedirectToActionResult;

            // Assert: redirected back to Index and roles updated.
            Assert.NotNull(result);
            Assert.Equal(nameof(ManageRolesController.Index), result.ActionName);
            _roleServiceMock.Verify(
                s => s.UpdateUserRolesAsync("U1", It.Is<IEnumerable<string>>(r => r.Contains("Lecturer"))),
                Times.Once
            );
        }

        [Fact]
        public async Task Manage_Post_ReturnsNotFound_WhenUserMissing()
        {
            // Arrange
            var model = new ManageUserRolesViewModel { UserId = "404" };
            _userManagerMock.Setup(u => u.FindByIdAsync("404")).ReturnsAsync((AppUser?)null);

            // Act
            var result = await _controller.Manage(model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddRole_CreatesRoleAndRedirects()
        {
            // Arrange
            _roleServiceMock.Setup(r => r.CreateRoleAsync("Lecturer")).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddRole("Lecturer") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(ManageRolesController.Index), result.ActionName);
            _roleServiceMock.Verify(r => r.CreateRoleAsync("Lecturer"), Times.Once);
        }

        [Fact]
        public async Task AddRole_DoesNotCreate_WhenNameIsEmpty()
        {
            // Act
            var result = await _controller.AddRole("") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(ManageRolesController.Index), result.ActionName);
            _roleServiceMock.Verify(r => r.CreateRoleAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRole_DeletesRoleAndRedirects()
        {
            // Arrange
            _roleServiceMock.Setup(r => r.DeleteRoleAsync("Lecturer")).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteRole("Lecturer") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(ManageRolesController.Index), result.ActionName);
            _roleServiceMock.Verify(r => r.DeleteRoleAsync("Lecturer"), Times.Once);
        }
    }
}
