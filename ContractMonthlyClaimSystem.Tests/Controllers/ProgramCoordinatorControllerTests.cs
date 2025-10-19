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
    /// Unit tests for the ProgramCoordinatorController class.
    /// These tests verify all main controller actions — Index, ClaimDetails,
    /// AcceptClaim, RejectClaim, and DownloadFile — ensuring correct logic,
    /// proper interactions with services, and appropriate return results.
    /// </summary>
    public class ProgramCoordinatorControllerTests
    {
        private readonly Mock<IReviewerClaimService> _reviewerClaimServiceMock;
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly ProgramCoordinatorController _controller;

        public ProgramCoordinatorControllerTests()
        {
            // Mock IReviewerClaimService dependency to avoid real database calls.
            _reviewerClaimServiceMock = new Mock<IReviewerClaimService>();

            // Mock UserManager (requires a dummy IUserStore).
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

            // Create controller with mocked dependencies.
            _controller = new ProgramCoordinatorController(
                _reviewerClaimServiceMock.Object,
                _userManagerMock.Object
            );

            // Simulate a logged-in Program Coordinator.
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U1") }, "mock")
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            // Default user manager behavior for current user.
            _userManagerMock
                .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new AppUser { Id = "U1", UserName = "coordinator@cmcs.app" });
        }

        [Fact]
        public async Task Index_ReturnsViewWithReviewerClaimsViewModel()
        {
            // Arrange: prepare sample claims with varying states.
            var lecturer = new AppUser { Id = "L1", UserName = "lecturer@cmcs.app" };
            var module = new Module { Name = "Programming 2B" };

            var claims = new List<ContractClaim>
            {
                new()
                {
                    Id = 1,
                    LecturerUser = lecturer,
                    Module = module,
                    HoursWorked = 5,
                    HourlyRate = 100,
                    ClaimStatus = ClaimStatus.PENDING_CONFIRM,
                    ProgramCoordinatorUserId = null,
                },
                new()
                {
                    Id = 2,
                    LecturerUser = lecturer,
                    Module = module,
                    HoursWorked = 8,
                    HourlyRate = 200,
                    ClaimStatus = ClaimStatus.PENDING_CONFIRM,
                    ProgramCoordinatorUserId = "U1",
                },
                new()
                {
                    Id = 3,
                    LecturerUser = lecturer,
                    Module = module,
                    HoursWorked = 10,
                    HourlyRate = 250,
                    ClaimStatus = ClaimStatus.ACCEPTED,
                },
            };

            _reviewerClaimServiceMock.Setup(s => s.GetClaimsAsync()).ReturnsAsync(claims);

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert: verify correct view model population.
            Assert.NotNull(result);
            var model = Assert.IsType<ReviewerClaimsViewModel>(result.Model);
            Assert.Single(model.PendingClaims);
            Assert.Single(model.PendingConfirmClaims);
            Assert.Single(model.CompletedClaims);
        }

        [Fact]
        public async Task ClaimDetails_ReturnsViewWithDetails_WhenClaimExists()
        {
            // Arrange: create claim with linked lecturer, module, and files.
            var lecturer = new AppUser { UserName = "lecturer@cmcs.app" };
            var module = new Module { Name = "Networking 3A" };
            var claim = new ContractClaim
            {
                Id = 5,
                LecturerUser = lecturer,
                Module = module,
                HoursWorked = 10,
                HourlyRate = 200,
                LecturerComment = "Completed work",
                ClaimStatus = ClaimStatus.PENDING_CONFIRM,
            };
            var files = new List<UploadedFile> { new() { FileName = "proof.pdf" } };

            _reviewerClaimServiceMock.Setup(s => s.GetClaimAsync(5)).ReturnsAsync(claim);
            _reviewerClaimServiceMock.Setup(s => s.GetClaimFilesAsync(claim)).ReturnsAsync(files);

            // Act
            var result = await _controller.ClaimDetails(5) as ViewResult;

            // Assert: ensure correct view and model data.
            Assert.NotNull(result);
            var model = Assert.IsType<ReviewerClaimDetailsViewModel>(result.Model);
            Assert.Equal("proof.pdf", model.Files.First().FileName);
            Assert.Equal("Networking 3A", model.ModuleName);
        }

        [Fact]
        public async Task ClaimDetails_ReturnsNotFound_WhenClaimMissing()
        {
            // Arrange
            _reviewerClaimServiceMock
                .Setup(s => s.GetClaimAsync(404))
                .ReturnsAsync((ContractClaim?)null);

            // Act
            var result = await _controller.ClaimDetails(404);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AcceptClaim_RedirectsToIndex_WhenValid()
        {
            // Arrange: valid review
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(10, "U1", true, "approved"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AcceptClaim(10, "approved") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(ProgramCoordinatorController.Index), result.ActionName);
        }

        [Fact]
        public async Task AcceptClaim_RedirectsToIndex_WhenInvalid()
        {
            // Arrange: invalid claim
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(99, "U1", true, "invalid"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.AcceptClaim(99, "invalid") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(ProgramCoordinatorController.Index), result.ActionName);
        }

        [Fact]
        public async Task RejectClaim_RedirectsToIndex_WhenValid()
        {
            // Arrange
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(12, "U1", false, "reject"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RejectClaim(12, "reject") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(ProgramCoordinatorController.Index), result.ActionName);
        }

        [Fact]
        public async Task RejectClaim_RedirectsToIndex_WhenInvalid()
        {
            // Arrange
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(12, "U1", false, "reject"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.RejectClaim(12, "reject") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(ProgramCoordinatorController.Index), result.ActionName);
        }

        [Fact]
        public async Task DownloadFile_ReturnsFileResult_WhenFileExists()
        {
            // Arrange: mock valid file tuple (FileName, MemoryStream, ContentType)
            var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
            _reviewerClaimServiceMock
                .Setup(s => s.GetFileAsync(1))
                .ReturnsAsync(("claim.pdf", fileStream, "application/pdf"));

            // Act
            var result = await _controller.DownloadFile(1) as FileStreamResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("application/pdf", result.ContentType);
            Assert.Equal("claim.pdf", result.FileDownloadName);
        }

        [Fact]
        public async Task DownloadFile_ReturnsNotFound_WhenFileMissing()
        {
            // Arrange: simulate null tuple return
            _reviewerClaimServiceMock
                .Setup(s => s.GetFileAsync(99))
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
