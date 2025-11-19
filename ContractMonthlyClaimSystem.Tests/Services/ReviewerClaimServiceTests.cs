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
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _tempRoot = Path.Combine(Path.GetTempPath(), $"testroot_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempRoot);

            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns(_tempRoot);

            _encryptionMock = new Mock<IFileEncryptionService>();

            _userServiceMock = new Mock<IUserService>();

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
                Directory.Delete(_tempRoot, true);
        }

        // -----------------------------------------------------------
        // Claim Retrieval
        // -----------------------------------------------------------

        [Fact]
        public async Task GetClaimsAsync_ReturnsAllClaimsWithIncludes()
        {
            var module = new Module { Name = "Programming", Code = "PROG6212" };
            var lecturer = new AppUser { Id = 10, UserName = "lecturer@test" };

            _context.Modules.Add(module);
            _context.Users.Add(lecturer);
            _context.ContractClaims.Add(
                new ContractClaim { Module = module, LecturerUser = lecturer }
            );

            await _context.SaveChangesAsync();

            var result = await _service.GetClaimsAsync();

            Assert.Single(result);
            Assert.Equal(module.Id, result.First().ModuleId);
            Assert.Equal(lecturer.Id, result.First().LecturerUserId);
        }

        [Fact]
        public async Task GetClaimAsync_ReturnsSpecificClaim()
        {
            var lecturer = new AppUser { Id = 20, UserName = "x" };
            var module = new Module { Name = "M", Code = "C" };

            _context.Users.Add(lecturer);
            _context.Modules.Add(module);

            var claim = new ContractClaim { LecturerUser = lecturer, Module = module };
            _context.ContractClaims.Add(claim);

            await _context.SaveChangesAsync();

            var result = await _service.GetClaimAsync(claim.Id);

            Assert.NotNull(result);
            Assert.Equal(claim.Id, result!.Id);
        }

        // -----------------------------------------------------------
        // Claim Files
        // -----------------------------------------------------------

        [Fact]
        public async Task GetClaimFilesAsync_ReturnsLinkedFiles()
        {
            var claim = new ContractClaim { Id = 1 };
            var file = new UploadedFile
            {
                Id = 5,
                FileName = "doc.pdf",
                FilePath = "/x",
            };

            _context.ContractClaims.Add(claim);
            _context.UploadedFiles.Add(file);

            _context.ContractClaimsDocuments.Add(
                new ContractClaimDocument
                {
                    ContractClaimId = 1,
                    UploadedFileId = 5,
                    UploadedFile = file,
                }
            );

            await _context.SaveChangesAsync();

            var result = await _service.GetClaimFilesAsync(claim);

            Assert.Single(result!);
            Assert.Equal("doc.pdf", result![0].FileName);
        }

        // -----------------------------------------------------------
        // ReviewClaimAsync – Validation
        // -----------------------------------------------------------

        [Fact]
        public async Task ReviewClaim_ReturnsFalse_WhenUserNotFound()
        {
            _userServiceMock
                .Setup(u => u.GetUserAsync(It.IsAny<int>()))
                .ReturnsAsync((AppUser?)null);

            var res = await _service.ReviewClaimAsync(1, 9999, true, "ok");

            Assert.False(res);
        }

        [Fact]
        public async Task ReviewClaim_ReturnsFalse_WhenUserHasNoValidRole()
        {
            _userServiceMock
                .Setup(u => u.GetUserAsync(1))
                .ReturnsAsync(
                    new AppUser
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
                    }
                );

            var lecturer = new AppUser { UserName = "L" };
            var module = new Module { Code = "M1", Name = "M" };

            _context.Users.Add(lecturer);
            _context.Modules.Add(module);
            _context.ContractClaims.Add(
                new ContractClaim { LecturerUser = lecturer, Module = module }
            );

            await _context.SaveChangesAsync();

            var res = await _service.ReviewClaimAsync(1, 1, true, "x");

            Assert.False(res);
        }

        // -----------------------------------------------------------
        // ReviewClaimAsync – Updates
        // -----------------------------------------------------------

        [Fact]
        public async Task ReviewClaim_Updates_ForProgramCoordinator()
        {
            _userServiceMock
                .Setup(u => u.GetUserAsync(1))
                .ReturnsAsync(
                    new AppUser
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
                    }
                );

            var lecturer = new AppUser { Id = 3, UserName = "L" };
            var module = new Module { Code = "M1", Name = "M" };

            _context.Users.Add(lecturer);
            _context.Modules.Add(module);
            _context.ContractClaims.Add(
                new ContractClaim
                {
                    Id = 10,
                    LecturerUser = lecturer,
                    Module = module,
                }
            );

            await _context.SaveChangesAsync();

            var res = await _service.ReviewClaimAsync(10, 1, true, "OK");

            Assert.True(res);
            var updated = await _context.ContractClaims.FindAsync(10);

            Assert.Equal(ClaimDecision.VERIFIED, updated?.ProgramCoordinatorDecision);
            Assert.Equal("OK", updated?.ProgramCoordinatorComment);
        }

        [Fact]
        public async Task ReviewClaim_Updates_ForAcademicManager()
        {
            _userServiceMock
                .Setup(u => u.GetUserAsync(2))
                .ReturnsAsync(
                    new AppUser
                    {
                        Id = 2,
                        UserRoles =
                        [
                            new AppUserRole
                            {
                                UserId = 2,
                                Role = new AppRole { Name = "AcademicManager" },
                            },
                        ],
                    }
                );

            var lecturer = new AppUser { Id = 5 };
            var module = new Module { Code = "M1", Name = "M" };

            _context.Users.Add(lecturer);
            _context.Modules.Add(module);

            _context.ContractClaims.Add(
                new ContractClaim
                {
                    Id = 20,
                    LecturerUser = lecturer,
                    Module = module,
                }
            );

            await _context.SaveChangesAsync();

            var res = await _service.ReviewClaimAsync(20, 2, false, "Bad");

            Assert.True(res);

            var updated = await _context.ContractClaims.FindAsync(20);
            Assert.Equal(ClaimDecision.REJECTED, updated!.AcademicManagerDecision);
            Assert.Equal("Bad", updated.AcademicManagerComment);
        }

        // -----------------------------------------------------------
        // GetFileAsync
        // -----------------------------------------------------------

        [Fact]
        public async Task GetFileAsync_ReturnsDecryptedFile()
        {
            var file = new UploadedFile
            {
                Id = 99,
                FileName = "x.txt",
                FilePath = Path.Combine("uploads", "x.txt"),
            };

            _context.UploadedFiles.Add(file);
            await _context.SaveChangesAsync();

            var full = Path.Combine(_tempRoot, file.FilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await File.WriteAllBytesAsync(full, new byte[] { 5, 5, 5 });

            _encryptionMock
                .Setup(e => e.DecryptToStreamAsync(full, It.IsAny<MemoryStream>()))
                .Returns(Task.CompletedTask);

            var result = await _service.GetFileAsync(99);

            Assert.NotNull(result);
            Assert.Equal("x.txt", result?.FileName);

            result?.FileStream?.Dispose();
        }

        [Fact]
        public async Task GetFileAsync_ReturnsNull_WhenFileMissing()
        {
            Assert.Null(await _service.GetFileAsync(555));
        }

        // -----------------------------------------------------------
        // AutoReviewRule CRUD
        // -----------------------------------------------------------

        [Fact]
        public async Task AddAutoReviewRuleAsync_AddsRule()
        {
            var rule = new AutoReviewRule { ReviewerId = 1, Priority = 5 };

            await _service.AddAutoReviewRuleAsync(rule);

            Assert.Single(_context.AutoReviewRules);
        }

        [Fact]
        public async Task GetAutoReviewRulesForUserAsync_ReturnsCorrectRules()
        {
            _context.AutoReviewRules.Add(new AutoReviewRule { ReviewerId = 1 });
            _context.AutoReviewRules.Add(new AutoReviewRule { ReviewerId = 2 });
            await _context.SaveChangesAsync();

            var res = await _service.GetAutoReviewRulesForUserAsync(1);

            Assert.Single(res);
            Assert.Equal(1, res.First().ReviewerId);
        }

        [Fact]
        public async Task GetAutoReviewRule_ReturnsSpecificRule()
        {
            var r = new AutoReviewRule { ReviewerId = 3 };
            _context.AutoReviewRules.Add(r);
            await _context.SaveChangesAsync();

            var res = await _service.GetAutoReviewRule(r.Id);

            Assert.NotNull(res);
            Assert.Equal(3, res?.ReviewerId);
        }

        [Fact]
        public async Task UpdateAutoReviewRuleAsync_UpdatesRule()
        {
            var r = new AutoReviewRule { ReviewerId = 5, AutoComment = "" };
            _context.AutoReviewRules.Add(r);
            await _context.SaveChangesAsync();

            r.AutoComment = "Updated";

            await _service.UpdateAutoReviewRuleAsync(r.Id, 5, r);

            var updated = await _context.AutoReviewRules.FindAsync(r.Id);
            Assert.Equal("Updated", updated!.AutoComment);
        }

        [Fact]
        public async Task RemoveAutoReviewRuleAsync_RemovesRule()
        {
            var r = new AutoReviewRule { ReviewerId = 7 };
            _context.AutoReviewRules.Add(r);
            await _context.SaveChangesAsync();

            await _service.RemoveAutoReviewRuleAsync(r.Id, 7);

            Assert.Empty(_context.AutoReviewRules);
        }

        // -----------------------------------------------------------
        // AutoReviewPendingClaimsAsync
        // -----------------------------------------------------------

        [Fact]
        public async Task AutoReviewPendingClaimsAsync_ReviewsMatchingClaims()
        {
            _userServiceMock
                .Setup(u => u.GetUserAsync(100))
                .ReturnsAsync(
                    new AppUser
                    {
                        Id = 100,
                        UserRoles =
                        [
                            new AppUserRole
                            {
                                UserId = 100,
                                Role = new AppRole { Name = "ProgramCoordinator" },
                            },
                        ],
                    }
                );

            var lecturer = new AppUser { Id = 50, UserName = "lect" };
            var module = new Module
            {
                Id = 10,
                Code = "X",
                Name = "Test",
            };

            _context.Users.Add(lecturer);
            _context.Modules.Add(module);

            var c1 = new ContractClaim
            {
                Id = 1,
                LecturerUserId = lecturer.Id,
                ModuleId = module.Id,

                HourlyRate = 100,
                HoursWorked = 1,

                ProgramCoordinatorUserId = null,
                AcademicManagerUserId = null,

                ClaimStatus = ClaimStatus.PENDING,
            };

            _context.ContractClaims.Add(c1);

            _context.AutoReviewRules.Add(
                new AutoReviewRule
                {
                    ReviewerId = 100,
                    Priority = 1,
                    ComparisonVar = AutoReviewComparisonVar.HOURLY_RATE,
                    ComparisonOp = AutoReviewComparisonOp.GREATER_THAN,
                    ComparisonValue = 50,
                    AutoDecision = ClaimDecision.VERIFIED,
                    AutoComment = "Auto OK",
                }
            );

            await _context.SaveChangesAsync();

            var result = await _service.AutoReviewPendingClaimsAsync(100);

            Assert.Equal(1, result.reviewed);

            var updated = await _context.ContractClaims.FindAsync(1);
            Assert.Equal(ClaimDecision.VERIFIED, updated!.ProgramCoordinatorDecision);
        }
    }
}
