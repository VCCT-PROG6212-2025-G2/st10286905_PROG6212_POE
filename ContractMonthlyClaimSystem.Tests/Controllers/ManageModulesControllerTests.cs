using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ContractMonthlyClaimSystem.Controllers;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ContractMonthlyClaimSystem.Tests.Controllers
{
    /// <summary>
    /// Unit tests for the ManageModulesController class.
    /// These tests verify all key controller methods: listing lecturers and modules,
    /// creating and deleting modules, and managing lecturer-module assignments.
    /// All external dependencies are mocked for isolation and determinism.
    /// </summary>
    public class ManageModulesControllerTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<IModuleService> _moduleServiceMock;
        private readonly ManageModulesController _controller;

        public ManageModulesControllerTests()
        {
            // Mock the UserManager (requires IUserStore).
            var store = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                store.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );

            // Mock the module service.
            _moduleServiceMock = new Mock<IModuleService>();

            // Instantiate the controller with mocked dependencies.
            _controller = new ManageModulesController(
                _userManagerMock.Object,
                _moduleServiceMock.Object
            );

            // Simulate a logged-in Admin or ProgramCoordinator.
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U1") }, "mock")
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
        }

        [Fact]
        public async Task Index_ReturnsViewWithModulesAndLecturers()
        {
            // Arrange: mock lecturers and their assigned modules.
            var lecturer1 = new AppUser { Id = "L1", UserName = "lecturer1@cmcs.app" };
            var lecturer2 = new AppUser { Id = "L2", UserName = "lecturer2@cmcs.app" };
            var lecturers = new List<AppUser> { lecturer1, lecturer2 };

            var modulesForL1 = new List<Module>
            {
                new() { Id = 1, Name = "Programming 1A" },
            };
            var modulesForL2 = new List<Module>
            {
                new() { Id = 2, Name = "Networking 2B" },
            };
            var allModules = new List<Module>
            {
                new() { Id = 1, Name = "Programming 1A" },
                new() { Id = 2, Name = "Networking 2B" },
            };

            _userManagerMock.Setup(u => u.GetUsersInRoleAsync("Lecturer")).ReturnsAsync(lecturers);
            _moduleServiceMock
                .Setup(s => s.GetModulesForLecturerAsync("L1"))
                .ReturnsAsync(modulesForL1);
            _moduleServiceMock
                .Setup(s => s.GetModulesForLecturerAsync("L2"))
                .ReturnsAsync(modulesForL2);
            _moduleServiceMock.Setup(s => s.GetModulesAsync()).ReturnsAsync(allModules);

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert: confirm view and view model data.
            Assert.NotNull(result);
            var model = Assert.IsType<ManageModulesIndexViewModel>(result.Model);
            Assert.Equal(2, model.Lecturers.Count);
            Assert.Equal(2, model.Modules.Count);
            Assert.NotNull(model.NewModule);
        }

        [Fact]
        public async Task CreateModule_AddsModuleAndRedirects()
        {
            // Arrange: create view model with new module.
            var model = new ManageModulesIndexViewModel
            {
                NewModule = new Module { Id = 1, Name = "New Module" },
            };

            _moduleServiceMock
                .Setup(s => s.AddModuleAsync(It.IsAny<Module>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateModule(model) as RedirectToActionResult;

            // Assert: ensure redirect and call verification.
            Assert.NotNull(result);
            Assert.Equal(nameof(ManageModulesController.Index), result.ActionName);
            _moduleServiceMock.Verify(s => s.AddModuleAsync(It.IsAny<Module>()), Times.Once);
        }

        [Fact]
        public async Task DeleteModule_RemovesModuleAndRedirects()
        {
            // Arrange
            _moduleServiceMock.Setup(s => s.RemoveModuleAsync(5)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteModule(5) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(ManageModulesController.Index), result.ActionName);
            _moduleServiceMock.Verify(s => s.RemoveModuleAsync(5), Times.Once);
        }

        [Fact]
        public async Task ManageLecturerModules_ReturnsViewWithLecturerModules()
        {
            // Arrange: lecturer exists with assigned modules.
            var lecturer = new AppUser { Id = "L1", UserName = "lecturer@cmcs.app" };
            var allModules = new List<Module>
            {
                new() { Id = 1, Name = "Programming 1A" },
                new() { Id = 2, Name = "Networking 2B" },
            };
            var assignedModules = new List<Module> { allModules[0] };

            _userManagerMock.Setup(u => u.FindByIdAsync("L1")).ReturnsAsync(lecturer);
            _moduleServiceMock.Setup(s => s.GetModulesAsync()).ReturnsAsync(allModules);
            _moduleServiceMock
                .Setup(s => s.GetModulesForLecturerAsync("L1"))
                .ReturnsAsync(assignedModules);

            // Act
            var result = await _controller.ManageLecturerModules("L1") as ViewResult;

            // Assert: verify correct lecturer and module data.
            Assert.NotNull(result);
            var model = Assert.IsType<ManageLecturerModulesViewModel>(result.Model);
            Assert.Equal("lecturer@cmcs.app", model.LecturerName);
            Assert.Single(model.AssignedModuleIds);
            Assert.Equal(2, model.AllModules.Count);
        }

        [Fact]
        public async Task ManageLecturerModules_ReturnsNotFound_WhenLecturerMissing()
        {
            // Arrange: lecturer not found
            _userManagerMock.Setup(u => u.FindByIdAsync("404")).ReturnsAsync((AppUser?)null);

            // Act
            var result = await _controller.ManageLecturerModules("404");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddLecturerModule_AssignsModuleAndRedirects()
        {
            // Arrange
            _moduleServiceMock
                .Setup(s => s.AddLecturerModuleAsync("L1", 10))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddLecturerModule("L1", 10) as RedirectToActionResult;

            // Assert: ensure redirect back to ManageLecturerModules.
            Assert.NotNull(result);
            Assert.Equal(nameof(ManageModulesController.ManageLecturerModules), result.ActionName);
            Assert.Equal("L1", result.RouteValues!["id"]);
        }

        [Fact]
        public async Task RemoveLecturerModule_RemovesModuleAndRedirects()
        {
            // Arrange
            _moduleServiceMock
                .Setup(s => s.RemoveLecturerModuleAsync("L1", 10))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RemoveLecturerModule("L1", 10) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(ManageModulesController.ManageLecturerModules), result.ActionName);
            Assert.Equal("L1", result.RouteValues!["id"]);
        }
    }
}
