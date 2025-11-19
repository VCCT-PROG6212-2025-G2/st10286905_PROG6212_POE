// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68f5452c-2788-800b-bbbc-175029690cfd

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.Auth;
using ContractMonthlyClaimSystem.Services;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
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
    public class ReviewerClaimServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<IFileEncryptionService> _encryptionMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly ReviewerClaimService _service;
        private readonly string _tempRoot;

        public ReviewerClaimServiceTests()
        {
            // Create an in-memory EF Core database to isolate each test run.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // Mock the hosting environment to simulate the web root path for uploads.
            _tempRoot = Path.Combine(Path.GetTempPath(), $"testroot_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempRoot);
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock
                .Setup(e => e.WebRootPath)
                .Returns(_tempRoot);

            // Mock the file encryption service to avoid actual encryption/decryption operations.
            _encryptionMock = new Mock<IFileEncryptionService>();

            // Mock IUserService
            _userServiceMock = new Mock<IUserService>();

            // Instantiate the service under test, injecting the mocks and context.
            _service = new ReviewerClaimService(
                _context,
                _envMock.Object,
                _userServiceMock.Object,
                _encryptionMock.Object
            );
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, true);
            }
        }

        [Fact]
        public async Task GetClaimsAsync_ReturnsAllClaimsWithIncludes()
        {
            // Arrange: add multiple claims with related modules and lecturer users.
            var module = new Module { Name = "Programming 2B", Code = "PROG6212" };
            var lecturer = new AppUser { Id = 1, UserName = "lecturer@iie.ac.za" };
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
            var lecturer = new AppUser { UserName = "lecturer" };
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
            var claim = new ContractClaim { Id = 1, LecturerUserId = 1 };
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
            _userServiceMock
                .Setup(u => u.GetUserAsync(It.IsAny<string>()))
                .ReturnsAsync((AppUser?)null);

            // Act: try to review a claim with a non-existent user.
            var result = await _service.ReviewClaim(1, -1, true, "ok");

            // Assert: should return false since the reviewer user cannot be found.
            Assert.False(result);
        }

        [Fact]
        public async Task ReviewClaim_ReturnsFalse_WhenUserHasNoValidRole()
        {
            // Arrange: mock a user with a non-reviewer role.
            var user = new AppUser
            {
                Id = 1,
                UserRoles =
                [
                    new AppUserRole
                    {
                        UserId = 1,
                        Role = new AppRole { Name = "Lecturer" },
                    },
                ],
            };
            _userServiceMock.Setup(u => u.GetUserAsync(1)).ReturnsAsync(user);

            // Add a valid claim to the database.
            var lecturer = new AppUser { UserName = "lecturer@cmcs.app" };
            _context.Users.Add(lecturer);
            var module = new Module { Name = "Programming 2B", Code = "PROG6212" };
            _context.Modules.Add(module);
            var claim = new ContractClaim { LecturerUserId = lecturer.Id, ModuleId = module.Id };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            // Act: attempt to review the claim with an invalid reviewer role.
            var result = await _service.ReviewClaim(claim.Id, 1, true, "no");

            // Assert: should return false because role is not ProgramCoordinator/AcademicManager.
            Assert.False(result);
        }

        [Fact]
        public async Task ReviewClaim_UpdatesClaim_ForProgramCoordinatorAcceptance()
        {
            // Arrange: set up a ProgramCoordinator user and a claim.
            var user = new AppUser
            {
                Id = 1,
                UserRoles =
                [
                    new AppUserRole
                    {
                        UserId = 1,
                        Role = new AppRole { Name = "ProgramCoordinator" },
                    },
                ],
            };
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
            _userServiceMock.Setup(u => u.GetUserAsync(1)).ReturnsAsync(user);

            // Act: review the claim as accepted by the Program Coordinator.
            var result = await _service.ReviewClaim(1, 1, true, "Looks good");

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
            var user = new AppUser
            {
                Id = 2,
                UserRoles =
                [
                    new AppUserRole
                    {
                        UserId = 1,
                        Role = new AppRole { Name = "AcademicManager" },
                    },
                ],
            };
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
            _userServiceMock.Setup(u => u.GetUserAsync(2)).ReturnsAsync(user);

            // Act: review the claim as rejected by the Academic Manager.
            var result = await _service.ReviewClaim(2, 2, false, "Incorrect hours");

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
                FileName = "reviewer_test_data.txt",
                FilePath = Path.Combine("uploads", "reviewer_test_data.txt"),
            };
            _context.UploadedFiles.Add(file);
            await _context.SaveChangesAsync();

            // Create the physical file
            var fullPath = Path.Combine(_envMock.Object.WebRootPath, file.FilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, new byte[] { 1, 2, 3 });

            // Mock decryption
            _encryptionMock
                .Setup(e => e.DecryptToStreamAsync(fullPath, It.IsAny<MemoryStream>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetFileAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("reviewer_test_data.txt", result?.FileName);
            Assert.Equal("application/octet-stream", result?.ContentType);

            // IMPORTANT: dispose MemoryStream so file unlocks
            result?.FileStream?.Dispose();
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
