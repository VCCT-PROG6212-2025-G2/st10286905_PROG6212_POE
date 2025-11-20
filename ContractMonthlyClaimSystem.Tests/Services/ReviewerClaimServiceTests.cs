// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/691f16e8-5034-800b-898a-2c7eb4000f43

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
    public class ReviewerClaimServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IFileService> _fileServiceMock;
        private readonly ReviewerClaimService _service;

        public ReviewerClaimServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _userServiceMock = new Mock<IUserService>();
            _fileServiceMock = new Mock<IFileService>();

            _service = new ReviewerClaimService(
                _context,
                _userServiceMock.Object,
                _fileServiceMock.Object
            );
        }

        // -----------------------------------------------------------
        // GetClaimsAsync
        // -----------------------------------------------------------
        [Fact]
        public async Task GetClaimsAsync_ReturnsAllClaims()
        {
            var c = new ContractClaim
            {
                Module = new Module { Code = "X", Name = "Test" },
                LecturerUser = new AppUser { Id = 1 },
            };

            _context.ContractClaims.Add(c);
            await _context.SaveChangesAsync();

            var result = await _service.GetClaimsAsync();

            Assert.Single(result);
            Assert.NotNull(result[0].Module);
            Assert.NotNull(result[0].LecturerUser);
        }

        // -----------------------------------------------------------
        // GetClaimAsync
        // -----------------------------------------------------------
        [Fact]
        public async Task GetClaimAsync_ReturnsClaim()
        {
            var lecturer = new AppUser { Id = 5, UserName = "lect" };
            var module = new Module { Code = "C101", Name = "Networking" };

            _context.Users.Add(lecturer);
            _context.Modules.Add(module);

            var claim = new ContractClaim
            {
                LecturerUserId = lecturer.Id,
                LecturerUser = lecturer,
                ModuleId = module.Id,
                Module = module,
            };

            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            var result = await _service.GetClaimAsync(claim.Id);

            Assert.NotNull(result);
            Assert.Equal(claim.Id, result!.Id);
            Assert.Equal(module.Id, result.ModuleId);
            Assert.Equal(lecturer.Id, result.LecturerUserId);
        }

        // -----------------------------------------------------------
        // GetClaimFilesAsync
        // -----------------------------------------------------------
        [Fact]
        public async Task GetClaimFilesAsync_ReturnsFiles()
        {
            var claim = new ContractClaim { Id = 10 };
            var file = new UploadedFile
            {
                Id = 50,
                FileName = "a.pdf",
                FilePath = "x",
            };

            _context.ContractClaims.Add(claim);
            _context.UploadedFiles.Add(file);
            _context.ContractClaimsDocuments.Add(
                new ContractClaimDocument
                {
                    ContractClaimId = 10,
                    UploadedFileId = 50,
                    UploadedFile = file,
                }
            );

            await _context.SaveChangesAsync();

            var result = await _service.GetClaimFilesAsync(claim);

            Assert.Single(result!);
            Assert.Equal("a.pdf", result![0].FileName);
        }

        // -----------------------------------------------------------
        // ReviewClaimAsync — invalid user
        // -----------------------------------------------------------
        [Fact]
        public async Task ReviewClaimAsync_ReturnsFalse_WhenUserNotFound()
        {
            _userServiceMock.Setup(u => u.GetUserAsync(99)).ReturnsAsync((AppUser?)null);

            var res = await _service.ReviewClaimAsync(1, 99, true, "ok");

            Assert.False(res);
        }

        // -----------------------------------------------------------
        // ReviewClaimAsync — user missing role
        // -----------------------------------------------------------
        [Fact]
        public async Task ReviewClaimAsync_ReturnsFalse_WhenUserHasNoReviewRole()
        {
            _userServiceMock
                .Setup(u => u.GetUserAsync(1))
                .ReturnsAsync(
                    new AppUser
                    {
                        Id = 1,
                        UserRoles = [new AppUserRole { Role = new AppRole { Name = "Lecturer" } }],
                    }
                );

            _context.ContractClaims.Add(new ContractClaim());
            await _context.SaveChangesAsync();

            var res = await _service.ReviewClaimAsync(1, 1, true, "x");

            Assert.False(res);
        }

        // -----------------------------------------------------------
        // PC review
        // -----------------------------------------------------------
        [Fact]
        public async Task ReviewClaimAsync_ProgramCoordinator_UpdatesFields()
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

            var claim = new ContractClaim { Id = 30 };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            var res = await _service.ReviewClaimAsync(30, 1, true, "OK");
            Assert.True(res);

            var updated = await _context.ContractClaims.FindAsync(30);
            Assert.Equal(ClaimDecision.VERIFIED, updated!.ProgramCoordinatorDecision);
            Assert.Equal("OK", updated.ProgramCoordinatorComment);
        }

        // -----------------------------------------------------------
        // AM review
        // -----------------------------------------------------------
        [Fact]
        public async Task ReviewClaimAsync_AcademicManager_UpdatesFields()
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

            var claim = new ContractClaim { Id = 40 };
            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();

            var res = await _service.ReviewClaimAsync(40, 2, false, "Bad");
            Assert.True(res);

            var updated = await _context.ContractClaims.FindAsync(40);

            Assert.Equal(ClaimDecision.REJECTED, updated!.AcademicManagerDecision);
            Assert.Equal("Bad", updated.AcademicManagerComment);
        }

        // -----------------------------------------------------------
        // GetFileAsync
        // -----------------------------------------------------------
        [Fact]
        public async Task GetFileAsync_ReturnsFile_WhenFileServiceReturns()
        {
            var expected = (
                FileStream: (Stream)new MemoryStream(new byte[] { 1 }),
                ContentType: "application/pdf",
                FileName: "x.pdf"
            );

            _fileServiceMock.Setup(f => f.GetFileAsync(10)).ReturnsAsync(expected);

            var result = await _service.GetFileAsync(10);

            Assert.NotNull(result);
            Assert.Equal("x.pdf", result!.Value.FileName);
        }

        [Fact]
        public async Task GetFileAsync_ReturnsNull_WhenFileServiceReturnsNull()
        {
            _fileServiceMock
                .Setup(f => f.GetFileAsync(It.IsAny<int>()))
                .ReturnsAsync(((Stream, string, string)?)null);

            Assert.Null(await _service.GetFileAsync(55));
        }

        // -----------------------------------------------------------
        // AutoReviewRule CRUD
        // -----------------------------------------------------------
        [Fact]
        public async Task AddAutoReviewRuleAsync_AddsRule()
        {
            var rule = new AutoReviewRule { ReviewerId = 1 };

            await _service.AddAutoReviewRuleAsync(rule);
            Assert.Single(_context.AutoReviewRules);
        }

        [Fact]
        public async Task GetAutoReviewRulesForUserAsync_ReturnsCorrect()
        {
            _context.AutoReviewRules.Add(new AutoReviewRule { ReviewerId = 1 });
            _context.AutoReviewRules.Add(new AutoReviewRule { ReviewerId = 2 });
            await _context.SaveChangesAsync();

            var res = await _service.GetAutoReviewRulesForUserAsync(1);

            Assert.Single(res);
            Assert.Equal(1, res[0].ReviewerId);
        }

        [Fact]
        public async Task UpdateAutoReviewRuleAsync_UpdatesRecord()
        {
            var r = new AutoReviewRule { ReviewerId = 3, AutoComment = "old" };
            _context.AutoReviewRules.Add(r);
            await _context.SaveChangesAsync();

            r.AutoComment = "new";

            await _service.UpdateAutoReviewRuleAsync(r.Id, 3, r);

            var updated = await _context.AutoReviewRules.FindAsync(r.Id);
            Assert.Equal("new", updated!.AutoComment);
        }

        [Fact]
        public async Task RemoveAutoReviewRuleAsync_Removes()
        {
            var r = new AutoReviewRule { ReviewerId = 4 };
            _context.AutoReviewRules.Add(r);
            await _context.SaveChangesAsync();

            await _service.RemoveAutoReviewRuleAsync(r.Id, 4);
            Assert.Empty(_context.AutoReviewRules);
        }

        // -----------------------------------------------------------
        // AutoReviewPendingClaimsAsync
        // -----------------------------------------------------------
        [Fact]
        public async Task AutoReviewPendingClaimsAsync_ReviewsMatching()
        {
            // User role
            _userServiceMock
                .Setup(u => u.GetUserAsync(10))
                .ReturnsAsync(
                    new AppUser
                    {
                        Id = 10,
                        UserRoles =
                        [
                            new AppUserRole { Role = new AppRole { Name = "ProgramCoordinator" } },
                        ],
                    }
                );

            // Lecture + Module
            _context.Users.Add(new AppUser { Id = 50 });
            _context.Modules.Add(
                new Module
                {
                    Id = 2,
                    Code = "ABC123",
                    Name = "Google Engineering",
                }
            );

            var claim = new ContractClaim
            {
                Id = 1,
                LecturerUserId = 50,
                ModuleId = 2,
                HourlyRate = 200,
                HoursWorked = 2,
                ClaimStatus = ClaimStatus.PENDING,
            };

            _context.ContractClaims.Add(claim);

            _context.AutoReviewRules.Add(
                new AutoReviewRule
                {
                    ReviewerId = 10,
                    Priority = 1,
                    ComparisonVar = AutoReviewComparisonVar.HOURLY_RATE,
                    ComparisonOp = AutoReviewComparisonOp.GREATER_THAN,
                    ComparisonValue = 100,
                    AutoDecision = ClaimDecision.VERIFIED,
                    AutoComment = "Auto rule",
                }
            );

            await _context.SaveChangesAsync();

            var result = await _service.AutoReviewPendingClaimsAsync(10);

            Assert.Equal(1, result.reviewed);

            var updated = await _context.ContractClaims.FindAsync(1);
            Assert.Equal(ClaimDecision.VERIFIED, updated!.ProgramCoordinatorDecision);
        }
    }
}
