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
    /// Unit tests for the AcademicManagerController class.
    /// Each test validates controller behavior such as view rendering,
    /// model building, interaction with services, and proper redirection.
    /// All external dependencies are mocked to ensure test isolation.
    /// </summary>
    public class AcademicManagerControllerTests
    {
        private readonly Mock<IReviewerClaimService> _reviewerClaimServiceMock;
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly AcademicManagerController _controller;

        public AcademicManagerControllerTests()
        {
            // Mock the required dependencies: reviewer claim service and user manager
            _reviewerClaimServiceMock = new Mock<IReviewerClaimService>();

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

            // Instantiate controller with mocks
            _controller = new AcademicManagerController(
                _reviewerClaimServiceMock.Object,
                _userManagerMock.Object
            );

            // Simulate a logged-in Academic Manager
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U1") }, "mock")
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            // Default user manager behavior for the current user
            _userManagerMock
                .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new AppUser { Id = "U1", UserName = "manager@cmcs.app" });
        }

        [Fact]
        public async Task Index_ReturnsViewWithReviewerClaimsViewModel()
        {
            // Arrange: prepare sample claims with different statuses
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
                },
                new()
                {
                    Id = 2,
                    LecturerUser = lecturer,
                    Module = module,
                    HoursWorked = 10,
                    HourlyRate = 200,
                    ClaimStatus = ClaimStatus.ACCEPTED,
                },
            };

            _reviewerClaimServiceMock.Setup(s => s.GetClaimsAsync()).ReturnsAsync(claims);

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<ReviewerClaimsViewModel>(result.Model);
            Assert.NotNull(model.PendingClaims);
            Assert.NotNull(model.CompletedClaims);
            Assert.Single(model.CompletedClaims);
        }

        [Fact]
        public async Task ClaimDetails_ReturnsViewWithClaimDetails()
        {
            // Arrange
            var lecturer = new AppUser { UserName = "lecturer@cmcs.app" };
            var module = new Module { Name = "Programming 2B" };
            var claim = new ContractClaim
            {
                Id = 5,
                LecturerUser = lecturer,
                Module = module,
                HoursWorked = 8,
                HourlyRate = 200,
                LecturerComment = "Done work",
                ClaimStatus = ClaimStatus.PENDING_CONFIRM,
            };
            var files = new List<UploadedFile> { new() { FileName = "proof.pdf" } };

            _reviewerClaimServiceMock.Setup(s => s.GetClaimAsync(5)).ReturnsAsync(claim);
            _reviewerClaimServiceMock.Setup(s => s.GetClaimFilesAsync(claim)).ReturnsAsync(files);

            // Act
            var result = await _controller.ClaimDetails(5) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<ReviewerClaimDetailsViewModel>(result.Model);
            Assert.Equal("proof.pdf", model.Files.First().FileName);
            Assert.Equal("Programming 2B", model.ModuleName);
        }

        [Fact]
        public async Task ClaimDetails_ReturnsNotFound_WhenClaimMissing()
        {
            // Arrange: mock null result
            _reviewerClaimServiceMock
                .Setup(s => s.GetClaimAsync(123))
                .ReturnsAsync((ContractClaim?)null);

            // Act
            var result = await _controller.ClaimDetails(123);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AcceptClaim_RedirectsToIndex_WhenValid()
        {
            // Arrange
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(10, "U1", true, "approved"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AcceptClaim(10, "approved") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(AcademicManagerController.Index), result.ActionName);
        }

        [Fact]
        public async Task AcceptClaim_RedirectsToIndex_WhenInvalid()
        {
            // Arrange: service returns false
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(99, "U1", true, "nope"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.AcceptClaim(99, "nope") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(AcademicManagerController.Index), result.ActionName);
        }

        [Fact]
        public async Task RejectClaim_RedirectsToIndex_WhenValid()
        {
            // Arrange
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(8, "U1", false, "reject"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RejectClaim(8, "reject") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(AcademicManagerController.Index), result.ActionName);
        }

        [Fact]
        public async Task RejectClaim_RedirectsToIndex_WhenInvalid()
        {
            // Arrange
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(8, "U1", false, "reject"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.RejectClaim(8, "reject") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(AcademicManagerController.Index), result.ActionName);
        }

        [Fact]
        public async Task DownloadFile_ReturnsFileResult_WhenFileExists()
        {
            // Arrange: simulate a file tuple returned by the service
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
            // Arrange: simulate no file found
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
