// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68f5452c-2788-800b-bbbc-175029690cfd

using System.IO;
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
    public class ProgramCoordinatorControllerTests
    {
        private readonly Mock<IReviewerClaimService> _reviewerClaimServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly ProgramCoordinatorController _controller;

        public ProgramCoordinatorControllerTests()
        {
            _reviewerClaimServiceMock = new Mock<IReviewerClaimService>();
            _userServiceMock = new Mock<IUserService>();

            _controller = new ProgramCoordinatorController(
                _reviewerClaimServiceMock.Object,
                _userServiceMock.Object
            );

            // Simulate logged-in Program Coordinator
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, "coordinator@cmcs.app") },
                    "mock"
                )
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            // Default "logged-in" user
            _userServiceMock
                .Setup(s => s.GetUserAsync("coordinator@cmcs.app"))
                .ReturnsAsync(
                    new AppUser
                    {
                        Id = 10,
                        FirstName = "Prog",
                        LastName = "Coord",
                    }
                );
        }

        // ---------------------------------------------------------
        // INDEX
        // ---------------------------------------------------------
        [Fact]
        public async Task Index_ReturnsViewWithReviewerClaimsViewModel()
        {
            var lecturer = new AppUser { FirstName = "John", LastName = "Doe" };
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
                    ProgramCoordinatorUserId = 10,
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

            var result = await _controller.Index() as ViewResult;

            Assert.NotNull(result);

            var vm = Assert.IsType<ReviewerClaimsViewModel>(result.Model);
            Assert.Single(vm.PendingClaims);
            Assert.Single(vm.PendingConfirmClaims);
            Assert.Single(vm.CompletedClaims);
        }

        // ---------------------------------------------------------
        // CLAIM DETAILS
        // ---------------------------------------------------------
        [Fact]
        public async Task ClaimDetails_ReturnsViewWithDetails_WhenClaimExists()
        {
            var lecturer = new AppUser { FirstName = "Alice", LastName = "Green" };
            var module = new Module { Name = "Networking 3A" };
            var files = new List<UploadedFile> { new() { FileName = "proof.pdf" } };

            var claim = new ContractClaim
            {
                Id = 99,
                LecturerUser = lecturer,
                Module = module,
                HoursWorked = 10,
                HourlyRate = 200,
                LecturerComment = "Completed",
                ProgramCoordinatorUser = new AppUser { FirstName = "Prog", LastName = "Coord" },
                AcademicManagerUser = new AppUser { FirstName = "Admin", LastName = "Manager" },
                ClaimStatus = ClaimStatus.PENDING_CONFIRM,
            };

            _reviewerClaimServiceMock.Setup(s => s.GetClaimAsync(99)).ReturnsAsync(claim);
            _reviewerClaimServiceMock.Setup(s => s.GetClaimFilesAsync(claim)).ReturnsAsync(files);

            var result = await _controller.ClaimDetails(99) as ViewResult;

            Assert.NotNull(result);

            var vm = Assert.IsType<ReviewerClaimDetailsViewModel>(result.Model);
            Assert.Equal("proof.pdf", vm.Files.First().FileName);
            Assert.Equal("Networking 3A", vm.ModuleName);
            Assert.Equal("Alice Green", vm.LecturerName);
        }

        [Fact]
        public async Task ClaimDetails_ReturnsNotFound_WhenClaimDoesNotExist()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.GetClaimAsync(404))
                .ReturnsAsync((ContractClaim?)null);

            var result = await _controller.ClaimDetails(404);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // ACCEPT CLAIM
        // ---------------------------------------------------------
        [Fact]
        public async Task AcceptClaim_Redirects_WhenValid()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(10, 10, true, "ok"))
                .ReturnsAsync(true);

            var result = await _controller.AcceptClaim(10, "ok") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ProgramCoordinatorController.Index), result.ActionName);
        }

        [Fact]
        public async Task AcceptClaim_Redirects_WhenInvalid()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(88, 10, true, "bad"))
                .ReturnsAsync(false);

            var result = await _controller.AcceptClaim(88, "bad") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ProgramCoordinatorController.Index), result.ActionName);
        }

        // ---------------------------------------------------------
        // REJECT CLAIM
        // ---------------------------------------------------------
        [Fact]
        public async Task RejectClaim_Redirects_WhenValid()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(5, 10, false, "reject"))
                .ReturnsAsync(true);

            var result = await _controller.RejectClaim(5, "reject") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ProgramCoordinatorController.Index), result.ActionName);
        }

        [Fact]
        public async Task RejectClaim_Redirects_WhenInvalid()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.ReviewClaim(5, 10, false, "reject"))
                .ReturnsAsync(false);

            var result = await _controller.RejectClaim(5, "reject") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(ProgramCoordinatorController.Index), result.ActionName);
        }

        // ---------------------------------------------------------
        // FILE DOWNLOAD
        // ---------------------------------------------------------
        [Fact]
        public async Task DownloadFile_ReturnsFileStreamResult_WhenFileExists()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            _reviewerClaimServiceMock
                .Setup(s => s.GetFileAsync(1))
                .Returns(
                    Task.FromResult<(
                        string FileName,
                        MemoryStream FileStream,
                        string ContentType
                    )?>(("claim.pdf", stream, "application/pdf"))
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
                    Task.FromResult<(
                        string FileName,
                        MemoryStream FileStream,
                        string ContentType
                    )?>(null)
                );

            var result = await _controller.DownloadFile(404);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
