using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContractMonthlyClaimSystem.Tests.Services
{
    /// <summary>
    /// Unit tests for the LecturerClaimService class.
    /// This suite verifies all major public methods of the service.
    /// Each test uses an in-memory database and mocks external dependencies.
    /// </summary>
    public class LecturerClaimServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<IModuleService> _moduleServiceMock;
        private readonly Mock<IFileEncryptionService> _encryptionMock;
        private readonly LecturerClaimService _service;

        public LecturerClaimServiceTests()
        {
            // Create an in-memory EF Core database context for isolated testing.
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Mock IWebHostEnvironment to simulate the web root directory.
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            // Mock the dependent services (IModuleService, IFileEncryptionService)
            // so that no external systems or file IO are used.
            _moduleServiceMock = new Mock<IModuleService>();
            _encryptionMock = new Mock<IFileEncryptionService>();

            // Instantiate the service under test, injecting the mocks and context.
            _service = new LecturerClaimService(
                _context,
                _envMock.Object,
                _moduleServiceMock.Object,
                _encryptionMock.Object
            );
        }

        [Fact]
        public async Task GetClaimsForLecturerAsync_ReturnsClaims()
        {
            // Arrange: add sample claims for two lecturers.
            var lecturerId = "L1";
            var module = new Module { Name = "Programming 2B", Code = "PROG6212" };
            _context.Modules.Add(module);
            _context.ContractClaims.AddRange(
                new ContractClaim { LecturerUserId = lecturerId, ModuleId = module.Id },
                new ContractClaim { LecturerUserId = "Other", ModuleId = module.Id }
            );
            await _context.SaveChangesAsync();

            // Act: retrieve claims for the target lecturer.
            var result = await _service.GetClaimsForLecturerAsync(lecturerId);

            // Assert: only the lecturer's claims should be returned.
            Assert.Single(result);
            Assert.Equal(lecturerId, result.First().LecturerUserId);
        }

        [Fact]
        public async Task GetClaimAsync_ReturnsSpecificClaim()
        {
            // Arrange: add a claim with a module for a lecturer.
            var lecturerId = "L1";
            var module = new Module { Name = "Programming 2B", Code = "PROG6212" };
            _context.Modules.Add(module);
            var claim = new ContractClaim { LecturerUserId = lecturerId, ModuleId = module.Id };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            // Act: request the claim by its ID and lecturer ID.
            var result = await _service.GetClaimAsync(claim.Id, lecturerId);

            // Assert: the correct claim should be retrieved.
            Assert.NotNull(result);
            Assert.Equal(claim.Id, result!.Id);
        }

        [Fact]
        public async Task GetClaimFilesAsync_ReturnsFilesLinkedToClaim()
        {
            // Arrange: add a claim and an uploaded file linked through a document record.
            var claim = new ContractClaim { LecturerUserId = "L1" };
            await _context.ContractClaims.AddAsync(claim);
            var file = new UploadedFile { FileName = "doc.pdf", FilePath = "/somewhere/doc.pdf" };
            await _context.UploadedFiles.AddAsync(file);

            _context.ContractClaimsDocuments.Add(
                new ContractClaimDocument
                {
                    ContractClaimId = claim.Id,
                    UploadedFileId = file.Id,
                    UploadedFile = file,
                }
            );
            await _context.SaveChangesAsync();

            // Act: retrieve files for this claim.
            var result = await _service.GetClaimFilesAsync(claim);

            // Assert: should return one file with the correct name.
            Assert.Single(result!);
            Assert.Equal("doc.pdf", result![0].FileName);
        }

        [Fact]
        public async Task GetModulesForLecturerAsync_DelegatesToModuleService()
        {
            // Arrange: mock the module service to return a specific result.
            var lecturerId = "L1";
            var expectedModules = new List<Module>
            {
                new()
                {
                    Id = 1,
                    Name = "Programming 2B",
                    Code = "PROG6212",
                },
            };
            _moduleServiceMock
                .Setup(m => m.GetModulesForLecturerAsync(lecturerId))
                .ReturnsAsync(expectedModules);

            // Act: call the service method.
            var result = await _service.GetModulesForLecturerAsync(lecturerId);

            // Assert: ensure it calls the mock and returns the expected list.
            Assert.Single(result);
            Assert.Equal("Programming 2B", result[0].Name);
            Assert.Equal("PROG6212", result[0].Code);
            _moduleServiceMock.Verify(m => m.GetModulesForLecturerAsync(lecturerId), Times.Once);
        }

        [Fact]
        public async Task CreateClaimAsync_AddsClaimToDatabase()
        {
            // Arrange: create a sample claim creation model.
            var model = new CreateClaimViewModel
            {
                ModuleId = 1,
                HoursWorked = 10,
                HourlyRate = 200,
                LecturerComment = "Work done",
            };
            var lecturerId = "L1";

            // Act: call the service to create a new claim.
            var claim = await _service.CreateClaimAsync(lecturerId, model);

            // Assert: a claim should be saved and contain the expected data.
            Assert.NotNull(claim);
            Assert.Equal(1, _context.ContractClaims.Count());
            Assert.Equal("Work done", claim.LecturerComment);
        }

        [Fact]
        public async Task AddFilesToClaimAsync_SavesFilesAndDocuments()
        {
            // Arrange: create a claim and add it to the database.
            var claim = new ContractClaim { Id = 10, LecturerUserId = "L1" };
            await _context.ContractClaims.AddAsync(claim);
            await _context.SaveChangesAsync();

            // Create a fake IFormFile using a memory stream.
            var fileBytes = new byte[] { 1, 2, 3 };
            var stream = new MemoryStream(fileBytes);
            var formFile = new FormFile(stream, 0, fileBytes.Length, "file", "test.txt");
            var files = new List<IFormFile> { formFile };

            // Mock encryption so that it does nothing (avoid real IO).
            _encryptionMock
                .Setup(e => e.EncryptToFileAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act: add files to the claim.
            await _service.AddFilesToClaimAsync(claim, files);

            // Assert: uploaded files and linking documents should be saved.
            Assert.True(_context.UploadedFiles.Any());
            Assert.True(_context.ContractClaimsDocuments.Any());
        }

        [Fact]
        public async Task GetFileAsync_ReturnsDecryptedStream_WhenFileExists()
        {
            // Arrange: set up a claim, uploaded file, and linking record.
            var lecturerId = "L1";
            var file = new UploadedFile
            {
                Id = 1,
                FileName = "data.txt",
                FilePath = Path.Combine("uploads", "data.txt"),
            };
            var claim = new ContractClaim { Id = 1, LecturerUserId = lecturerId };

            _context.ContractClaims.Add(claim);
            _context.UploadedFiles.Add(file);
            _context.ContractClaimsDocuments.Add(
                new ContractClaimDocument { ContractClaimId = claim.Id, UploadedFileId = file.Id }
            );
            await _context.SaveChangesAsync();

            // Create a physical test file to simulate an existing encrypted file.
            var fullPath = Path.Combine(_envMock.Object.WebRootPath, file.FilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, new byte[] { 1, 2, 3 });

            // Mock decryption service to do nothing but satisfy the call.
            _encryptionMock
                .Setup(e => e.DecryptToStreamAsync(fullPath, It.IsAny<MemoryStream>()))
                .Returns(Task.CompletedTask);

            // Act: attempt to retrieve and decrypt the file.
            var result = await _service.GetFileAsync(1, lecturerId);

            // Assert: ensure a result was returned with expected metadata.
            Assert.NotNull(result);
            Assert.Equal("data.txt", result?.FileName);
            Assert.Equal("application/octet-stream", result?.ContentType);
        }
    }
}
