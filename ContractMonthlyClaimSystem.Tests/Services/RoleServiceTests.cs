using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContractMonthlyClaimSystem.Tests.Services
{
    /// <summary>
    /// Unit tests for the RoleService class.
    /// This suite uses mocked UserManager and RoleManager dependencies to isolate behavior.
    /// Each test focuses on a single responsibility:
    ///     - Retrieving users and roles
    ///     - Creating, deleting, and updating roles
    ///     - Updating user role assignments
    /// </summary>
    public class RoleServiceTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly RoleService _service;

        public RoleServiceTests()
        {
            // Mock user store and role store to satisfy manager constructors.
            var userStore = new Mock<IUserStore<AppUser>>();
            var roleStore = new Mock<IRoleStore<IdentityRole>>();

            // Create mock managers using dummy dependencies.
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

            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                roleStore.Object,
                null,
                null,
                null,
                null
            );

            // Instantiate the service under test with mocks.
            _service = new RoleService(_userManagerMock.Object, _roleManagerMock.Object);
        }

        [Fact]
        public async Task GetUsersWithRolesAsync_ReturnsExpectedPairs()
        {
            // Arrange: create sample users and configure GetRolesAsync behavior.
            var users = new List<AppUser>
            {
                new() { Id = "U1", UserName = "user1@cmcs.app" },
                new() { Id = "U2", UserName = "user2@cmcs.app" },
            };

            // Mock IQueryable Users collection for EF Core ToListAsync().
            var queryable = users.AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(u => u.Users).Returns(queryable.Object);

            // Mock role retrieval per user.
            _userManagerMock
                .Setup(u => u.GetRolesAsync(users[0]))
                .ReturnsAsync(new List<string> { "Lecturer" });
            _userManagerMock
                .Setup(u => u.GetRolesAsync(users[1]))
                .ReturnsAsync(new List<string> { "ProgramCoordinator" });

            // Act: call the service.
            var result = await _service.GetUsersWithRolesAsync();

            // Assert: both users and their roles should be returned.
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Roles.Contains("Lecturer"));
            Assert.Contains(result, r => r.Roles.Contains("ProgramCoordinator"));
        }

        [Fact]
        public async Task GetRolesAsync_ReturnsAllRoles()
        {
            // Arrange: mock roles for the RoleManager.
            var roles = new List<IdentityRole>
            {
                new("Admin"),
                new("Lecturer"),
                new("AcademicManager"),
            };

            var queryable = roles.AsQueryable().BuildMockDbSet();
            _roleManagerMock.Setup(r => r.Roles).Returns(queryable.Object);

            // Act
            var result = await _service.GetRolesAsync();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, r => r.Name == "Admin");
        }

        [Fact]
        public async Task CreateRoleAsync_CreatesRole_WhenNotExisting()
        {
            // Arrange: configure RoleExistsAsync to return false initially.
            _roleManagerMock.Setup(r => r.RoleExistsAsync("NewRole")).ReturnsAsync(false);
            _roleManagerMock
                .Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _service.CreateRoleAsync("NewRole");

            // Assert: ensure CreateAsync was called once.
            _roleManagerMock.Verify(
                r => r.CreateAsync(It.Is<IdentityRole>(x => x.Name == "NewRole")),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateRoleAsync_DoesNothing_WhenRoleAlreadyExists()
        {
            // Arrange: simulate an already existing role.
            _roleManagerMock.Setup(r => r.RoleExistsAsync("ExistingRole")).ReturnsAsync(true);

            // Act
            await _service.CreateRoleAsync("ExistingRole");

            // Assert: CreateAsync should never be called.
            _roleManagerMock.Verify(r => r.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
        }

        [Fact]
        public async Task CreateRoleAsync_ThrowsException_WhenNameInvalid()
        {
            // Act + Assert: ensure invalid names raise ArgumentNullException.
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateRoleAsync(""));
        }

        [Fact]
        public async Task DeleteRoleAsync_Deletes_WhenRoleFound()
        {
            // Arrange: set up a role to delete.
            var role = new IdentityRole("TempRole");
            _roleManagerMock.Setup(r => r.FindByNameAsync("TempRole")).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.DeleteAsync(role)).ReturnsAsync(IdentityResult.Success);

            // Act
            await _service.DeleteRoleAsync("TempRole");

            // Assert
            _roleManagerMock.Verify(r => r.DeleteAsync(role), Times.Once);
        }

        [Fact]
        public async Task DeleteRoleAsync_DoesNothing_WhenRoleMissing()
        {
            // Arrange: return null to simulate missing role.
            _roleManagerMock
                .Setup(r => r.FindByNameAsync("GhostRole"))
                .ReturnsAsync((IdentityRole?)null);

            // Act
            await _service.DeleteRoleAsync("GhostRole");

            // Assert
            _roleManagerMock.Verify(r => r.DeleteAsync(It.IsAny<IdentityRole>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUserRolesAsync_AddsAndRemovesRolesCorrectly()
        {
            // Arrange: create user and current vs target roles.
            var user = new AppUser { Id = "U1", UserName = "user@cmcs.app" };
            var currentRoles = new List<string> { "Lecturer", "Reviewer" };
            var selectedRoles = new List<string> { "Lecturer", "ProgramCoordinator" };

            // Mock retrieval.
            _userManagerMock.Setup(u => u.FindByIdAsync("U1")).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(currentRoles);

            // Mock add/remove behaviors.
            _userManagerMock
                .Setup(u => u.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock
                .Setup(u => u.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _service.UpdateUserRolesAsync("U1", selectedRoles);

            // Assert: should add ProgramCoordinator and remove Reviewer.
            _userManagerMock.Verify(
                u =>
                    u.AddToRolesAsync(
                        user,
                        It.Is<IEnumerable<string>>(r => r.Contains("ProgramCoordinator"))
                    ),
                Times.Once
            );
            _userManagerMock.Verify(
                u =>
                    u.RemoveFromRolesAsync(
                        user,
                        It.Is<IEnumerable<string>>(r => r.Contains("Reviewer"))
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateUserRolesAsync_Throws_WhenUserNotFound()
        {
            // Arrange: return null to simulate missing user.
            _userManagerMock.Setup(u => u.FindByIdAsync("X1")).ReturnsAsync((AppUser?)null);

            // Act + Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateUserRolesAsync("X1", new[] { "Lecturer" })
            );
        }
    }

    /// <summary>
    /// Helper extension for mocking DbSet queries used in EF-based manager properties.
    /// This mimics EF's async enumeration behavior.
    /// </summary>
    internal static class MockDbSetExtensions
    {
        public static Mock<DbSet<T>> BuildMockDbSet<T>(this IQueryable<T> data)
            where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet
                .As<IQueryable<T>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => data.GetEnumerator());
            mockSet
                .As<IAsyncEnumerable<T>>()
                .Setup(d => d.GetAsyncEnumerator(default))
                .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
            return mockSet;
        }
    }

    /// <summary>
    /// Helper class for simulating async enumeration on mock DbSets.
    /// </summary>
    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());

        public T Current => _inner.Current;
    }
}
