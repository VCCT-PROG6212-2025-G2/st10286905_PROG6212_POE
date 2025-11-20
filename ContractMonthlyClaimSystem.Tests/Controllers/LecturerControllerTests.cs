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
    public class LecturerControllerTests
    {
        private readonly Mock<ILecturerClaimService> _claimServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly LecturerController _controller;

        public LecturerControllerTests()
        {
            _claimServiceMock = new Mock<ILecturerClaimService>();
            _userServiceMock = new Mock<IUserService>();

            _controller = new LecturerController(_claimServiceMock.Object, _userServiceMock.Object);

            // Fake HTTP user
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, "lecturer@cmcs.app") },
                    "mock"
                )
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            // Default user lookup
            _userServiceMock
                .Setup(s => s.GetUserAsync("lecturer@cmcs.app"))
                .ReturnsAsync(new AppUser { Id = 1, UserName = "lecturer@cmcs.app" });
        }

        // ---------------------------------------------------------
        // INDEX
        // ---------------------------------------------------------
        [Fact]
        public async Task Index_ReturnsViewWithCorrectModel()
        {
            var claims = new List<ContractClaim>
            {
                new()
                {
                    Id = 1,
                    LecturerUserId = 1,
                    HoursWorked = 10,
                    HourlyRate = 100,
                    Module = new Module { Name = "PROG6212" },
                    ClaimStatus = ClaimStatus.PENDING_CONFIRM,
                },
                new()
                {
                    Id = 2,
                    LecturerUserId = 1,
                    HoursWorked = 5,
                    HourlyRate = 200,
                    Module = new Module { Name = "CLDV6212" },
                    ClaimStatus = ClaimStatus.ACCEPTED,
                },
            };

            _claimServiceMock.Setup(s => s.GetClaimsForLecturerAsync(1)).ReturnsAsync(claims);

            var result = await _controller.Index() as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<LecturerClaimsViewModel>(result.Model);

            Assert.Single(model.PendingClaims);
            Assert.Single(model.CompletedClaims);
        }

        // ---------------------------------------------------------
        // GET HOURLY RATE
        // ---------------------------------------------------------
        [Fact]
        public async Task GetHourlyRate_ReturnsJson_WhenRateExists()
        {
            // Arrange
            const int moduleId = 42;
            const decimal expectedRate = 500m;

            _claimServiceMock
                .Setup(s => s.GetLecturerHourlyRateAsync(1, moduleId))
                .ReturnsAsync(expectedRate);

            // Act
            var result = await _controller.GetHourlyRate(moduleId);

            // Assert
            var json = Assert.IsType<JsonResult>(result);
            Assert.NotNull(json.Value);

            // Anonymous type, so use reflection to read the property.
            var value = json.Value!;
            var prop = value.GetType().GetProperty("hourlyRate");
            Assert.NotNull(prop);

            var hourlyRate = (decimal?)prop!.GetValue(value);
            Assert.Equal(expectedRate, hourlyRate);
        }

        [Fact]
        public async Task GetHourlyRate_ReturnsNotFound_WhenRateMissing()
        {
            // Arrange
            const int moduleId = 42;

            _claimServiceMock
                .Setup(s => s.GetLecturerHourlyRateAsync(1, moduleId))
                .ReturnsAsync((decimal?)null);

            // Act
            var result = await _controller.GetHourlyRate(moduleId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetHourlyRate_ReturnsNotFound_WhenLecturerNotFound()
        {
            // Arrange
            const int moduleId = 42;

            // Override the default setup: user lookup now returns null
            _userServiceMock
                .Setup(s => s.GetUserAsync("lecturer@cmcs.app"))
                .ReturnsAsync((AppUser?)null);

            // Act
            var result = await _controller.GetHourlyRate(moduleId);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            // Optional: ensure the claim service was never called
            _claimServiceMock.Verify(
                s => s.GetLecturerHourlyRateAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        // ---------------------------------------------------------
        // CREATE CLAIM - GET
        // ---------------------------------------------------------
        [Fact]
        public async Task CreateClaim_Get_ReturnsViewWithModules()
        {
            var modules = new List<Module>
            {
                new() { Id = 1, Name = "Programming 2B" },
            };

            _claimServiceMock.Setup(s => s.GetModulesForLecturerAsync(1)).ReturnsAsync(modules);

            var result = await _controller.CreateClaim() as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<CreateClaimViewModel>(result.Model);
            Assert.Single(model.Modules);
        }

        // ---------------------------------------------------------
        // CREATE CLAIM - POST INVALID
        // ---------------------------------------------------------
        [Fact]
        public async Task CreateClaim_Post_InvalidModel_ReturnsViewWithModules()
        {
            _controller.ModelState.AddModelError("HoursWorked", "Required");

            var model = new CreateClaimViewModel();

            _claimServiceMock
                .Setup(s => s.GetModulesForLecturerAsync(1))
                .ReturnsAsync(
                    new List<Module>
                    {
                        new() { Id = 1, Name = "Networking 3A" },
                    }
                );

            var result = await _controller.CreateClaim(model) as ViewResult;

            Assert.NotNull(result);
            var vm = Assert.IsType<CreateClaimViewModel>(result.Model);
            Assert.Single(vm.Modules);
        }

        // ---------------------------------------------------------
        // CREATE CLAIM - POST VALID
        // ---------------------------------------------------------
        [Fact]
        public async Task CreateClaim_Post_ValidModel_RedirectsToIndex()
        {
            var model = new CreateClaimViewModel
            {
                ModuleId = 1,
                HoursWorked = 8,
                HourlyRate = 150,
                LecturerComment = "Work done",
                Files = new List<IFormFile>(),
            };

            var claim = new ContractClaim { Id = 10, LecturerUserId = 1 };

            _claimServiceMock.Setup(s => s.CreateClaimAsync(1, model)).ReturnsAsync(claim);

            _claimServiceMock
                .Setup(s => s.AddFilesToClaimAsync(claim, model.Files))
                .Returns(Task.CompletedTask);

            var result = await _controller.CreateClaim(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(LecturerController.Index), result.ActionName);
        }

        // ---------------------------------------------------------
        // CLAIM DETAILS
        // ---------------------------------------------------------
        [Fact]
        public async Task ClaimDetails_ReturnsViewWithDetails()
        {
            var claim = new ContractClaim
            {
                Id = 1,
                LecturerUserId = 1,
                HoursWorked = 10,
                HourlyRate = 100,
                LecturerComment = "Done",
                Module = new Module { Name = "PROG6212" },
                ClaimStatus = ClaimStatus.PENDING_CONFIRM,
            };

            var files = new List<UploadedFile> { new() { FileName = "proof.pdf" } };

            _claimServiceMock.Setup(s => s.GetClaimAsync(1, 1)).ReturnsAsync(claim);
            _claimServiceMock.Setup(s => s.GetClaimFilesAsync(claim)).ReturnsAsync(files);

            var result = await _controller.ClaimDetails(1) as ViewResult;

            Assert.NotNull(result);
            var vm = Assert.IsType<LecturerClaimDetailsViewModel>(result.Model);
            Assert.Equal("proof.pdf", vm.Files.First().FileName);
        }

        [Fact]
        public async Task ClaimDetails_ReturnsNotFound_WhenClaimMissing()
        {
            _claimServiceMock.Setup(s => s.GetClaimAsync(1, 1)).ReturnsAsync((ContractClaim?)null);

            var result = await _controller.ClaimDetails(1);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // FILE DOWNLOAD
        // ---------------------------------------------------------
        [Fact]
        public async Task DownloadFile_ReturnsFileResult_WhenFileExists()
        {
            var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });

            _claimServiceMock
                .Setup(s => s.GetFileAsync(1, 1))
                .Returns(
                    Task.FromResult<(Stream FileStream, string ContentType, string FileName)?>(
                        (fileStream, "application/pdf", "doc.pdf")
                    )
                );

            var result = await _controller.DownloadFile(1) as FileStreamResult;

            Assert.NotNull(result);
            Assert.Equal("application/pdf", result.ContentType);
            Assert.Equal("doc.pdf", result.FileDownloadName);
        }

        [Fact]
        public async Task DownloadFile_ReturnsNotFound_WhenFileMissing()
        {
            _claimServiceMock
                .Setup(s => s.GetFileAsync(99, 1))
                .Returns(
                    Task.FromResult<(Stream FileStream, string ContentType, string FileName)?>(null)
                );

            var result = await _controller.DownloadFile(99);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
