// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/691f16e8-5034-800b-898a-2c7eb4000f43

using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.Auth;
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
        private readonly Mock<IFileService> _fileMock;
        private readonly HumanResourcesService _service;

        public HumanResourcesServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _reviewerMock = new Mock<IReviewerClaimService>(MockBehavior.Strict);
            _fileMock = new Mock<IFileService>(MockBehavior.Strict);

            _service = new HumanResourcesService(_context, _reviewerMock.Object, _fileMock.Object);
        }

        // =======================================================
        // GET LECTURER DETAILS
        // =======================================================
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

        // =======================================================
        // SET LECTURER DETAILS
        // =======================================================
        [Fact]
        public async Task SetLecturerDetailsAsync_AddsWhenNotExisting()
        {
            var d = new LecturerDetails { UserId = 10, ContactNumber = "555" };

            await _service.SetLecturerDetailsAsync(d);

            Assert.Single(_context.LecturerDetails);
            Assert.Equal("555", _context.LecturerDetails.First().ContactNumber);
        }

        [Fact]
        public async Task SetLecturerDetailsAsync_UpdatesWhenExisting()
        {
            var existing = new LecturerDetails { UserId = 5, ContactNumber = "111" };
            _context.LecturerDetails.Add(existing);
            await _context.SaveChangesAsync();

            var incoming = new LecturerDetails { UserId = 5, ContactNumber = "999" };
            await _service.SetLecturerDetailsAsync(incoming);

            Assert.Single(_context.LecturerDetails);
            Assert.Equal("999", _context.LecturerDetails.First().ContactNumber);
        }

        // =======================================================
        // AUTO REVIEW CLAIMS
        // =======================================================
        [Fact]
        public async Task AutoReviewClaimsForReviewersAsync_CallsReviewerService()
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

        // =======================================================
        // GET APPROVED CLAIMS
        // =======================================================
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

        // =======================================================
        // GET PROCESSED CLAIM INVOICES
        // =======================================================
        [Fact]
        public async Task GetProcessedClaimInvoices_ReturnsInvoicesWithIncludes()
        {
            var claim = new ContractClaim { Id = 10, ClaimStatus = ClaimStatus.ACCEPTED };

            var file = new UploadedFile
            {
                Id = 5,
                FileName = "inv.pdf",
                FilePath = "uploads/inv.pdf",
                FileSize = 123,
            };

            var doc = new ClaimInvoiceDocument
            {
                ClaimId = claim.Id,
                UploadedFileId = file.Id,
                Claim = claim,
                UploadedFile = file,
            };

            _context.ClaimInvoiceDocuments.Add(doc);
            await _context.SaveChangesAsync();

            var result = await _service.GetProcessedClaimInvoices();

            Assert.Single(result);
            Assert.Equal("inv.pdf", result.First().UploadedFile.FileName);
        }

        // =======================================================
        // PROCESS APPROVED CLAIM INVOICES
        // =======================================================
        [Fact]
        public async Task ProcessApprovedClaimInvoices_ReturnsZero_WhenNoneToProcess()
        {
            var claim = new ContractClaim { Id = 1, ClaimStatus = ClaimStatus.ACCEPTED };

            _context.ContractClaims.Add(claim);
            _context.ClaimInvoiceDocuments.Add(
                new ClaimInvoiceDocument
                {
                    ClaimId = claim.Id,
                    Claim = claim,
                    UploadedFileId = 99,
                }
            );
            await _context.SaveChangesAsync();

            var result = await _service.ProcessApprovedClaimInvoices();

            Assert.Equal(0, result);
        }

        [Fact]
        public async Task ProcessApprovedClaimInvoices_GeneratesInvoices()
        {
            _context.ContractClaims.Add(
                new ContractClaim
                {
                    Id = 1,
                    ClaimStatus = ClaimStatus.ACCEPTED,
                    LecturerUserId = 1,
                    LecturerUser = new AppUser(),
                    Module = new Module { Name = "M1", Code = "M101" },
                    HoursWorked = 2,
                    HourlyRate = 100,
                }
            );

            _context.ContractClaims.Add(
                new ContractClaim
                {
                    Id = 2,
                    ClaimStatus = ClaimStatus.ACCEPTED,
                    LecturerUserId = 1,
                    LecturerUser = new AppUser(),
                    Module = new Module { Name = "M2", Code = "M202" },
                    HoursWorked = 3,
                    HourlyRate = 200,
                }
            );

            await _context.SaveChangesAsync();

            _reviewerMock
                .Setup(r => r.GetClaimAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _context.ContractClaims.Find(id)!);

            var fakePdf = (
                FileStream: (Stream)new MemoryStream(new byte[] { 1, 2, 3 }),
                ContentType: "application/pdf",
                FileName: "gen.pdf"
            );

            _fileMock
                .Setup(f =>
                    f.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<long>())
                )
                .ReturnsAsync(
                    new UploadedFile
                    {
                        Id = 99,
                        FileName = "gen.pdf",
                        FilePath = "uploads/gen.pdf",
                        FileSize = 3,
                    }
                );

            _fileMock.Setup(f => f.GetFileAsync(99)).ReturnsAsync(fakePdf);

            var result = await _service.ProcessApprovedClaimInvoices();

            Assert.Equal(2, result);
            Assert.Equal(2, _context.ClaimInvoiceDocuments.Count());
        }

        // =======================================================
        // GET CLAIM INVOICE PDF — NULLS
        // =======================================================
        [Fact]
        public async Task GetClaimInvoicePdfAsync_ReturnsNull_WhenClaimNotFound()
        {
            _reviewerMock.Setup(r => r.GetClaimAsync(999)).ReturnsAsync((ContractClaim?)null);

            var result = await _service.GetClaimInvoicePdfAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetClaimInvoicePdfAsync_ReturnsExisting_WhenInvoiceAlreadyCreated()
        {
            var claim = new ContractClaim { Id = 10 };
            var file = new UploadedFile
            {
                Id = 777,
                FileName = "exist.pdf",
                FilePath = "uploads/exist.pdf",
                FileSize = 5,
            };

            _reviewerMock.Setup(r => r.GetClaimAsync(10)).ReturnsAsync(claim);

            _context.ClaimInvoiceDocuments.Add(
                new ClaimInvoiceDocument
                {
                    ClaimId = 10,
                    UploadedFileId = 777,
                    Claim = claim,
                    UploadedFile = file,
                }
            );
            await _context.SaveChangesAsync();

            var fakePdf = (
                FileStream: (Stream)new MemoryStream(new byte[] { 1 }),
                ContentType: "application/pdf",
                FileName: "exist.pdf"
            );

            _fileMock.Setup(f => f.GetFileAsync(777)).ReturnsAsync(fakePdf);

            var result = await _service.GetClaimInvoicePdfAsync(10);

            Assert.NotNull(result);
            Assert.Equal("exist.pdf", result!.Value.FileName);
        }

        // =======================================================
        // GET CLAIM INVOICE PDF — GENERATE IF NOT EXIST
        // =======================================================
        [Fact]
        public async Task GetClaimInvoicePdfAsync_GeneratesAndSaves_WhenNotExisting()
        {
            var claim = new ContractClaim
            {
                Id = 10,
                ClaimStatus = ClaimStatus.ACCEPTED,
                LecturerUserId = 1,
                LecturerUser = new AppUser { FirstName = "A", LastName = "B" },
                Module = new Module { Name = "X", Code = "X101" },
                HoursWorked = 5,
                HourlyRate = 100,
            };

            _context.ContractClaims.Add(claim);
            _context.LecturerDetails.Add(new LecturerDetails { UserId = 1, ContactNumber = "123" });

            await _context.SaveChangesAsync();

            _reviewerMock.Setup(r => r.GetClaimAsync(10)).ReturnsAsync(claim);

            var uploaded = new UploadedFile
            {
                Id = 40,
                FileName = "inv10.pdf",
                FilePath = "uploads/inv10.pdf",
                FileSize = 3,
            };

            _fileMock
                .Setup(f =>
                    f.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<long>())
                )
                .ReturnsAsync(uploaded);

            var returnedPdf = (
                FileStream: (Stream)new MemoryStream(new byte[] { 1, 2, 3 }),
                ContentType: "application/pdf",
                FileName: "inv10.pdf"
            );

            _fileMock.Setup(f => f.GetFileAsync(40)).ReturnsAsync(returnedPdf);

            var result = await _service.GetClaimInvoicePdfAsync(10);

            Assert.NotNull(result);
            Assert.Equal("inv10.pdf", result!.Value.FileName);
            Assert.Equal(1, _context.ClaimInvoiceDocuments.Count());
        }

        // =======================================================
        // GENERATE PDF — NULL CASES
        // =======================================================
        [Fact]
        public async Task GenerateClaimInvoicePdfAsync_ReturnsNull_WhenClaimNotAccepted()
        {
            _reviewerMock
                .Setup(r => r.GetClaimAsync(7))
                .ReturnsAsync(
                    new ContractClaim { Id = 7, ClaimStatus = ClaimStatus.PENDING_CONFIRM }
                );

            var result = await _service.GenerateClaimInvoicePdfAsync(7);
            Assert.Null(result);
        }

        // =======================================================
        // GENERATE PDF — SUCCESS
        // =======================================================
        [Fact]
        public async Task GenerateClaimInvoicePdfAsync_ReturnsPdf_WhenAccepted()
        {
            var claim = new ContractClaim
            {
                Id = 55,
                ClaimStatus = ClaimStatus.ACCEPTED,
                LecturerUserId = 1,
                LecturerUser = new AppUser { FirstName = "Jane", LastName = "Doe" },
                Module = new Module { Name = "Networking", Code = "NET101" },
                HoursWorked = 10,
                HourlyRate = 200,
            };

            var details = new LecturerDetails
            {
                UserId = 1,
                Address = "123 Road",
                ContactNumber = "555",
                BankDetails = "ABSA-12345",
            };

            _context.LecturerDetails.Add(details);
            await _context.SaveChangesAsync();

            _reviewerMock.Setup(r => r.GetClaimAsync(55)).ReturnsAsync(claim);

            var result = await _service.GenerateClaimInvoicePdfAsync(55);

            Assert.NotNull(result);
            Assert.Equal("application/pdf", result!.Value.ContentType);
            Assert.EndsWith(".pdf", result.Value.FileName);
            Assert.True(result.Value.FileStream.Length > 0);
        }
    }
}
