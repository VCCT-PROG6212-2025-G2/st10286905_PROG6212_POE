// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/691f16e8-5034-800b-898a-2c7eb4000f43

using System.Text;
using ContractMonthlyClaimSystem.Services;
using Microsoft.Extensions.Configuration;

namespace ContractMonthlyClaimSystem.Tests.Services
{
    /// <summary>
    /// Complete test suite for FileEncryptionService.
    /// Tests encryption, decryption, OpenDecryptedRead, constructor validation,
    /// and error edge cases.
    /// </summary>
    public class FileEncryptionServiceTests : IDisposable
    {
        private readonly IConfiguration _config;
        private readonly string _tempDir;

        public FileEncryptionServiceTests()
        {
            // Valid AES-256 key (32 bytes) + 16-byte IV
            var inMemorySettings = new Dictionary<string, string?>
            {
                {
                    "Encryption:Key",
                    Convert.ToBase64String(
                        Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF")
                    )
                },
                {
                    "Encryption:IV",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes("ABCDEF0123456789"))
                },
            };

            _config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            _tempDir = Path.Combine(Path.GetTempPath(), $"enc_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        // ============================================================
        // ENCRYPT → BASIC BEHAVIOR
        // ============================================================
        [Fact]
        public async Task EncryptToFileAsync_CreatesEncryptedFile()
        {
            var service = new FileEncryptionService(_config);

            var plaintext = "This is a test.";
            using var input = new MemoryStream(Encoding.UTF8.GetBytes(plaintext));

            string encPath = Path.Combine(_tempDir, "file.enc");

            await service.EncryptToFileAsync(input, encPath);

            Assert.True(File.Exists(encPath));

            var encryptedBytes = await File.ReadAllBytesAsync(encPath);

            Assert.NotEmpty(encryptedBytes);
            Assert.NotEqual(Encoding.UTF8.GetBytes(plaintext), encryptedBytes);
        }

        // ============================================================
        // DECRYPT → MATCHES ORIGINAL
        // ============================================================
        [Fact]
        public async Task DecryptToStreamAsync_RestoresOriginalPlainText()
        {
            var service = new FileEncryptionService(_config);

            var original = "Secret invoice data!";
            var plainStream = new MemoryStream(Encoding.UTF8.GetBytes(original));

            string encPath = Path.Combine(_tempDir, "file.enc");

            await service.EncryptToFileAsync(plainStream, encPath);

            var output = new MemoryStream();
            await service.DecryptToStreamAsync(encPath, output);

            string decrypted = Encoding.UTF8.GetString(output.ToArray());
            Assert.Equal(original, decrypted);
        }

        // ============================================================
        // OpenDecryptedRead → STREAM VALIDATION
        // ============================================================
        [Fact]
        public async Task OpenDecryptedRead_ReturnsReadableDecryptedStream()
        {
            var service = new FileEncryptionService(_config);

            var original = "Data for streaming decrypt.";
            var plain = new MemoryStream(Encoding.UTF8.GetBytes(original));

            string encPath = Path.Combine(_tempDir, "stream.enc");
            await service.EncryptToFileAsync(plain, encPath);

            using var decryptedStream = service.OpenDecryptedRead(encPath);
            using var mem = new MemoryStream();

            await decryptedStream.CopyToAsync(mem);

            string text = Encoding.UTF8.GetString(mem.ToArray());
            Assert.Equal(original, text);
        }

        // ============================================================
        // OpenDecryptedRead → FILE HANDLE BEHAVIOR
        // ============================================================
        [Fact]
        public async Task OpenDecryptedRead_ClosesUnderlyingFileStream_WhenDisposed()
        {
            var service = new FileEncryptionService(_config);

            var original = "Close stream test.";
            string encPath = Path.Combine(_tempDir, "test.enc");

            await service.EncryptToFileAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(original)),
                encPath
            );

            Stream? stream = service.OpenDecryptedRead(encPath);
            stream.Dispose(); // Force closure

            // Attempt reopening file to verify handle released
            using var reopened = File.Open(encPath, FileMode.Open, FileAccess.Read, FileShare.None);
            Assert.NotNull(reopened);
        }

        // ============================================================
        // MISSING FILE → DECRYPT SHOULD THROW
        // ============================================================
        [Fact]
        public async Task DecryptToStreamAsync_Throws_WhenFileMissing()
        {
            var service = new FileEncryptionService(_config);

            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await service.DecryptToStreamAsync(
                    Path.Combine(_tempDir, "does_not_exist.enc"),
                    new MemoryStream()
                )
            );
        }

        // ============================================================
        // INVALID STREAM → ENCRYPT SHOULD THROW
        // ============================================================
        [Fact]
        public async Task EncryptToFileAsync_Throws_WhenInputStreamDisposed()
        {
            var service = new FileEncryptionService(_config);

            var disposedStream = new MemoryStream();
            disposedStream.Dispose();

            string encPath = Path.Combine(_tempDir, "bad.enc");

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                service.EncryptToFileAsync(disposedStream, encPath)
            );
        }

        // ============================================================
        // CONSTRUCTOR VALIDATION
        // ============================================================
        [Fact]
        public void Constructor_Throws_WhenKeyMissing()
        {
            var badConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        {
                            "Encryption:IV",
                            Convert.ToBase64String(Encoding.UTF8.GetBytes("ABCDEF0123456789"))
                        },
                    }
                )
                .Build();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new FileEncryptionService(badConfig)
            );
            Assert.Contains("Encryption Key missing", ex.Message);
        }

        [Fact]
        public void Constructor_Throws_WhenIVMissing()
        {
            var badConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        {
                            "Encryption:Key",
                            Convert.ToBase64String(
                                Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF")
                            )
                        },
                    }
                )
                .Build();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new FileEncryptionService(badConfig)
            );
            Assert.Contains("Encryption IV missing", ex.Message);
        }

        [Fact]
        public void Constructor_Throws_WhenKeyInvalidLength()
        {
            var badConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        {
                            "Encryption:Key",
                            Convert.ToBase64String(Encoding.UTF8.GetBytes("TOO_SHORT_KEY"))
                        },
                        {
                            "Encryption:IV",
                            Convert.ToBase64String(Encoding.UTF8.GetBytes("ABCDEF0123456789"))
                        },
                    }
                )
                .Build();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new FileEncryptionService(badConfig)
            );

            Assert.Contains("Invalid Key length", ex.Message);
        }

        [Fact]
        public void Constructor_Throws_WhenIVInvalidLength()
        {
            var badConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        {
                            "Encryption:Key",
                            Convert.ToBase64String(
                                Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF")
                            )
                        },
                        {
                            "Encryption:IV",
                            Convert.ToBase64String(Encoding.UTF8.GetBytes("TOO_LONG_IV_123456"))
                        },
                    }
                )
                .Build();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new FileEncryptionService(badConfig)
            );

            Assert.Contains("Invalid IV length", ex.Message);
        }
    }
}
