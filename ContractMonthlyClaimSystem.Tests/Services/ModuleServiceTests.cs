// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68f5452c-2788-800b-bbbc-175029690cfd

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
    /// Unit tests for ModuleService.
    /// Uses an isolated in-memory EF database per test.
    /// </summary>
    public class ModuleServiceTests
    {
        private readonly AppDbContext _context;
        private readonly ModuleService _service;

        public ModuleServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _service = new ModuleService(_context);
        }

        // -------------------------------------------------------
        // MODULE CRUD TESTS
        // -------------------------------------------------------

        [Fact]
        public async Task GetModulesAsync_ReturnsAllModules()
        {
            _context.Modules.AddRange(
                new Module { Name = "Programming 1A", Code = "PROG1234" },
                new Module { Name = "Programming 2B", Code = "PROG6212" }
            );
            await _context.SaveChangesAsync();

            var result = await _service.GetModulesAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, m => m.Code == "PROG1234");
            Assert.Contains(result, m => m.Code == "PROG6212");
        }

        [Fact]
        public async Task GetModulesForLecturerAsync_ReturnsLinkedModules()
        {
            var lecturerId = 1;
            var module = new Module { Id = 1, Name = "Cloud Development", Code = "CLDV6212" };

            _context.Modules.Add(module);
            _context.LecturerModules.Add(new LecturerModule
            {
                LecturerUserId = lecturerId,
                ModuleId = module.Id,
                Module = module,
                HourlyRate = 500
            });
            await _context.SaveChangesAsync();

            var result = await _service.GetModulesForLecturerAsync(lecturerId);

            Assert.Single(result);
            Assert.Equal("CLDV6212", result[0].Code);
        }

        [Fact]
        public async Task AddModuleAsync_SavesValidModule()
        {
            var module = new Module { Name = "Databases 2", Code = "DBAS6212" };

            await _service.AddModuleAsync(module);

            Assert.Single(_context.Modules);
            Assert.Equal("DBAS6212", _context.Modules.First().Code);
        }

        [Fact]
        public async Task AddModuleAsync_IgnoresInvalidModules()
        {
            var invalidModule = new Module { Name = "", Code = "" };

            await _service.AddModuleAsync(invalidModule);

            Assert.Empty(_context.Modules);
        }

        [Fact]
        public async Task AddModuleAsync_ByNameAndCode_AddsModuleSuccessfully()
        {
            await _service.AddModuleAsync("Networking 1", "NETW5111");

            var saved = _context.Modules.FirstOrDefault();
            Assert.NotNull(saved);
            Assert.Equal("NETW5111", saved!.Code);
        }

        [Fact]
        public async Task RemoveModuleAsync_DeletesExistingModule()
        {
            var module = new Module { Name = "Operating Systems", Code = "OPSY6212" };
            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            await _service.RemoveModuleAsync(module.Id);

            Assert.Empty(_context.Modules);
        }

        [Fact]
        public async Task RemoveModuleAsync_DoesNothing_IfModuleNotFound()
        {
            await _service.RemoveModuleAsync(999);

            Assert.Empty(_context.Modules);
        }

        // -------------------------------------------------------
        // LECTURER-MODULE ASSOCIATION TESTS
        // -------------------------------------------------------

        [Fact]
        public async Task AddLecturerModuleAsync_AddsAssociation_WhenNotExisting()
        {
            var lecturerId = 1;
            var module = new Module { Id = 10, Name = "Advanced Programming", Code = "PROG7312" };

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            await _service.AddLecturerModuleAsync(lecturerId, module.Id, 400);

            var link = _context.LecturerModules.FirstOrDefault();
            Assert.NotNull(link);
            Assert.Equal(lecturerId, link!.LecturerUserId);
            Assert.Equal(module.Id, link.ModuleId);
            Assert.Equal(400, link.HourlyRate);
        }

        [Fact]
        public async Task AddLecturerModuleAsync_DoesNotDuplicateExistingLink()
        {
            var lecturerId = 1;
            var module = new Module { Id = 11, Name = "Web Development", Code = "WEDE6212" };

            _context.Modules.Add(module);
            _context.LecturerModules.Add(new LecturerModule
            {
                LecturerUserId = lecturerId,
                ModuleId = module.Id,
                HourlyRate = 300
            });
            await _context.SaveChangesAsync();

            await _service.AddLecturerModuleAsync(lecturerId, module.Id, 999); // should NOT overwrite

            Assert.Single(_context.LecturerModules);
            Assert.Equal(300, _context.LecturerModules.First().HourlyRate);
        }

        [Fact]
        public async Task GetLecturerModulesAsync_ReturnsModulesWithRates()
        {
            var lecturerId = 1;

            var module = new Module { Id = 22, Name = "AI Fundamentals", Code = "AIFU5111" };
            _context.Modules.Add(module);

            _context.LecturerModules.Add(new LecturerModule
            {
                LecturerUserId = lecturerId,
                ModuleId = module.Id,
                Module = module,
                HourlyRate = 750
            });

            await _context.SaveChangesAsync();

            var result = await _service.GetLecturerModulesAsync(lecturerId);

            Assert.Single(result);
            Assert.Equal(22, result[0].ModuleId);
            Assert.Equal(750, result[0].HourlyRate);
            Assert.NotNull(result[0].Module);
        }

        [Fact]
        public async Task RemoveLecturerModuleAsync_RemovesExistingLink()
        {
            var lecturerId = 1;
            var module = new Module { Id = 12, Name = "Security Fundamentals", Code = "SECU5111" };

            _context.Modules.Add(module);

            var link = new LecturerModule
            {
                LecturerUserId = lecturerId,
                ModuleId = module.Id,
                HourlyRate = 500
            };

            _context.LecturerModules.Add(link);
            await _context.SaveChangesAsync();

            await _service.RemoveLecturerModuleAsync(lecturerId, module.Id);

            Assert.Empty(_context.LecturerModules);
        }

        [Fact]
        public async Task RemoveLecturerModuleAsync_DoesNothing_IfLinkNotFound()
        {
            await _service.RemoveLecturerModuleAsync(1, 999);

            Assert.Empty(_context.LecturerModules);
        }

        // -------------------------------------------------------
        // HOURLY RATE UPDATE TESTS
        // -------------------------------------------------------

        [Fact]
        public async Task UpdateLecturerModuleHourlyRate_UpdatesRate_WhenLinkExists()
        {
            var lecturerId = 1;
            var module = new Module { Id = 30, Name = "Maths", Code = "MATH1111" };

            _context.Modules.Add(module);
            _context.LecturerModules.Add(new LecturerModule
            {
                LecturerUserId = lecturerId,
                ModuleId = module.Id,
                HourlyRate = 200
            });
            await _context.SaveChangesAsync();

            await _service.UpdateLecturerModuleHourlyRate(lecturerId, module.Id, 600);

            var link = _context.LecturerModules.First();
            Assert.Equal(600, link.HourlyRate);
        }

        [Fact]
        public async Task UpdateLecturerModuleHourlyRate_DoesNothing_IfLinkNotFound()
        {
            await _service.UpdateLecturerModuleHourlyRate(1, 999, 777);

            Assert.Empty(_context.LecturerModules);
        }
    }
}
