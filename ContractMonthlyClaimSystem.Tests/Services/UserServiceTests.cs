// AI Disclosure: ChatGPT assisted creating this. Link: https://chatgpt.com/s/t_6920669a02548191bc198b6f0aa38242

using System.Security.Claims;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models.Auth;
using ContractMonthlyClaimSystem.Services;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ContractMonthlyClaimSystem.Tests.Services
{
    public class UserServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IPasswordHasher> _passwordMock;
        private readonly UserService _service;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _passwordMock = new Mock<IPasswordHasher>();
            _service = new UserService(_context, _passwordMock.Object);
        }

        // ============================================================
        // GET USER (SYNC)
        // ============================================================
        [Fact]
        public void GetUser_ById_ReturnsUser()
        {
            var user = new AppUser { Id = 44, UserName = "id@test.com" };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = _service.GetUser(44);

            Assert.NotNull(result);
            Assert.Equal("id@test.com", result!.UserName);
        }

        [Fact]
        public void GetUser_ByUsername_ReturnsUser()
        {
            var user = new AppUser { UserName = "sync@test.com" };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = _service.GetUser("sync@test.com");

            Assert.NotNull(result);
            Assert.Equal("sync@test.com", result!.UserName);
        }

        [Fact]
        public void GetUser_ByClaimsPrincipal_ReturnsUser()
        {
            var user = new AppUser { Id = 5, UserName = "claim@test.com" };
            _context.Users.Add(user);
            _context.SaveChanges();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "5"),
                new Claim(ClaimTypes.Name, "claim@test.com"),
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            var result = _service.GetUser(principal);

            Assert.NotNull(result);
            Assert.Equal("claim@test.com", result!.UserName);
        }

        // ============================================================
        // GET USER (ASYNC)
        // ============================================================
        [Fact]
        public async Task GetUserAsync_ById_ReturnsUser()
        {
            var user = new AppUser { Id = 10, UserName = "test@test.com" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _service.GetUserAsync(10);

            Assert.NotNull(result);
            Assert.Equal("test@test.com", result!.UserName);
        }

        [Fact]
        public async Task GetUserAsync_ByUsername_ReturnsUser()
        {
            var user = new AppUser { UserName = "test@test.com" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _service.GetUserAsync("test@test.com");

            Assert.NotNull(result);
            Assert.Equal("test@test.com", result!.UserName);
        }

        [Fact]
        public async Task GetUserAsync_ByClaimsPrincipal_ReturnsUser()
        {
            var user = new AppUser { Id = 6, UserName = "async@test.com" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "6") };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            var result = await _service.GetUserAsync(principal);

            Assert.NotNull(result);
            Assert.Equal("async@test.com", result!.UserName);
        }

        // ============================================================
        // GET ALL USERS
        // ============================================================
        [Fact]
        public async Task GetAllUsersAsync_ReturnsAllUsers()
        {
            _context.Users.Add(new AppUser { UserName = "a" });
            _context.Users.Add(new AppUser { UserName = "b" });
            await _context.SaveChangesAsync();

            var result = await _service.GetAllUsersAsync();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllUsersInRoleAsync_ReturnsOnlyMatchingUsers()
        {
            var admin = new AppRole { Name = "Admin" };
            var lecturer = new AppRole { Name = "Lecturer" };

            var u1 = new AppUser { UserName = "one" };
            var u2 = new AppUser { UserName = "two" };

            u1.UserRoles.Add(new AppUserRole { Role = admin });
            u2.UserRoles.Add(new AppUserRole { Role = lecturer });

            _context.Users.AddRange(u1, u2);
            await _context.SaveChangesAsync();

            var admins = await _service.GetAllUsersInRoleAsync("Admin");

            Assert.Single(admins);
            Assert.Equal("one", admins[0].UserName);
        }

        // ============================================================
        // IS USER IN ROLE
        // ============================================================
        [Fact]
        public async Task IsUserInRoleAsync_ReturnsTrue_WhenUserInRole()
        {
            var role = new AppRole { Name = "Lecturer" };
            var user = new AppUser { Id = 99, UserName = "x" };

            user.UserRoles.Add(new AppUserRole { User = user, Role = role });

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _service.IsUserInRoleAsync(99, "Lecturer");

            Assert.True(result);
        }

        // ============================================================
        // AUTHENTICATION
        // ============================================================
        [Fact]
        public async Task AuthenticateAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            var result = await _service.AuthenticateAsync("missing@test.com", "123");
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_ReturnsUser_WhenPasswordCorrect()
        {
            var user = new AppUser
            {
                UserName = "u@test.com",
                PasswordHash = "HASH",
                PasswordSalt = "SALT",
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _passwordMock.Setup(p => p.Verify("123", "HASH", "SALT")).Returns(true);

            var result = await _service.AuthenticateAsync("u@test.com", "123");

            Assert.NotNull(result);
            Assert.Equal("u@test.com", result!.UserName);
        }

        // ============================================================
        // REGISTER (ALL OVERLOADS)
        // ============================================================
        [Fact]
        public async Task RegisterAsync_CreatesUser()
        {
            _passwordMock.Setup(p => p.HashPassword("123")).Returns(("HASH", "SALT"));

            var result = await _service.RegisterAsync("new@test.com", "123");

            Assert.NotNull(result);
            Assert.Equal("new@test.com", result!.UserName);
            Assert.Equal(1, _context.Users.Count());
        }

        [Fact]
        public async Task RegisterAsync_WithRole_CreatesRoleIfMissing()
        {
            _passwordMock.Setup(p => p.HashPassword("123")).Returns(("H", "S"));

            var result = await _service.RegisterAsync("x@test.com", "123", roleName: "Lecturer");

            Assert.NotNull(result);
            Assert.Single(result!.UserRoles);
            Assert.Equal("Lecturer", result.UserRoles.First().Role.Name);
        }

        [Fact]
        public async Task RegisterAsync_WithMultipleRoles_CreatesAll()
        {
            _passwordMock.Setup(p => p.HashPassword("123")).Returns(("H", "S"));

            var result = await _service.RegisterAsync(
                "multi@test.com",
                "123",
                roleNames: new() { "Lecturer", "Admin" }
            );

            Assert.NotNull(result);
            Assert.Equal(2, result!.UserRoles.Count);
        }

        [Fact]
        public async Task RegisterAsync_WithoutRoles_SetsAllProperties()
        {
            _passwordMock.Setup(p => p.HashPassword("pw")).Returns(("H", "S"));

            var result = await _service.RegisterAsync(
                "new@test.com",
                "pw",
                email: "mail@test.com",
                firstName: "Tom",
                lastName: "Smith",
                (string?)null
            );

            Assert.NotNull(result);
            Assert.Equal("mail@test.com", result!.Email);
            Assert.Equal("Tom", result.FirstName);
            Assert.Equal("Smith", result.LastName);
        }

        [Fact]
        public async Task RegisterAsync_WithRole_SetsPropertiesAndRole()
        {
            _passwordMock.Setup(p => p.HashPassword("pw")).Returns(("H", "S"));

            var result = await _service.RegisterAsync(
                "role@test.com",
                "pw",
                email: "x@test.com",
                firstName: "A",
                lastName: "B",
                roleName: "Lecturer"
            );

            Assert.NotNull(result);
            Assert.Equal("A", result!.FirstName);
            Assert.Single(result.UserRoles);
            Assert.Equal("Lecturer", result.UserRoles.First().Role.Name);
        }

        // ============================================================
        // ADD ROLE
        // ============================================================
        [Fact]
        public async Task AddUserToRoleAsync_AddsRole()
        {
            var role = new AppRole { Id = 2, Name = "Lecturer" };
            var user = new AppUser { Id = 1, UserName = "x@test.com" };

            _context.Roles.Add(role);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.AddUserToRoleAsync(1, "Lecturer");

            var updated = await _service.GetUserAsync(1);
            Assert.Single(updated!.UserRoles);
        }

        [Fact]
        public async Task AddUserToRolesAsync_AddsEachRole()
        {
            var user = new AppUser { Id = 7, UserName = "roles@test.com" };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.AddUserToRolesAsync(7, new[] { "Lecturer", "HR" });

            var updated = await _service.GetUserAsync(7);

            Assert.Equal(2, updated!.UserRoles.Count);
        }

        // ============================================================
        // REMOVE ROLE
        // ============================================================
        [Fact]
        public async Task RemoveUserFromRoleAsync_RemovesRole()
        {
            var role = new AppRole { Id = 1, Name = "Admin" };
            var user = new AppUser
            {
                Id = 5,
                UserName = "u",
                UserRoles = { new AppUserRole { Role = role } },
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.RemoveUserFromRoleAsync(5, "Admin");

            var updated = await _service.GetUserAsync(5);
            Assert.Empty(updated!.UserRoles);
        }

        [Fact]
        public async Task RemoveUserFromRolesAsync_RemovesAllRoles()
        {
            var r1 = new AppRole { Name = "Admin" };
            var r2 = new AppRole { Name = "Lecturer" };
            var user = new AppUser
            {
                Id = 22,
                UserName = "u22",
                UserRoles =
                {
                    new AppUserRole { Role = r1 },
                    new AppUserRole { Role = r2 },
                },
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.RemoveUserFromRolesAsync(22, new[] { "Admin", "Lecturer" });

            var updated = await _service.GetUserAsync(22);
            Assert.Empty(updated!.UserRoles);
        }

        [Fact]
        public async Task RemoveUserFromRolesAsync_RemovesOnlyMatchingRoles()
        {
            var r1 = new AppRole { Name = "Admin" };
            var r2 = new AppRole { Name = "Lecturer" };
            var r3 = new AppRole { Name = "HR" };

            var user = new AppUser
            {
                Id = 31,
                UserName = "u31",
                UserRoles =
                {
                    new AppUserRole { Role = r1 },
                    new AppUserRole { Role = r2 },
                    new AppUserRole { Role = r3 },
                },
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.RemoveUserFromRolesAsync(31, new[] { "Admin", "HR" });

            var updated = await _service.GetUserAsync(31);

            Assert.Single(updated!.UserRoles);
            Assert.Equal("Lecturer", updated.UserRoles.First().Role.Name);
        }

        // ============================================================
        // DELETE USER
        // ============================================================
        [Fact]
        public async Task DeleteUserAsync_ById_RemovesUser()
        {
            var user = new AppUser { Id = 77, UserName = "u77" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _service.DeleteUserAsync(77);

            Assert.True(result);
            Assert.Empty(_context.Users);
        }

        [Fact]
        public async Task DeleteUserAsync_ByUsername_RemovesUser()
        {
            var user = new AppUser { Id = 88, UserName = "u88" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _service.DeleteUserAsync("u88");

            Assert.True(result);
            Assert.Empty(_context.Users);
        }

        // ============================================================
        // CHANGE PASSWORD
        // ============================================================
        [Fact]
        public async Task ChangePasswordAsync_UpdatesPassword()
        {
            _passwordMock.Setup(p => p.HashPassword("new")).Returns(("NEW", "SALT"));
            _passwordMock.Setup(p => p.Verify("old", "OLD", "SALT")).Returns(true);

            var user = new AppUser
            {
                UserName = "u",
                PasswordHash = "OLD",
                PasswordSalt = "SALT",
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _service.ChangePasswordAsync("u", "old", "new");

            Assert.NotNull(result);
            Assert.Equal("NEW", result!.PasswordHash);
        }

        // ============================================================
        // UPDATE DETAILS
        // ============================================================
        [Fact]
        public async Task UpdateDetailsAsync_UpdatesProperties()
        {
            _passwordMock.Setup(p => p.Verify("pw", "H", "S")).Returns(true);

            var user = new AppUser
            {
                UserName = "u",
                PasswordHash = "H",
                PasswordSalt = "S",
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.UpdateDetailsAsync("u", "pw", email: "new@mail.com", firstName: "A");

            var updated = await _service.GetUserAsync("u");

            Assert.Equal("new@mail.com", updated!.Email);
            Assert.Equal("A", updated.FirstName);
        }

        // ============================================================
        // CLAIMS PRINCIPAL
        // ============================================================
        [Fact]
        public void BuildClaimsPrincipal_ReturnsCorrectClaims()
        {
            var user = new AppUser
            {
                Id = 100,
                UserName = "x",
                UserRoles =
                {
                    new AppUserRole { Role = new AppRole { Name = "Admin" } },
                    new AppUserRole { Role = new AppRole { Name = "Lecturer" } },
                },
            };

            var principal = _service.BuildClaimsPrincipal(user);

            var claims = principal.Claims.ToList();

            Assert.Contains(claims, c => c.Type == ClaimTypes.Name && c.Value == "x");
            Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "100");
            Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
            Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "Lecturer");
        }
    }
}
