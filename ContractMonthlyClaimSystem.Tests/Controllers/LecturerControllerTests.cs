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
    /// Unit tests for the LecturerController class.
    /// These tests focus on verifying controller logic, including model handling,
    /// validation, and correct usage of injected services.
    /// All dependencies are mocked to isolate controller behavior.
    /// </summary>
    public class LecturerControllerTests
    {
        private readonly Mock<ILecturerClaimService> _claimServiceMock;
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly LecturerController _controller;

        public LecturerControllerTests()
        {
            // Mock UserManager (requires a fake IUserStore)
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

            // Mock the LecturerClaimService interface
            _claimServiceMock = new Mock<ILecturerClaimService>();

            // Create the controller instance with mocks
            _controller = new LecturerController(_claimServiceMock.Object, _userManagerMock.Object);

            // Setup a fake HttpContext with a logged-in lecturer
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "L1") }, "mock")
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            // Default UserManager behavior
            _userManagerMock
                .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new AppUser { Id = "L1", UserName = "lecturer@cmcs.app" });
        }

        [Fact]
        public async Task Index_ReturnsViewWithCorrectModel()
        {
            // Arrange: mock claim data for the lecturer
            var claims = new List<ContractClaim>
            {
                new()
                {
                    Id = 1,
                    LecturerUserId = "L1",
                    HoursWorked = 10,
                    HourlyRate = 100,
                    Module = new Module { Name = "PROG6212" },
                    ClaimStatus = ClaimStatus.PENDING_CONFIRM,
                },
                new()
                {
                    Id = 2,
                    LecturerUserId = "L1",
                    HoursWorked = 5,
                    HourlyRate = 200,
                    Module = new Module { Name = "CLDV6212" },
                    ClaimStatus = ClaimStatus.ACCEPTED,
                },
            };

            _claimServiceMock.Setup(s => s.GetClaimsForLecturerAsync("L1")).ReturnsAsync(claims);

            // Act: call Index()
            var result = await _controller.Index() as ViewResult;

            // Assert: view should contain a valid LecturerClaimsViewModel
            Assert.NotNull(result);
            var model = Assert.IsType<LecturerClaimsViewModel>(result.Model);
            Assert.Single(model.PendingClaims);
            Assert.Single(model.CompletedClaims);
        }

        [Fact]
        public async Task CreateClaim_Get_ReturnsViewWithModules()
        {
            // Arrange
            var modules = new List<Module>
            {
                new() { Id = 1, Name = "Programming 2B" },
            };
            _claimServiceMock.Setup(s => s.GetModulesForLecturerAsync("L1")).ReturnsAsync(modules);

            // Act
            var result = await _controller.CreateClaim() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<CreateClaimViewModel>(result.Model);
            Assert.Single(model.Modules);
        }

        [Fact]
        public async Task CreateClaim_Post_InvalidModel_ReturnsViewWithModules()
        {
            // Arrange: force model validation failure
            _controller.ModelState.AddModelError("HoursWorked", "Required");
            var model = new CreateClaimViewModel();

            _claimServiceMock
                .Setup(s => s.GetModulesForLecturerAsync("L1"))
                .ReturnsAsync(
                    new List<Module>
                    {
                        new() { Id = 1, Name = "Networking 3A" },
                    }
                );

            // Act
            var result = await _controller.CreateClaim(model) as ViewResult;

            // Assert: should return the same view for correction
            Assert.NotNull(result);
            var vm = Assert.IsType<CreateClaimViewModel>(result.Model);
            Assert.Single(vm.Modules);
        }

        [Fact]
        public async Task CreateClaim_Post_ValidModel_RedirectsToIndex()
        {
            // Arrange
            var model = new CreateClaimViewModel
            {
                ModuleId = 1,
                HoursWorked = 8,
                HourlyRate = 150,
                LecturerComment = "Work done",
                Files = new List<IFormFile>(),
            };
            var claim = new ContractClaim { Id = 10, LecturerUserId = "L1" };

            _claimServiceMock.Setup(s => s.CreateClaimAsync("L1", model)).ReturnsAsync(claim);
            _claimServiceMock
                .Setup(s => s.AddFilesToClaimAsync(claim, model.Files))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateClaim(model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(LecturerController.Index), result.ActionName);
        }

        [Fact]
        public async Task ClaimDetails_ReturnsViewWithDetails()
        {
            // Arrange
            var claim = new ContractClaim
            {
                Id = 1,
                LecturerUserId = "L1",
                HoursWorked = 10,
                HourlyRate = 100,
                LecturerComment = "Done",
                Module = new Module { Name = "PROG6212" },
                ClaimStatus = ClaimStatus.PENDING_CONFIRM,
            };
            var files = new List<UploadedFile> { new() { FileName = "proof.pdf" } };

            _claimServiceMock.Setup(s => s.GetClaimAsync(1, "L1")).ReturnsAsync(claim);
            _claimServiceMock.Setup(s => s.GetClaimFilesAsync(claim)).ReturnsAsync(files);

            // Act
            var result = await _controller.ClaimDetails(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var vm = Assert.IsType<LecturerClaimDetailsViewModel>(result.Model);
            Assert.Equal("proof.pdf", vm.Files.First().FileName);
        }

        [Fact]
        public async Task ClaimDetails_ReturnsNotFound_WhenClaimMissing()
        {
            // Arrange
            _claimServiceMock
                .Setup(s => s.GetClaimAsync(1, "L1"))
                .ReturnsAsync((ContractClaim?)null);

            // Act
            var result = await _controller.ClaimDetails(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadFile_ReturnsFileResult_WhenFileExists()
        {
            // Arrange
            var fileData = new MemoryStream(new byte[] { 1, 2, 3 });
            var fileTuple = ("doc.pdf", fileData, "application/pdf");

            _claimServiceMock.Setup(s => s.GetFileAsync(1, "L1")).ReturnsAsync(fileTuple);

            // Act
            var result = await _controller.DownloadFile(1) as FileStreamResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("application/pdf", result.ContentType);
            Assert.Equal("doc.pdf", result.FileDownloadName);
        }

        [Fact]
        public async Task DownloadFile_ReturnsNotFound_WhenFileMissing()
        {
            // Arrange
            _claimServiceMock
                .Setup(s => s.GetFileAsync(99, "L1"))
                .Returns(
                    Task.FromResult<(
                        string FileName,
                        MemoryStream FileStream,
                        string ContentType
                    )?>(null)
                );

            // Act
            var result = await _controller.DownloadFile(99);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
