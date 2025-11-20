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
    public class AcademicManagerControllerTests
    {
        private readonly Mock<IReviewerClaimService> _reviewerClaimServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly AcademicManagerController _controller;

        public AcademicManagerControllerTests()
        {
            _reviewerClaimServiceMock = new Mock<IReviewerClaimService>();
            _userServiceMock = new Mock<IUserService>();

            _controller = new AcademicManagerController(
                _reviewerClaimServiceMock.Object,
                _userServiceMock.Object
            );

            // Simulate logged-in Academic Manager using Identity.Name
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "manager@cmcs.app") }, "mock")
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            // Default GetUserAsync behavior
            _userServiceMock
                .Setup(s => s.GetUserAsync("manager@cmcs.app"))
                .ReturnsAsync(new AppUser { Id = 1, UserName = "manager@cmcs.app" });
        }

        // ---------------------------------------------------------
        // INDEX
        // ---------------------------------------------------------
        [Fact]
        public async Task Index_ReturnsViewWithReviewerClaimsViewModel()
        {
            var lecturer = new AppUser { UserName = "lecturer@cmcs.app" };
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
                    AcademicManagerUserId = null,
                },
                new()
                {
                    Id = 2,
                    LecturerUser = lecturer,
                    Module = module,
                    HoursWorked = 10,
                    HourlyRate = 200,
                    ClaimStatus = ClaimStatus.ACCEPTED,
                    AcademicManagerUserId = 1,
                },
            };

            _reviewerClaimServiceMock.Setup(s => s.GetClaimsAsync()).ReturnsAsync(claims);

            var result = await _controller.Index() as ViewResult;

            Assert.NotNull(result);

            var model = Assert.IsType<ReviewerClaimsViewModel>(result.Model);

            Assert.Single(model.PendingClaims); // claim #1
            Assert.Single(model.CompletedClaims); // claim #2
        }

        // ---------------------------------------------------------
        // CLAIM DETAILS
        // ---------------------------------------------------------
        [Fact]
        public async Task ClaimDetails_ReturnsViewWithDetails()
        {
            var claim = new ContractClaim
            {
                Id = 5,
                LecturerUser = new AppUser { UserName = "lecturer@cmcs.app" },
                Module = new Module { Name = "Programming 2B" },
                HoursWorked = 8,
                HourlyRate = 200,
                LecturerComment = "Done work",
                ClaimStatus = ClaimStatus.PENDING_CONFIRM,
            };

            var files = new List<UploadedFile> { new() { FileName = "proof.pdf" } };

            _reviewerClaimServiceMock.Setup(s => s.GetClaimAsync(5)).ReturnsAsync(claim);
            _reviewerClaimServiceMock.Setup(s => s.GetClaimFilesAsync(claim)).ReturnsAsync(files);

            var result = await _controller.ClaimDetails(5) as ViewResult;

            Assert.NotNull(result);

            var model = Assert.IsType<ReviewerClaimDetailsViewModel>(result.Model);
            Assert.Equal("Programming 2B", model.ModuleName);
            Assert.Equal("proof.pdf", model.Files.First().FileName);
        }

        [Fact]
        public async Task ClaimDetails_ReturnsNotFound_WhenClaimMissing()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.GetClaimAsync(123))
                .ReturnsAsync((ContractClaim?)null);

            var result = await _controller.ClaimDetails(123);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // ACCEPT CLAIM
        // ---------------------------------------------------------
        [Fact]
        public async Task AcceptClaim_RedirectsToIndex_WhenValid()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaimAsync(10, 1, true, "approved"))
                .ReturnsAsync(true);

            var result = await _controller.AcceptClaim(10, "approved") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(AcademicManagerController.Index), result.ActionName);
        }

        [Fact]
        public async Task AcceptClaim_RedirectsToIndex_WhenInvalid()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaimAsync(99, 1, true, "nope"))
                .ReturnsAsync(false);

            var result = await _controller.AcceptClaim(99, "nope") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(AcademicManagerController.Index), result.ActionName);
        }

        // ---------------------------------------------------------
        // REJECT CLAIM
        // ---------------------------------------------------------
        [Fact]
        public async Task RejectClaim_RedirectsToIndex_WhenValid()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaimAsync(8, 1, false, "reject"))
                .ReturnsAsync(true);

            var result = await _controller.RejectClaim(8, "reject") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(AcademicManagerController.Index), result.ActionName);
        }

        [Fact]
        public async Task RejectClaim_RedirectsToIndex_WhenInvalid()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaimAsync(8, 1, false, "reject"))
                .ReturnsAsync(false);

            var result = await _controller.RejectClaim(8, "reject") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(AcademicManagerController.Index), result.ActionName);
        }

        // ---------------------------------------------------------
        // DOWNLOAD FILE
        // ---------------------------------------------------------
        [Fact]
        public async Task DownloadFile_ReturnsFileStreamResult_WhenFileExists()
        {
            var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });

            _reviewerClaimServiceMock
                .Setup(s => s.GetFileAsync(1))
                .Returns(
                    Task.FromResult<(Stream FileStream, string ContentType, string FileName)?>(
                        (fileStream, "application/pdf", "claim.pdf")
                    )
                );

            var result = await _controller.DownloadFile(1) as FileStreamResult;

            Assert.NotNull(result);
            Assert.Equal("application/pdf", result.ContentType);
            Assert.Equal("claim.pdf", result.FileDownloadName);
        }

        [Fact]
        public async Task DownloadFile_ReturnsNotFound_WhenFileMissing()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.GetFileAsync(99))
                .Returns(
                    Task.FromResult<(Stream FileStream, string ContentType, string FileName)?>(null)
                );

            var result = await _controller.DownloadFile(99);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
