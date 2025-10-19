// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68f5452c-2788-800b-bbbc-175029690cfd

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContractMonthlyClaimSystem.Services;
using Microsoft.Extensions.Configuration;

namespace ContractMonthlyClaimSystem.Tests.Services
{
    /// <summary>
    /// Unit tests for the FileEncryptionService class.
    /// Each test validates encryption, decryption, and constructor behavior.
    /// Uses temporary in-memory and disk files to ensure correctness without side effects.
    /// </summary>
    public class FileEncryptionServiceTests
    {
        private readonly IConfiguration _config;

        public FileEncryptionServiceTests()
        {
            // Build a mock configuration containing valid 32-byte key and 16-byte IV.
            // The base64 strings correspond to 32 and 16 bytes respectively.
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
        }

        [Fact]
        public async Task EncryptToFileAsync_CreatesEncryptedFile()
        {
            // Arrange: create a temporary plaintext file in memory.
            var service = new FileEncryptionService(_config);
            var plainText = "This is a test string for encryption.";
            var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(plainText));

            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".enc");
            try
            {
                // Act: perform encryption and write to disk.
                await service.EncryptToFileAsync(inputStream, tempFile);

                // Assert: file should exist and contain different bytes than plaintext.
                Assert.True(File.Exists(tempFile));
                var fileBytes = await File.ReadAllBytesAsync(tempFile);
                Assert.NotEmpty(fileBytes);
                Assert.NotEqual(Encoding.UTF8.GetBytes(plainText), fileBytes);
            }
            finally
            {
                // Cleanup: remove the temporary file.
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task DecryptToStreamAsync_RestoresOriginalPlainText()
        {
            // Arrange: prepare a small plaintext message and encrypt it.
            var service = new FileEncryptionService(_config);
            var original = "Sensitive data to be protected.";
            var plainStream = new MemoryStream(Encoding.UTF8.GetBytes(original));

            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".enc");
            try
            {
                await service.EncryptToFileAsync(plainStream, tempFile);

                // Act: decrypt the file into a memory stream.
                var decryptedStream = new MemoryStream();
                await service.DecryptToStreamAsync(tempFile, decryptedStream);

                // Assert: decrypted text matches the original.
                var result = Encoding.UTF8.GetString(decryptedStream.ToArray());
                Assert.Equal(original, result);
            }
            finally
            {
                // Cleanup
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Constructor_Throws_WhenKeyMissing()
        {
            // Arrange: build config missing the Encryption:Key field.
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

            // Act + Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new FileEncryptionService(badConfig)
            );
            Assert.Contains("Encryption Key missing", ex.Message);
        }

        [Fact]
        public void Constructor_Throws_WhenIVMissing()
        {
            // Arrange: config missing the IV.
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

            // Act + Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new FileEncryptionService(badConfig)
            );
            Assert.Contains("Encryption IV missing", ex.Message);
        }

        [Fact]
        public void Constructor_Throws_WhenKeyInvalidLength()
        {
            // Arrange: Key is too short (16 bytes instead of 32).
            var badKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("SHORTKEY0123456"));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { "Encryption:Key", badKey },
                        {
                            "Encryption:IV",
                            Convert.ToBase64String(Encoding.UTF8.GetBytes("ABCDEF0123456789"))
                        },
                    }
                )
                .Build();

            // Act + Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new FileEncryptionService(config)
            );
            Assert.Contains("Invalid Key length", ex.Message);
        }

        [Fact]
        public void Constructor_Throws_WhenIVInvalidLength()
        {
            // Arrange: IV is too long.
            var badIV = Convert.ToBase64String(Encoding.UTF8.GetBytes("TOOLONGVECTOR123456"));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        {
                            "Encryption:Key",
                            Convert.ToBase64String(
                                Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF")
                            )
                        },
                        { "Encryption:IV", badIV },
                    }
                )
                .Build();

            // Act + Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new FileEncryptionService(config)
            );
            Assert.Contains("Invalid IV length", ex.Message);
        }

        [Fact]
        public async Task DecryptToStreamAsync_Throws_WhenFileMissing()
        {
            // Arrange: valid config but missing file path.
            var service = new FileEncryptionService(_config);
            var outputStream = new MemoryStream();

            // Act + Assert: ensure FileNotFoundException is thrown.
            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                service.DecryptToStreamAsync("nonexistent.enc", outputStream)
            );
        }

        [Fact]
        public async Task EncryptToFileAsync_Throws_WhenInputStreamInvalid()
        {
            // Arrange: create service and invalid input (disposed stream).
            var service = new FileEncryptionService(_config);
            var stream = new MemoryStream();
            stream.Dispose();

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".enc");

            // Act + Assert: should fail due to invalid stream state.
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                service.EncryptToFileAsync(stream, tempPath)
            );
        }
    }
}
