// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68f5452c-2788-800b-bbbc-175029690cfd

using System.Security.Claims;
using ContractMonthlyClaimSystem.Controllers;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.Auth;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ContractMonthlyClaimSystem.Tests.Controllers
{
    public class ManageModulesControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IModuleService> _moduleServiceMock;
        private readonly ManageModulesController _controller;

        public ManageModulesControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _moduleServiceMock = new Mock<IModuleService>();

            _controller = new ManageModulesController(
                _userServiceMock.Object,
                _moduleServiceMock.Object
            );

            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin@cmcs.app") }, "mock")
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
        }

        // ---------------------------------------------------------
        // INDEX
        // ---------------------------------------------------------
        [Fact]
        public async Task Index_ReturnsViewWithModulesAndLecturers()
        {
            var lecturers = new List<AppUser>
            {
                new()
                {
                    Id = 1,
                    FirstName = "John",
                    LastName = "Doe",
                },
                new()
                {
                    Id = 2,
                    FirstName = "Jane",
                    LastName = "Smith",
                },
            };

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

            _userServiceMock.Setup(s => s.GetUsersInRoleAsync("Lecturer")).ReturnsAsync(lecturers);

            _moduleServiceMock
                .Setup(s => s.GetModulesForLecturerAsync(1))
                .ReturnsAsync(modulesForL1);

            _moduleServiceMock
                .Setup(s => s.GetModulesForLecturerAsync(2))
                .ReturnsAsync(modulesForL2);

            _moduleServiceMock.Setup(s => s.GetModulesAsync()).ReturnsAsync(allModules);

            var result = await _controller.Index() as ViewResult;

            Assert.NotNull(result);
            var vm = Assert.IsType<ManageModulesIndexViewModel>(result.Model);

            Assert.Equal(2, vm.Lecturers.Count);
            Assert.Equal(2, vm.Modules.Count);
            Assert.NotNull(vm.NewModule);
        }

        // ---------------------------------------------------------
        // CREATE MODULE
        // ---------------------------------------------------------
        [Fact]
        public async Task CreateModule_AddsModule_AndRedirects()
        {
            var model = new ManageModulesIndexViewModel
            {
                NewModule = new Module { Id = 1, Name = "AI Systems" },
            };

            _moduleServiceMock
                .Setup(s => s.AddModuleAsync(It.IsAny<Module>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.CreateModule(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageModulesController.Index), result.ActionName);

            _moduleServiceMock.Verify(
                s => s.AddModuleAsync(It.Is<Module>(m => m.Name == "AI Systems")),
                Times.Once
            );
        }

        // ---------------------------------------------------------
        // DELETE MODULE
        // ---------------------------------------------------------
        [Fact]
        public async Task DeleteModule_RemovesModule_AndRedirects()
        {
            _moduleServiceMock.Setup(s => s.RemoveModuleAsync(3)).Returns(Task.CompletedTask);

            var result = await _controller.DeleteModule(3) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageModulesController.Index), result.ActionName);

            _moduleServiceMock.Verify(s => s.RemoveModuleAsync(3), Times.Once);
        }

        // ---------------------------------------------------------
        // MANAGE LECTURER MODULES VIEW
        // ---------------------------------------------------------
        [Fact]
        public async Task ManageLecturerModules_ReturnsViewWithDetailedModules()
        {
            var lecturer = new AppUser
            {
                Id = 5,
                FirstName = "Sam",
                LastName = "Williams",
            };

            var allModules = new List<Module>
            {
                new() { Id = 1, Name = "Programming 1A" },
                new() { Id = 2, Name = "Networking 2B" },
            };

            var lecturerModules = new List<LecturerModule>
            {
                new()
                {
                    LecturerUserId = 5,
                    ModuleId = 2,
                    HourlyRate = 500,
                    Module = new Module { Id = 2, Name = "Networking 2B" },
                },
            };

            _userServiceMock.Setup(s => s.GetUserAsync(5)).ReturnsAsync(lecturer);

            _moduleServiceMock.Setup(s => s.GetModulesAsync()).ReturnsAsync(allModules);

            _moduleServiceMock
                .Setup(s => s.GetLecturerModulesAsync(5))
                .ReturnsAsync(lecturerModules);

            var result = await _controller.ManageLecturerModules(5) as ViewResult;

            Assert.NotNull(result);
            var vm = Assert.IsType<ManageLecturerModulesViewModel>(result.Model);

            Assert.Equal(5, vm.LecturerId);
            Assert.Equal("Sam Williams", vm.LecturerName);
            Assert.Equal(2, vm.AllModules.Count);

            Assert.Single(vm.AssignedModuleIds);
            Assert.Contains(2, vm.AssignedModuleIds);

            Assert.Single(vm.AssignedModulesDetailed);
            Assert.Equal(500m, vm.AssignedModulesDetailed[0].HourlyRate);
            Assert.Equal("Networking 2B", vm.AssignedModulesDetailed[0].ModuleName);
        }

        [Fact]
        public async Task ManageLecturerModules_ReturnsNotFound_WhenLecturerMissing()
        {
            _userServiceMock.Setup(s => s.GetUserAsync(404)).ReturnsAsync((AppUser?)null);

            var result = await _controller.ManageLecturerModules(404);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // ADD MODULE TO LECTURER
        // ---------------------------------------------------------
        [Fact]
        public async Task AddLecturerModule_AssignsModule_AndRedirects()
        {
            _moduleServiceMock
                .Setup(s => s.AddLecturerModuleAsync(10, 7, 250m))
                .Returns(Task.CompletedTask);

            var result = await _controller.AddLecturerModule(10, 7, 250m) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageModulesController.ManageLecturerModules), result.ActionName);
            Assert.Equal(10, result.RouteValues!["id"]);
        }

        // ---------------------------------------------------------
        // REMOVE MODULE FROM LECTURER
        // ---------------------------------------------------------
        [Fact]
        public async Task RemoveLecturerModule_RemovesModule_AndRedirects()
        {
            _moduleServiceMock
                .Setup(s => s.RemoveLecturerModuleAsync(10, 7))
                .Returns(Task.CompletedTask);

            var result = await _controller.RemoveLecturerModule(10, 7) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageModulesController.ManageLecturerModules), result.ActionName);
            Assert.Equal(10, result.RouteValues!["id"]);
        }

        // ---------------------------------------------------------
        // UPDATE HOURLY RATE
        // ---------------------------------------------------------
        [Fact]
        public async Task UpdateLecturerModuleHourlyRate_UpdatesAndRedirects()
        {
            _moduleServiceMock
                .Setup(s => s.UpdateLecturerModuleHourlyRate(10, 7, 800m))
                .Returns(Task.CompletedTask);

            var result =
                await _controller.UpdateLecturerModuleHourlyRate(10, 7, 800m)
                as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageModulesController.ManageLecturerModules), result.ActionName);
            Assert.Equal(10, result.RouteValues!["id"]);

            _moduleServiceMock.Verify(
                s => s.UpdateLecturerModuleHourlyRate(10, 7, 800m),
                Times.Once
            );
        }
    }
}
