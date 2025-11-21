// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/691f16e8-5034-800b-898a-2c7eb4000f43

using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContractMonthlyClaimSystem.Tests.Services
{
    public class FileServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<IFileEncryptionService> _encryptMock;
        private readonly FileService _service;
        private readonly string _root;

        public FileServiceTests()
        {
            // DB
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            // Root folder
            _root = Path.Combine(Path.GetTempPath(), $"webroot_{Guid.NewGuid()}");
            Directory.CreateDirectory(_root);

            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns(_root);

            _encryptMock = new Mock<IFileEncryptionService>();

            _service = new FileService(_context, _envMock.Object, _encryptMock.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        // -------------------------------------------------------------
        // UploadFileAsync(Stream)
        // -------------------------------------------------------------
        [Fact]
        public async Task UploadFileAsync_SavesEncryptedFile_AndCreatesDbRecord()
        {
            var data = new byte[] { 10, 20, 30, 40, 50 };
            using var ms = new MemoryStream(data);

            string? capturedPath = null;

            _encryptMock
                .Setup(e => e.EncryptToFileAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .Callback<Stream, string>(
                    (input, savePath) =>
                    {
                        capturedPath = savePath;
                        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

                        // simulate encrypted output
                        File.WriteAllBytes(savePath, new byte[] { 9, 9, 9 });
                    }
                )
                .Returns(Task.CompletedTask);

            var result = await _service.UploadFileAsync(ms, "test.txt", data.Length);

            Assert.NotNull(result);
            Assert.Single(_context.UploadedFiles);

            Assert.NotNull(capturedPath);
            Assert.True(File.Exists(capturedPath!), "Encrypted file should exist on disk.");
        }

        // -------------------------------------------------------------
        // UploadFileAsync(IFormFile)
        // -------------------------------------------------------------
        [Fact]
        public async Task UploadFileAsync_IFormFile_WrapsCorrectly()
        {
            var bytes = new byte[] { 5, 5, 5, 5 };
            var ms = new MemoryStream(bytes);

            var formFile = new FormFile(ms, 0, bytes.Length, "file", "hello.txt");

            string? capturedPath = null;

            _encryptMock
                .Setup(e => e.EncryptToFileAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .Callback<Stream, string>(
                    (input, savePath) =>
                    {
                        capturedPath = savePath;
                        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

                        // simulate encrypted output
                        File.WriteAllBytes(savePath, new byte[] { 9, 9, 9 });
                    }
                )
                .Returns(Task.CompletedTask);

            var result = await _service.UploadFileAsync(formFile);

            Assert.NotNull(result);
            Assert.Equal("hello.txt", result!.FileName);

            Assert.NotNull(capturedPath);
            Assert.True(File.Exists(capturedPath!), "Encrypted file should exist.");
        }

        // -------------------------------------------------------------
        // UploadFileAsync null cases
        // -------------------------------------------------------------
        [Fact]
        public async Task UploadFileAsync_ReturnsNull_WhenNameMissing()
        {
            using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
            var result = await _service.UploadFileAsync(ms, "", 3);

            Assert.Null(result);
        }

        [Fact]
        public async Task UploadFileAsync_ReturnsNull_WhenSizeInvalid()
        {
            using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
            var result = await _service.UploadFileAsync(ms, "x.txt", 0);

            Assert.Null(result);
        }

        // -------------------------------------------------------------
        // GetFileAsync
        // -------------------------------------------------------------
        [Fact]
        public async Task GetFileAsync_ReturnsDecryptedStream_WhenExists()
        {
            // DB record
            var file = new UploadedFile
            {
                FileName = "a.txt",
                FilePath = "uploads/a.txt",
                FileSize = 3,
            };

            _context.UploadedFiles.Add(file);
            await _context.SaveChangesAsync();

            // Physical file (encrypted)
            var fullPath = Path.Combine(
                _root,
                file.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString())
            );
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await File.WriteAllBytesAsync(fullPath, new byte[] { 9, 9, 9 });

            // Mock decrypted stream
            var decryptedStream = new MemoryStream(new byte[] { 100, 101, 102 });

            _encryptMock
                .Setup(e => e.OpenDecryptedRead(It.IsAny<string>()))
                .Returns(decryptedStream);

            var result = await _service.GetFileAsync(file.Id);

            Assert.NotNull(result);
            Assert.Equal("a.txt", result!.Value.FileName);
            Assert.Equal("text/plain", result.Value.ContentType);

            // Ensure the returned stream is correct
            var buffer = new byte[3];
            await result.Value.FileStream.ReadAsync(buffer, 0, 3);

            Assert.Equal(new byte[] { 100, 101, 102 }, buffer);
        }

        // -------------------------------------------------------------
        // GetFileAsync edge cases
        // -------------------------------------------------------------
        [Fact]
        public async Task GetFileAsync_ReturnsNull_WhenNotFound()
        {
            var result = await _service.GetFileAsync(999);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFileAsync_ReturnsNull_WhenPhysicalMissing()
        {
            var file = new UploadedFile
            {
                FileName = "missing.txt",
                FilePath = "uploads/missing.txt",
                FileSize = 1,
            };

            _context.UploadedFiles.Add(file);
            await _context.SaveChangesAsync();

            // No file written

            var result = await _service.GetFileAsync(file.Id);

            Assert.Null(result);
        }

        // -------------------------------------------------------------
        // DeleteFileAsync
        // -------------------------------------------------------------
        [Fact]
        public async Task DeleteFileAsync_ReturnsFalse_WhenFileNotFoundInDb()
        {
            var result = await _service.DeleteFileAsync(999);
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFileAsync_DeletesPhysicalFile_AndRemovesDbRecord()
        {
            // Arrange DB record
            var file = new UploadedFile
            {
                FileName = "gone.txt",
                FilePath = "uploads/gone.txt",
                FileSize = 10,
            };

            _context.UploadedFiles.Add(file);
            await _context.SaveChangesAsync();

            // Arrange physical file
            var fullPath = Path.Combine(_root, "uploads", "gone.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllBytes(fullPath, new byte[] { 1, 2, 3 });

            // Act
            var result = await _service.DeleteFileAsync(file.Id);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(fullPath));
            Assert.Empty(_context.UploadedFiles);
        }

        [Fact]
        public async Task DeleteFileAsync_ReturnsTrue_WhenFileDoesNotExistPhysically()
        {
            // Arrange DB record
            var file = new UploadedFile
            {
                FileName = "nofile.txt",
                FilePath = "uploads/nofile.txt",
                FileSize = 10,
            };

            _context.UploadedFiles.Add(file);
            await _context.SaveChangesAsync();

            // Physical file intentionally not created

            // Act
            var result = await _service.DeleteFileAsync(file.Id);

            // Assert
            Assert.True(result);
            Assert.Empty(_context.UploadedFiles);
        }

        [Fact]
        public async Task DeleteFileAsync_RestoresDbRecord_WhenPhysicalDeleteFails()
        {
            // Arrange DB record
            var file = new UploadedFile
            {
                FileName = "fail.txt",
                FilePath = "uploads/fail.txt",
                FileSize = 10,
            };

            _context.UploadedFiles.Add(file);
            await _context.SaveChangesAsync();

            // Create a directory instead of a file to force delete failure
            var fullPath = Path.Combine(_root, "uploads", "fail.txt");

            // This ensures the parent folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            // Create a DIRECTORY where a FILE should be
            Directory.CreateDirectory(fullPath);

            // Act
            var result = await _service.DeleteFileAsync(file.Id);

            // Assert
            Assert.False(result); // delete should fail
            Assert.True(Directory.Exists(fullPath)); // still exists (cannot delete)
            Assert.Single(_context.UploadedFiles); // DB restored
            Assert.Equal(file.Id, _context.UploadedFiles.Single().Id);
        }
    }
}
