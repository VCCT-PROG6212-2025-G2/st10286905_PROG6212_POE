// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/s/t_691f47093e18819188fef6caccbbf3d7

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
    public class ManageLecturerModulesControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IModuleService> _moduleServiceMock;
        private readonly Mock<IHumanResourcesService> _hrServiceMock;

        private readonly ManageLecturerModulesController _controller;

        public ManageLecturerModulesControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _moduleServiceMock = new Mock<IModuleService>();
            _hrServiceMock = new Mock<IHumanResourcesService>();

            _controller = new ManageLecturerModulesController(
                _userServiceMock.Object,
                _moduleServiceMock.Object,
                _hrServiceMock.Object
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
            var vm = Assert.IsType<ManageLecturerModulesIndexViewModel>(result.Model);

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
            var model = new ManageLecturerModulesIndexViewModel
            {
                NewModule = new Module { Id = 1, Name = "AI Systems" },
            };

            var result = await _controller.CreateModule(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageLecturerModulesController.Index), result.ActionName);

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
            var result = await _controller.DeleteModule(3) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageLecturerModulesController.Index), result.ActionName);

            _moduleServiceMock.Verify(s => s.RemoveModuleAsync(3), Times.Once);
        }

        // ---------------------------------------------------------
        // MANAGE LECTURER VIEW
        // ---------------------------------------------------------
        [Fact]
        public async Task ManageLecturer_ReturnsViewWithDetailedModulesAndDetails()
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

            var lecturerDetails = new LecturerDetails
            {
                UserId = 5,
                ContactNumber = "0821234567",
                Address = "123 Example Road",
                BankDetails = "FNB - 123456",
            };

            _userServiceMock.Setup(s => s.GetUserAsync(5)).ReturnsAsync(lecturer);
            _moduleServiceMock.Setup(s => s.GetModulesAsync()).ReturnsAsync(allModules);
            _moduleServiceMock
                .Setup(s => s.GetLecturerModulesAsync(5))
                .ReturnsAsync(lecturerModules);
            _hrServiceMock.Setup(s => s.GetLecturerDetailsAsync(5)).ReturnsAsync(lecturerDetails);

            var result = await _controller.ManageLecturer(5) as ViewResult;

            Assert.NotNull(result);
            var vm = Assert.IsType<ManageLecturerViewModel>(result.Model);

            Assert.Equal(5, vm.LecturerId);
            Assert.Equal("Sam Williams", vm.LecturerName);

            Assert.Equal(2, vm.AllModules.Count);
            Assert.Single(vm.AssignedModuleIds);
            Assert.Contains(2, vm.AssignedModuleIds);

            Assert.Single(vm.AssignedModulesDetailed);
            Assert.Equal(500m, vm.AssignedModulesDetailed[0].HourlyRate);

            // NEW: Verify LecturerDetails is correctly mapped
            Assert.Equal("0821234567", vm.LecturerDetails.ContactNumber);
            Assert.Equal("123 Example Road", vm.LecturerDetails.Address);
            Assert.Equal("FNB - 123456", vm.LecturerDetails.BankDetails);
        }

        [Fact]
        public async Task ManageLecturer_ReturnsNotFound_WhenLecturerMissing()
        {
            _userServiceMock.Setup(s => s.GetUserAsync(404)).ReturnsAsync((AppUser?)null);

            var result = await _controller.ManageLecturer(404);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // UPDATE LECTURER DETAILS
        // ---------------------------------------------------------
        [Fact]
        public async Task ManageLecturerDetails_UpdatesDetails_AndRedirects()
        {
            var model = new ManageLecturerViewModel
            {
                LecturerId = 12,
                LecturerDetails = new LecturerDetailsViewModel
                {
                    ContactNumber = "0123456789",
                    Address = "New Address",
                    BankDetails = "Capitec 112233",
                },
            };

            var result = await _controller.ManageLecturerDetails(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageLecturerModulesController.ManageLecturer), result.ActionName);
            Assert.Equal(12, result.RouteValues!["Id"]);

            _hrServiceMock.Verify(
                s =>
                    s.SetLecturerDetailsAsync(
                        It.Is<LecturerDetails>(d =>
                            d.UserId == 12
                            && d.ContactNumber == "0123456789"
                            && d.Address == "New Address"
                            && d.BankDetails == "Capitec 112233"
                        )
                    ),
                Times.Once
            );
        }

        // ---------------------------------------------------------
        // ADD MODULE TO LECTURER
        // ---------------------------------------------------------
        [Fact]
        public async Task AddLecturerModule_AssignsModule_AndRedirects()
        {
            var result = await _controller.AddLecturerModule(10, 7, 250m) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageLecturerModulesController.ManageLecturer), result.ActionName);
            Assert.Equal(10, result.RouteValues!["id"]);

            _moduleServiceMock.Verify(s => s.AddLecturerModuleAsync(10, 7, 250m), Times.Once);
        }

        // ---------------------------------------------------------
        // REMOVE MODULE FROM LECTURER
        // ---------------------------------------------------------
        [Fact]
        public async Task RemoveLecturerModule_RemovesModule_AndRedirects()
        {
            var result = await _controller.RemoveLecturerModule(10, 7) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageLecturerModulesController.ManageLecturer), result.ActionName);
            Assert.Equal(10, result.RouteValues!["id"]);

            _moduleServiceMock.Verify(s => s.RemoveLecturerModuleAsync(10, 7), Times.Once);
        }

        // ---------------------------------------------------------
        // UPDATE HOURLY RATE
        // ---------------------------------------------------------
        [Fact]
        public async Task UpdateLecturerModuleHourlyRate_UpdatesAndRedirects()
        {
            var result =
                await _controller.UpdateLecturerModuleHourlyRate(10, 7, 800m)
                as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ManageLecturerModulesController.ManageLecturer), result.ActionName);
            Assert.Equal(10, result.RouteValues!["id"]);

            _moduleServiceMock.Verify(
                s => s.UpdateLecturerModuleHourlyRate(10, 7, 800m),
                Times.Once
            );
        }
    }
}
