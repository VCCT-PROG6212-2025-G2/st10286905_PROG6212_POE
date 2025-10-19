using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContractMonthlyClaimSystem.Tests.Services
{
    /// <summary>
    /// Unit tests for the ReviewerClaimService class.
    /// Each test uses an in-memory EF Core database and mocks external dependencies.
    /// This suite verifies that the service correctly handles claim retrieval,
    /// claim reviews, and file decryption operations.
    /// </summary>
    public class ReviewerClaimServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<IFileEncryptionService> _encryptionMock;
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly ReviewerClaimService _service;

        public ReviewerClaimServiceTests()
        {
            // Create an in-memory EF Core database to isolate each test run.
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Mock the hosting environment to simulate the web root path for uploads.
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            // Mock the file encryption service to avoid actual encryption/decryption operations.
            _encryptionMock = new Mock<IFileEncryptionService>();

            // Mock UserManager<AppUser> to simulate role checks and identity lookups.
            var userStore = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStore.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );

            // Instantiate the service under test, injecting the mocks and context.
            _service = new ReviewerClaimService(
                _context,
                _envMock.Object,
                _userManagerMock.Object,
                _encryptionMock.Object
            );
        }

        [Fact]
        public async Task GetClaimsAsync_ReturnsAllClaimsWithIncludes()
        {
            // Arrange: add multiple claims with related modules and lecturer users.
            var module = new Module { Name = "Programming 2B", Code = "PROG6212" };
            var lecturer = new AppUser { Id = "L1", UserName = "lecturer@iie.ac.za" };
            _context.Modules.Add(module);
            _context.Users.Add(lecturer);
            _context.ContractClaims.Add(
                new ContractClaim { LecturerUserId = lecturer.Id, ModuleId = module.Id }
            );
            await _context.SaveChangesAsync();

            // Act: call the service method that retrieves all claims.
            var result = await _service.GetClaimsAsync();

            // Assert: verify that a single claim was retrieved with the correct references.
            Assert.Single(result);
            Assert.Equal(module.Id, result.First().ModuleId);
            Assert.Equal(lecturer.Id, result.First().LecturerUserId);
        }

        [Fact]
        public async Task GetClaimAsync_ReturnsSpecificClaim()
        {
            // Arrange: create and save a claim with related module and lecturer.
            var lecturer = new AppUser { UserName = "lecturer@cmcs.app" };
            _context.Users.Add(lecturer);
            var module = new Module { Name = "Programming 2B", Code = "PROG6212" };
            _context.Modules.Add(module);
            var claim = new ContractClaim { LecturerUserId = lecturer.Id, ModuleId = module.Id };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            // Act: retrieve that claim using its ID.
            var result = await _service.GetClaimAsync(claim.Id);

            // Assert: confirm that the correct claim is returned.
            Assert.NotNull(result);
            Assert.Equal(claim.Id, result!.Id);
        }

        [Fact]
        public async Task GetClaimFilesAsync_ReturnsLinkedFiles()
        {
            // Arrange: create a claim and an uploaded file linked via a join table.
            var claim = new ContractClaim { Id = 1, LecturerUserId = "L1" };
            var file = new UploadedFile
            {
                Id = 1,
                FileName = "proof.pdf",
                FilePath = "/somewhere/proof.pdf",
            };
            var link = new ContractClaimDocument
            {
                ContractClaimId = claim.Id,
                UploadedFileId = file.Id,
                UploadedFile = file,
            };
            _context.ContractClaims.Add(claim);
            _context.UploadedFiles.Add(file);
            _context.ContractClaimsDocuments.Add(link);
            await _context.SaveChangesAsync();

            // Act: call the service method to get files for this claim.
            var result = await _service.GetClaimFilesAsync(claim);

            // Assert: ensure the file list contains the expected document.
            Assert.Single(result!);
            Assert.Equal("proof.pdf", result![0].FileName);
        }

        [Fact]
        public async Task ReviewClaim_ReturnsFalse_WhenUserNotFound()
        {
            // Arrange: configure the mock UserManager to return null for any user lookup.
            _userManagerMock
                .Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((AppUser?)null);

            // Act: try to review a claim with a non-existent user.
            var result = await _service.ReviewClaim(1, "invalid", true, "ok");

            // Assert: should return false since the reviewer user cannot be found.
            Assert.False(result);
        }

        [Fact]
        public async Task ReviewClaim_ReturnsFalse_WhenUserHasNoValidRole()
        {
            // Arrange: mock a user with a non-reviewer role.
            var user = new AppUser { Id = "U1" };
            _userManagerMock.Setup(u => u.FindByIdAsync("U1")).ReturnsAsync(user);
            _userManagerMock
                .Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Lecturer" });

            // Add a valid claim to the database.
            var lecturer = new AppUser { UserName = "lecturer@cmcs.app" };
            _context.Users.Add(lecturer);
            var module = new Module { Name = "Programming 2B", Code = "PROG6212" };
            _context.Modules.Add(module);
            var claim = new ContractClaim { LecturerUserId = lecturer.Id, ModuleId = module.Id };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            // Act: attempt to review the claim with an invalid reviewer role.
            var result = await _service.ReviewClaim(claim.Id, "U1", true, "no");

            // Assert: should return false because role is not ProgramCoordinator/AcademicManager.
            Assert.False(result);
        }

        [Fact]
        public async Task ReviewClaim_UpdatesClaim_ForProgramCoordinatorAcceptance()
        {
            // Arrange: set up a ProgramCoordinator user and a claim.
            var user = new AppUser { Id = "U1" };
            _context.Users.Add(user);
            var lecturer = new AppUser { UserName = "lecturer@cmcs.app" };
            _context.Users.Add(lecturer);
            var module = new Module { Name = "Programming 2B", Code = "PROG6212" };
            _context.Modules.Add(module);
            var claim = new ContractClaim
            {
                Id = 1,
                LecturerUserId = lecturer.Id,
                ModuleId = module.Id,
            };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            // Mock the user's role and retrieval behavior.
            _userManagerMock.Setup(u => u.FindByIdAsync("U1")).ReturnsAsync(user);
            _userManagerMock
                .Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "ProgramCoordinator" });

            // Act: review the claim as accepted by the Program Coordinator.
            var result = await _service.ReviewClaim(1, "U1", true, "Looks good");

            // Assert: verify that the claim status was updated correctly.
            Assert.True(result);
            var updated = await _context.ContractClaims.FindAsync(1);
            Assert.Equal(ClaimDecision.VERIFIED, updated!.ProgramCoordinatorDecision);
            Assert.Equal("Looks good", updated.ProgramCoordinatorComment);
        }

        [Fact]
        public async Task ReviewClaim_UpdatesClaim_ForAcademicManagerRejection()
        {
            // Arrange: create an AcademicManager user and claim to review.
            var user = new AppUser { Id = "U2" };
            _context.Users.Add(user);
            var lecturer = new AppUser { UserName = "lecturer@cmcs.app" };
            _context.Users.Add(lecturer);
            var module = new Module { Name = "Programming 2B", Code = "PROG6212" };
            _context.Modules.Add(module);
            var claim = new ContractClaim
            {
                Id = 2,
                LecturerUserId = lecturer.Id,
                ModuleId = module.Id,
            };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            // Mock the user's role and lookup behavior.
            _userManagerMock.Setup(u => u.FindByIdAsync("U2")).ReturnsAsync(user);
            _userManagerMock
                .Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "AcademicManager" });

            // Act: review the claim as rejected by the Academic Manager.
            var result = await _service.ReviewClaim(2, "U2", false, "Incorrect hours");

            // Assert: confirm claim was updated and rejection recorded.
            Assert.True(result);
            var updated = await _context.ContractClaims.FindAsync(2);
            Assert.Equal(ClaimDecision.REJECTED, updated!.AcademicManagerDecision);
            Assert.Equal("Incorrect hours", updated.AcademicManagerComment);
        }

        [Fact]
        public async Task GetFileAsync_ReturnsDecryptedFile_WhenExists()
        {
            // Arrange: create a file record and corresponding encrypted test file on disk.
            var file = new UploadedFile
            {
                Id = 1,
                FileName = "data.txt",
                FilePath = Path.Combine("uploads", "data.txt"),
            };
            _context.UploadedFiles.Add(file);
            await _context.SaveChangesAsync();

            // Create the file physically under a temp directory.
            var fullPath = Path.Combine(_envMock.Object.WebRootPath, file.FilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, new byte[] { 1, 2, 3 });

            // Mock decryption to avoid any actual cryptography work.
            _encryptionMock
                .Setup(e => e.DecryptToStreamAsync(fullPath, It.IsAny<MemoryStream>()))
                .Returns(Task.CompletedTask);

            // Act: retrieve and decrypt the file using the service.
            var result = await _service.GetFileAsync(1);

            // Cleanup: remove the uploads directory created for this test.
            var uploadRoot = Path.Combine(_envMock.Object.WebRootPath, "uploads");
            if (Directory.Exists(uploadRoot))
                Directory.Delete(uploadRoot, true);

            // Assert: verify that file metadata is correctly returned.
            Assert.NotNull(result);
            Assert.Equal("data.txt", result?.FileName);
            Assert.Equal("application/octet-stream", result?.ContentType);
        }

        [Fact]
        public async Task GetFileAsync_ReturnsNull_WhenFileNotFound()
        {
            // Arrange: do not add any file to the database.

            // Act: attempt to retrieve a file that doesn't exist.
            var result = await _service.GetFileAsync(999);

            // Assert: service should return null since file ID was invalid.
            Assert.Null(result);
        }
    }
}
