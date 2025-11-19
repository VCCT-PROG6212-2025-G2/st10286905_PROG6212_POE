// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68f5452c-2788-800b-bbbc-175029690cfd

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
    public class LecturerClaimServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<IModuleService> _moduleServiceMock;
        private readonly Mock<IFileEncryptionService> _encryptionMock;
        private readonly LecturerClaimService _service;
        private readonly string _tempRoot;

        public LecturerClaimServiceTests()
        {
            // In-memory DB
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            // Mock web root
            _tempRoot = Path.Combine(Path.GetTempPath(), $"testroot_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempRoot);
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock
                .Setup(e => e.WebRootPath)
                .Returns(_tempRoot);

            _moduleServiceMock = new Mock<IModuleService>();
            _encryptionMock = new Mock<IFileEncryptionService>();

            _service = new LecturerClaimService(
                _context,
                _envMock.Object,
                _moduleServiceMock.Object,
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

        // ---------------------------------------------------------
        // GET CLAIMS FOR LECTURER
        // ---------------------------------------------------------
        [Fact]
        public async Task GetClaimsForLecturerAsync_ReturnsClaims()
        {
            var lecturerId = 1;
            var module = new Module { Name = "Programming", Code = "PROG6212" };
            _context.Modules.Add(module);

            _context.ContractClaims.AddRange(
                new ContractClaim { LecturerUserId = lecturerId, ModuleId = module.Id },
                new ContractClaim { LecturerUserId = 2, ModuleId = module.Id }
            );
            await _context.SaveChangesAsync();

            var result = await _service.GetClaimsForLecturerAsync(lecturerId);

            Assert.Single(result);
            Assert.Equal(lecturerId, result.First().LecturerUserId);
        }

        // ---------------------------------------------------------
        // GET CLAIM BY ID
        // ---------------------------------------------------------
        [Fact]
        public async Task GetClaimAsync_ReturnsSpecificClaim()
        {
            var lecturerId = 1;
            var module = new Module { Name = "Programming", Code = "PROG6212" };
            _context.Modules.Add(module);

            var claim = new ContractClaim { LecturerUserId = lecturerId, ModuleId = module.Id };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            var result = await _service.GetClaimAsync(claim.Id, lecturerId);

            Assert.NotNull(result);
            Assert.Equal(claim.Id, result!.Id);
        }

        [Fact]
        public async Task GetClaimAsync_ReturnsNull_IfWrongLecturer()
        {
            var module = new Module { Name = "Programming", Code = "PROG6212" };
            _context.Modules.Add(module);

            var claim = new ContractClaim { LecturerUserId = 1, ModuleId = module.Id };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            var result = await _service.GetClaimAsync(claim.Id, 999);

            Assert.Null(result);
        }

        // ---------------------------------------------------------
        // GET CLAIM FILES
        // ---------------------------------------------------------
        [Fact]
        public async Task GetClaimFilesAsync_ReturnsFilesLinkedToClaim()
        {
            var claim = new ContractClaim { LecturerUserId = 1 };
            _context.ContractClaims.Add(claim);

            var file = new UploadedFile { FileName = "doc.pdf", FilePath = "uploads/doc.pdf" };
            _context.UploadedFiles.Add(file);

            _context.ContractClaimsDocuments.Add(
                new ContractClaimDocument
                {
                    ContractClaimId = claim.Id,
                    UploadedFileId = file.Id,
                    UploadedFile = file,
                }
            );

            await _context.SaveChangesAsync();

            var result = await _service.GetClaimFilesAsync(claim);

            Assert.Single(result!);
            Assert.Equal("doc.pdf", result![0].FileName);
        }

        // ---------------------------------------------------------
        // GET MODULES FOR LECTURER
        // ---------------------------------------------------------
        [Fact]
        public async Task GetModulesForLecturerAsync_DelegatesToModuleService()
        {
            var lecturerId = 1;
            var expected = new List<Module>
            {
                new() { Id = 1, Name = "Programming", Code = "PROG6212" }
            };

            _moduleServiceMock
                .Setup(m => m.GetModulesForLecturerAsync(lecturerId))
                .ReturnsAsync(expected);

            var result = await _service.GetModulesForLecturerAsync(lecturerId);

            Assert.Single(result);
            Assert.Equal("Programming", result[0].Name);
            _moduleServiceMock.Verify(m => m.GetModulesForLecturerAsync(lecturerId), Times.Once);
        }

        // ---------------------------------------------------------
        // NEW: GET HOURLY RATE
        // ---------------------------------------------------------
        [Fact]
        public async Task GetLecturerHourlyRateAsync_ReturnsRate()
        {
            var lecturerId = 1;
            var moduleId = 10;

            _context.LecturerModules.Add(
                new LecturerModule
                {
                    LecturerUserId = lecturerId,
                    ModuleId = moduleId,
                    HourlyRate = 550m
                }
            );
            await _context.SaveChangesAsync();

            var result = await _service.GetLecturerHourlyRateAsync(lecturerId, moduleId);

            Assert.Equal(550m, result);
        }

        [Fact]
        public async Task GetLecturerHourlyRateAsync_ReturnsNull_IfNotFound()
        {
            var result = await _service.GetLecturerHourlyRateAsync(1, 999);

            Assert.Null(result);
        }

        // ---------------------------------------------------------
        // CREATE CLAIM
        // ---------------------------------------------------------
        [Fact]
        public async Task CreateClaimAsync_AddsClaimToDatabase_AndPullsHourlyRate()
        {
            _context.LecturerModules.Add(
                new LecturerModule { LecturerUserId = 1, ModuleId = 1, HourlyRate = 300 }
            );
            await _context.SaveChangesAsync();

            var model = new CreateClaimViewModel
            {
                ModuleId = 1,
                HoursWorked = 10,
                LecturerComment = "Work done"
            };

            var claim = await _service.CreateClaimAsync(1, model);

            Assert.Equal(300m, claim.HourlyRate);
            Assert.Equal(1, _context.ContractClaims.Count());
        }

        [Fact]
        public async Task CreateClaimAsync_SetsRateZero_IfModuleNotLinked()
        {
            var model = new CreateClaimViewModel { ModuleId = 5, HoursWorked = 8 };

            var claim = await _service.CreateClaimAsync(1, model);

            Assert.Equal(0m, claim.HourlyRate);
        }

        // ---------------------------------------------------------
        // ADD FILES
        // ---------------------------------------------------------
        [Fact]
        public async Task AddFilesToClaimAsync_SavesFiles_AndDocuments()
        {
            var claim = new ContractClaim { Id = 10, LecturerUserId = 1 };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            var bytes = new byte[] { 1, 2, 3 };
            var ms = new MemoryStream(bytes);

            var formFile = new FormFile(ms, 0, bytes.Length, "file", "test.txt");
            var files = new List<IFormFile> { formFile };

            _encryptionMock
                .Setup(e => e.EncryptToFileAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _service.AddFilesToClaimAsync(claim, files);

            Assert.True(_context.UploadedFiles.Any());
            Assert.True(_context.ContractClaimsDocuments.Any());
        }

        // ---------------------------------------------------------
        // GET FILE (MAIN)
        // ---------------------------------------------------------
        [Fact]
        public async Task GetFileAsync_ReturnsDecryptedStream_WhenFileExists()
        {
            var lecturerId = 1;

            var module = new Module { Name = "Networking", Code = "NET123" };
            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            var claim = new ContractClaim { LecturerUserId = lecturerId, ModuleId = module.Id };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            var file = new UploadedFile
            {
                FileName = "file.txt",
                FilePath = "uploads/file.txt",
            };
            _context.UploadedFiles.Add(file);
            await _context.SaveChangesAsync();

            _context.ContractClaimsDocuments.Add(
                new ContractClaimDocument
                {
                    ContractClaimId = claim.Id,
                    UploadedFileId = file.Id,
                }
            );
            await _context.SaveChangesAsync();

            // Write mock encrypted file to disk
            var fullPath = Path.Combine(_envMock.Object.WebRootPath, file.FilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, new byte[] { 9, 9, 9 });

            _encryptionMock
                .Setup(e => e.DecryptToStreamAsync(fullPath, It.IsAny<MemoryStream>()))
                .Returns(Task.CompletedTask);

            var result = await _service.GetFileAsync(file.Id, lecturerId);

            Assert.NotNull(result);
            Assert.Equal("file.txt", result?.FileName);

            result?.FileStream.Dispose();
        }

        // ---------------------------------------------------------
        // GET FILE (EDGE CASES)
        // ---------------------------------------------------------
        [Fact]
        public async Task GetFileAsync_ReturnsNull_WhenFileNotLinked()
        {
            var result = await _service.GetFileAsync(999, 1);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetFileAsync_ReturnsNull_WhenLecturerDoesNotOwnClaim()
        {
            var claim = new ContractClaim { LecturerUserId = 1 };
            var file = new UploadedFile { FileName = "x", FilePath = "uploads/x" };

            await _context.ContractClaims.AddAsync(claim);
            await _context.UploadedFiles.AddAsync(file);
            await _context.SaveChangesAsync();

            _context.ContractClaimsDocuments.Add(
                new ContractClaimDocument { ContractClaimId = claim.Id, UploadedFileId = file.Id }
            );
            await _context.SaveChangesAsync();

            var result = await _service.GetFileAsync(file.Id, lecturerId: 999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetFileAsync_ReturnsNull_WhenPhysicalFileMissing()
        {
            var lecturerId = 1;

            var claim = new ContractClaim { LecturerUserId = lecturerId };
            var file = new UploadedFile { FileName = "x", FilePath = "uploads/x" };

            _context.ContractClaims.Add(claim);
            _context.UploadedFiles.Add(file);
            _context.ContractClaimsDocuments.Add(
                new ContractClaimDocument
                {
                    ContractClaimId = claim.Id,
                    UploadedFileId = file.Id
                }
            );

            await _context.SaveChangesAsync();

            var result = await _service.GetFileAsync(file.Id, lecturerId);

            Assert.Null(result);
        }
    }
}
