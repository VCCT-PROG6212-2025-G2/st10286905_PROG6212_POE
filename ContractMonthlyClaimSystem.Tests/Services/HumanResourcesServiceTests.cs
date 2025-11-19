// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/691edcd1-3058-800b-bbcd-c3ae3566b589

using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContractMonthlyClaimSystem.Tests.Services
{
    public class HumanResourcesServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IReviewerClaimService> _reviewerMock;
        private readonly HumanResourcesService _service;

        public HumanResourcesServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _reviewerMock = new Mock<IReviewerClaimService>(MockBehavior.Strict);

            _service = new HumanResourcesService(_context, _reviewerMock.Object);
        }

        // -------------------------------------------------------
        // GET LECTURER DETAILS
        // -------------------------------------------------------
        [Fact]
        public async Task GetLecturerDetailsAsync_ReturnsDetails()
        {
            var details = new LecturerDetails { UserId = 1, ContactNumber = "123" };
            _context.LecturerDetails.Add(details);
            await _context.SaveChangesAsync();

            var result = await _service.GetLecturerDetailsAsync(1);

            Assert.NotNull(result);
            Assert.Equal("123", result!.ContactNumber);
        }

        // -------------------------------------------------------
        // SET LECTURER DETAILS
        // -------------------------------------------------------
        [Fact]
        public async Task SetLecturerDetailsAsync_AddsWhenNotExisting()
        {
            var details = new LecturerDetails { UserId = 10, ContactNumber = "555" };

            await _service.SetLecturerDetailsAsync(details);

            Assert.Single(_context.LecturerDetails);
            Assert.Equal("555", _context.LecturerDetails.First().ContactNumber);
        }

        [Fact]
        public async Task SetLecturerDetailsAsync_UpdatesWhenExisting()
        {
            var details = new LecturerDetails { UserId = 5, ContactNumber = "111" };
            _context.LecturerDetails.Add(details);
            await _context.SaveChangesAsync();

            var updated = new LecturerDetails { UserId = 5, ContactNumber = "999" };
            await _service.SetLecturerDetailsAsync(updated);

            Assert.Single(_context.LecturerDetails);
            Assert.Equal("999", _context.LecturerDetails.First().ContactNumber);
        }

        // -------------------------------------------------------
        // AUTO REVIEW CLAIMS
        // -------------------------------------------------------
        [Fact]
        public async Task AutoReviewClaimsForReviewersAsync_CallsServiceForEachReviewer()
        {
            _context.AutoReviewRules.Add(new AutoReviewRule { ReviewerId = 1 });
            _context.AutoReviewRules.Add(new AutoReviewRule { ReviewerId = 2 });
            await _context.SaveChangesAsync();

            _reviewerMock
                .Setup(r => r.AutoReviewPendingClaimsAsync(1))
                .ReturnsAsync((pending: 5, reviewed: 3));
            _reviewerMock
                .Setup(r => r.AutoReviewPendingClaimsAsync(2))
                .ReturnsAsync((pending: 2, reviewed: 2));

            await _service.AutoReviewClaimsForReviewersAsync();

            _reviewerMock.Verify(r => r.AutoReviewPendingClaimsAsync(1), Times.Once);
            _reviewerMock.Verify(r => r.AutoReviewPendingClaimsAsync(2), Times.Once);
        }

        // -------------------------------------------------------
        // GET APPROVED CLAIMS
        // -------------------------------------------------------
        [Fact]
        public async Task GetApprovedClaimsAsync_ReturnsOnlyAccepted()
        {
            _context.ContractClaims.Add(
                new ContractClaim { Id = 1, ClaimStatus = ClaimStatus.ACCEPTED }
            );
            _context.ContractClaims.Add(
                new ContractClaim { Id = 2, ClaimStatus = ClaimStatus.REJECTED }
            );
            await _context.SaveChangesAsync();

            var result = await _service.GetApprovedClaimsAsync();

            Assert.Single(result);
            Assert.Equal(1, result.First().Id);
        }

        // -------------------------------------------------------
        // GENERATE PDF — NULL CASES
        // -------------------------------------------------------
        [Fact]
        public async Task GenerateClaimInvoicePdfAsync_ReturnsNull_WhenClaimNotFound()
        {
            _reviewerMock.Setup(r => r.GetClaimAsync(123)).ReturnsAsync((ContractClaim?)null);

            var result = await _service.GenerateClaimInvoicePdfAsync(123);

            Assert.Null(result);
        }

        [Fact]
        public async Task GenerateClaimInvoicePdfAsync_ReturnsNull_WhenClaimNotAccepted()
        {
            var claim = new ContractClaim { Id = 12, ClaimStatus = ClaimStatus.PENDING_CONFIRM };

            _reviewerMock.Setup(r => r.GetClaimAsync(12)).ReturnsAsync(claim);

            var result = await _service.GenerateClaimInvoicePdfAsync(12);

            Assert.Null(result);
        }

        // -------------------------------------------------------
        // GENERATE PDF — SUCCESS
        // -------------------------------------------------------
        [Fact]
        public async Task GenerateClaimInvoicePdfAsync_ReturnsPdf_WhenAccepted()
        {
            var claim = new ContractClaim
            {
                Id = 55,
                ClaimStatus = ClaimStatus.ACCEPTED,
                LecturerUserId = 1,
                LecturerUser = new Models.Auth.AppUser { FirstName = "John", LastName = "Doe" },
                Module = new Module { Name = "Maths 101" },
                HoursWorked = 10,
                HourlyRate = 100,
            };

            var details = new LecturerDetails
            {
                UserId = 1,
                Address = "123 Road",
                ContactNumber = "111",
                BankDetails = "ABC Bank - 12345",
            };

            _context.LecturerDetails.Add(details);
            await _context.SaveChangesAsync();

            _reviewerMock.Setup(r => r.GetClaimAsync(55)).ReturnsAsync(claim);

            var result = await _service.GenerateClaimInvoicePdfAsync(55);

            Assert.NotNull(result);
            Assert.EndsWith(".pdf", result!.Value.FileName);
            Assert.NotNull(result.Value.FileStream);
            Assert.Equal("application/pdf", result.Value.ContentType);

            // Ensure PDF has content
            Assert.True(result.Value.FileStream.Length > 0);
        }
    }
}
