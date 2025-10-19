using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Tests.Services
{
    /// <summary>
    /// Unit tests for the ModuleService class.
    /// Each test uses an in-memory EF Core database to ensure isolation and reproducibility.
    /// This suite verifies that modules and lecturer-module associations are
    /// correctly created, retrieved, and removed.
    /// </summary>
    public class ModuleServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly ModuleService _service;

        public ModuleServiceTests()
        {
            // Create an in-memory EF Core database for each test instance.
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new ModuleService(_context);
        }

        [Fact]
        public async Task GetModulesAsync_ReturnsAllModules()
        {
            // Arrange: populate the database with modules.
            _context.Modules.AddRange(
                new Module { Name = "Programming 1A", Code = "PROG1234" },
                new Module { Name = "Programming 2B", Code = "PROG6212" }
            );
            await _context.SaveChangesAsync();

            // Act: call the service to retrieve all modules.
            var result = await _service.GetModulesAsync();

            // Assert: verify that both modules are returned.
            Assert.Equal(2, result.Count);
            Assert.Contains(result, m => m.Code == "PROG1234");
            Assert.Contains(result, m => m.Code == "PROG6212");
        }

        [Fact]
        public async Task GetModulesForLecturerAsync_ReturnsLinkedModules()
        {
            // Arrange: create a lecturer-module relationship.
            var lecturerId = "L1";
            var module = new Module
            {
                Id = 1,
                Name = "Cloud Development",
                Code = "CLDV6212",
            };
            _context.Modules.Add(module);
            _context.LecturerModules.Add(
                new LecturerModule
                {
                    LecturerUserId = lecturerId,
                    ModuleId = module.Id,
                    Module = module,
                }
            );
            await _context.SaveChangesAsync();

            // Act: retrieve modules for the lecturer.
            var result = await _service.GetModulesForLecturerAsync(lecturerId);

            // Assert: ensure only one module is linked and returned.
            Assert.Single(result);
            Assert.Equal("CLDV6212", result[0].Code);
        }

        [Fact]
        public async Task AddModuleAsync_SavesValidModule()
        {
            // Arrange: prepare a valid module.
            var module = new Module { Name = "Databases 2", Code = "DBAS6212" };

            // Act: add the module using the service.
            await _service.AddModuleAsync(module);

            // Assert: confirm that it was saved.
            Assert.Single(_context.Modules);
            Assert.Equal("DBAS6212", _context.Modules.First().Code);
        }

        [Fact]
        public async Task AddModuleAsync_IgnoresInvalidModules()
        {
            // Arrange: create an invalid module with missing data.
            var invalidModule = new Module { Name = "", Code = "" };

            // Act: attempt to add invalid module.
            await _service.AddModuleAsync(invalidModule);

            // Assert: ensure no records were added to the database.
            Assert.Empty(_context.Modules);
        }

        [Fact]
        public async Task AddModuleAsync_ByNameAndCode_AddsModuleSuccessfully()
        {
            // Act: directly call the overload with name and code.
            await _service.AddModuleAsync("Networking 1", "NETW5111");

            // Assert: verify it was saved properly.
            var saved = _context.Modules.FirstOrDefault();
            Assert.NotNull(saved);
            Assert.Equal("NETW5111", saved!.Code);
        }

        [Fact]
        public async Task RemoveModuleAsync_DeletesExistingModule()
        {
            // Arrange: add a module to be removed later.
            var module = new Module { Name = "Operating Systems", Code = "OPSY6212" };
            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            // Act: remove it using the service.
            await _service.RemoveModuleAsync(module.Id);

            // Assert: module should no longer exist.
            Assert.Empty(_context.Modules);
        }

        [Fact]
        public async Task RemoveModuleAsync_DoesNothing_IfModuleNotFound()
        {
            // Arrange: database is empty, module ID doesn’t exist.

            // Act: attempt removal.
            await _service.RemoveModuleAsync(999);

            // Assert: nothing should happen (no exceptions, no changes).
            Assert.Empty(_context.Modules);
        }

        [Fact]
        public async Task AddLecturerModuleAsync_AddsAssociation_WhenNotExisting()
        {
            // Arrange: prepare a lecturer and module.
            var lecturerId = "L1";
            var module = new Module
            {
                Id = 10,
                Name = "Advanced Programming",
                Code = "PROG7312",
            };
            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            // Act: add lecturer-module link.
            await _service.AddLecturerModuleAsync(lecturerId, module.Id);

            // Assert: ensure link was created.
            var link = _context.LecturerModules.FirstOrDefault();
            Assert.NotNull(link);
            Assert.Equal(lecturerId, link!.LecturerUserId);
            Assert.Equal(module.Id, link.ModuleId);
        }

        [Fact]
        public async Task AddLecturerModuleAsync_DoesNotDuplicateExistingLink()
        {
            // Arrange: add a link that already exists.
            var lecturerId = "L1";
            var module = new Module
            {
                Id = 11,
                Name = "Web Development",
                Code = "WEDE6212",
            };
            _context.Modules.Add(module);
            _context.LecturerModules.Add(
                new LecturerModule { LecturerUserId = lecturerId, ModuleId = module.Id }
            );
            await _context.SaveChangesAsync();

            // Act: try to add the same association again.
            await _service.AddLecturerModuleAsync(lecturerId, module.Id);

            // Assert: still only one link should exist.
            Assert.Single(_context.LecturerModules);
        }

        [Fact]
        public async Task RemoveLecturerModuleAsync_RemovesExistingLink()
        {
            // Arrange: create and save a lecturer-module link.
            var lecturerId = "L1";
            var module = new Module
            {
                Id = 12,
                Name = "Security Fundamentals",
                Code = "SECU5111",
            };
            _context.Modules.Add(module);
            var link = new LecturerModule { LecturerUserId = lecturerId, ModuleId = module.Id };
            _context.LecturerModules.Add(link);
            await _context.SaveChangesAsync();

            // Act: remove the link.
            await _service.RemoveLecturerModuleAsync(lecturerId, module.Id);

            // Assert: ensure it has been deleted.
            Assert.Empty(_context.LecturerModules);
        }

        [Fact]
        public async Task RemoveLecturerModuleAsync_DoesNothing_IfLinkNotFound()
        {
            // Arrange: no links exist in DB.
            var lecturerId = "L1";
            var moduleId = 999;

            // Act: attempt to remove nonexistent link.
            await _service.RemoveLecturerModuleAsync(lecturerId, moduleId);

            // Assert: still no links in database.
            Assert.Empty(_context.LecturerModules);
        }
    }
}
