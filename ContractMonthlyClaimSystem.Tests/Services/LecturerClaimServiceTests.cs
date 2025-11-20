// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/691f16e8-5034-800b-898a-2c7eb4000f43

using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContractMonthlyClaimSystem.Tests.Services
{
    public class LecturerClaimServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IModuleService> _moduleServiceMock;
        private readonly Mock<IFileService> _fileServiceMock;
        private readonly LecturerClaimService _service;

        public LecturerClaimServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _moduleServiceMock = new Mock<IModuleService>();
            _fileServiceMock = new Mock<IFileService>();

            _service = new LecturerClaimService(
                _context,
                _moduleServiceMock.Object,
                _fileServiceMock.Object
            );
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
        public async Task GetClaimAsync_ReturnsClaim_WhenOwned()
        {
            var module = new Module { Name = "Networking", Code = "NET123" };
            _context.Modules.Add(module);

            var claim = new ContractClaim { LecturerUserId = 5, ModuleId = module.Id };
            _context.ContractClaims.Add(claim);

            await _context.SaveChangesAsync();

            var result = await _service.GetClaimAsync(claim.Id, 5);

            Assert.NotNull(result);
            Assert.Equal(claim.Id, result!.Id);
        }

        [Fact]
        public async Task GetClaimAsync_ReturnsNull_WhenNotOwned()
        {
            var module = new Module { Name = "Networking", Code = "NET123" };
            _context.Modules.Add(module);

            var claim = new ContractClaim { LecturerUserId = 5, ModuleId = module.Id };
            _context.ContractClaims.Add(claim);

            await _context.SaveChangesAsync();

            var result = await _service.GetClaimAsync(claim.Id, 999);

            Assert.Null(result);
        }

        // ---------------------------------------------------------
        // GET CLAIM FILES
        // ---------------------------------------------------------
        [Fact]
        public async Task GetClaimFilesAsync_ReturnsFiles()
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

            var files = await _service.GetClaimFilesAsync(claim);

            Assert.Single(files!);
            Assert.Equal("doc.pdf", files![0].FileName);
        }

        // ---------------------------------------------------------
        // GET MODULES
        // ---------------------------------------------------------
        [Fact]
        public async Task GetModulesForLecturerAsync_Delegates()
        {
            var lecturerId = 8;

            var expected = new List<Module>
            {
                new()
                {
                    Id = 1,
                    Name = "Maths",
                    Code = "MTH101",
                },
            };

            _moduleServiceMock
                .Setup(m => m.GetModulesForLecturerAsync(lecturerId))
                .ReturnsAsync(expected);

            var result = await _service.GetModulesForLecturerAsync(lecturerId);

            Assert.Single(result);
            Assert.Equal("Maths", result[0].Name);

            _moduleServiceMock.Verify(m => m.GetModulesForLecturerAsync(lecturerId), Times.Once);
        }

        // ---------------------------------------------------------
        // HOURLY RATE
        // ---------------------------------------------------------
        [Fact]
        public async Task GetLecturerHourlyRateAsync_ReturnsRate()
        {
            _context.LecturerModules.Add(
                new LecturerModule
                {
                    LecturerUserId = 1,
                    ModuleId = 10,
                    HourlyRate = 550m,
                }
            );

            await _context.SaveChangesAsync();

            var rate = await _service.GetLecturerHourlyRateAsync(1, 10);

            Assert.Equal(550m, rate);
        }

        [Fact]
        public async Task GetLecturerHourlyRateAsync_ReturnsNull_WhenMissing()
        {
            var rate = await _service.GetLecturerHourlyRateAsync(1, 999);
            Assert.Null(rate);
        }

        // ---------------------------------------------------------
        // CREATE CLAIM
        // ---------------------------------------------------------
        [Fact]
        public async Task CreateClaimAsync_SetsFields_AndSaves()
        {
            _context.LecturerModules.Add(
                new LecturerModule
                {
                    LecturerUserId = 2,
                    ModuleId = 20,
                    HourlyRate = 250,
                }
            );

            await _context.SaveChangesAsync();

            var model = new CreateClaimViewModel
            {
                ModuleId = 20,
                HoursWorked = 12,
                LecturerComment = "Did work",
            };

            var claim = await _service.CreateClaimAsync(2, model);

            Assert.Equal(250, claim.HourlyRate);
            Assert.Equal(12, claim.HoursWorked);
            Assert.Equal("Did work", claim.LecturerComment);
            Assert.Equal(1, _context.ContractClaims.Count());
        }

        [Fact]
        public async Task CreateClaimAsync_ZeroRate_WhenNotLinked()
        {
            var model = new CreateClaimViewModel { ModuleId = 999, HoursWorked = 5 };

            var claim = await _service.CreateClaimAsync(1, model);

            Assert.Equal(0, claim.HourlyRate);
        }

        // ---------------------------------------------------------
        // ADD FILES
        // ---------------------------------------------------------
        [Fact]
        public async Task AddFilesToClaimAsync_SavesUploadedFile()
        {
            var claim = new ContractClaim { Id = 100, LecturerUserId = 5 };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            var bytes = new byte[] { 7, 8, 9 };
            var stream = new MemoryStream(bytes);

            var formFile = new FormFile(stream, 0, bytes.Length, "file", "test.txt");

            // fileService.UploadFileAsync returns UploadedFile
            _fileServiceMock
                .Setup(f => f.UploadFileAsync(formFile))
                .ReturnsAsync(
                    new UploadedFile
                    {
                        Id = 80,
                        FileName = "test.txt",
                        FilePath = "uploads/test.txt",
                        FileSize = bytes.Length,
                    }
                );

            await _service.AddFilesToClaimAsync(claim, new List<IFormFile> { formFile });

            Assert.Equal(1, _context.ContractClaimsDocuments.Count());
            Assert.Equal(80, _context.ContractClaimsDocuments.First().UploadedFileId);
        }

        [Fact]
        public async Task AddFilesToClaimAsync_DoesNothing_WhenNull()
        {
            var claim = new ContractClaim { Id = 30, LecturerUserId = 2 };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            await _service.AddFilesToClaimAsync(claim, null);

            Assert.Empty(_context.ContractClaimsDocuments);
        }

        // ---------------------------------------------------------
        // GET FILE
        // ---------------------------------------------------------
        [Fact]
        public async Task GetFileAsync_ReturnsFile_WhenOwned()
        {
            var claim = new ContractClaim { LecturerUserId = 1 };
            _context.ContractClaims.Add(claim);

            var file = new UploadedFile
            {
                Id = 10,
                FileName = "f.pdf",
                FilePath = "uploads/f.pdf",
            };
            _context.UploadedFiles.Add(file);

            _context.ContractClaimsDocuments.Add(
                new ContractClaimDocument { ContractClaimId = claim.Id, UploadedFileId = 10 }
            );

            await _context.SaveChangesAsync();

            var expected = (
                FileStream: (Stream)new MemoryStream(new byte[] { 1 }),
                ContentType: "application/pdf",
                FileName: "f.pdf"
            );

            _fileServiceMock.Setup(f => f.GetFileAsync(10)).ReturnsAsync(expected);

            var result = await _service.GetFileAsync(10, 1);

            Assert.NotNull(result);
            Assert.Equal("f.pdf", result!.Value.FileName);
        }

        [Fact]
        public async Task GetFileAsync_ReturnsNull_WhenNotOwned()
        {
            var claim = new ContractClaim { LecturerUserId = 999 };
            _context.ContractClaims.Add(claim);

            var file = new UploadedFile
            {
                Id = 44,
                FileName = "x",
                FilePath = "x/x",
            };
            _context.UploadedFiles.Add(file);

            _context.ContractClaimsDocuments.Add(
                new ContractClaimDocument { ContractClaimId = claim.Id, UploadedFileId = 44 }
            );

            await _context.SaveChangesAsync();

            var result = await _service.GetFileAsync(44, 1);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetFileAsync_ReturnsNull_WhenNotLinked()
        {
            var result = await _service.GetFileAsync(999, 1);
            Assert.Null(result);
        }
    }
}
