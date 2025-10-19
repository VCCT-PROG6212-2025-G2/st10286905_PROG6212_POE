// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68c17e75-6410-800b-922a-8487a7e06720
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageRolesController(IRoleService roleService, UserManager<AppUser> userManager)
        : Controller
    {
        private readonly IRoleService _roleService = roleService;
        private readonly UserManager<AppUser> _userManager = userManager;

        // Display all users with their roles, plus list of roles
        public async Task<IActionResult> Index()
        {
            var usersWithRoles = await _roleService.GetUsersWithRolesAsync();
            var model = usersWithRoles
                .Select(ur => new UserRolesViewModel
                {
                    UserId = ur.User.Id,
                    UserName = ur.User.UserName,
                    Roles = ur.Roles,
                })
                .ToList();

            ViewBag.AllRoles = await _roleService.GetRolesAsync();
            return View(model);
        }

        // Manage user roles (GET)
        public async Task<IActionResult> Manage(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var model = new ManageUserRolesViewModel { UserId = user.Id, UserName = user.UserName };

            var allRoles = await _roleService.GetRolesAsync();
            foreach (var role in allRoles)
            {
                if (role.Name == null)
                    continue;

                model.Roles.Add(
                    new RoleSelectionViewModel
                    {
                        RoleName = role.Name,
                        Selected = await _userManager.IsInRoleAsync(user, role.Name),
                    }
                );
            }

            return View(model);
        }

        // Manage user roles (POST)
        [HttpPost]
        public async Task<IActionResult> Manage(ManageUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound();

            var selectedRoles = model.Roles.Where(r => r.Selected).Select(r => r.RoleName);
            await _roleService.UpdateUserRolesAsync(user.Id, selectedRoles);

            return RedirectToAction(nameof(Index));
        }

        // Add new role
        [HttpPost]
        public async Task<IActionResult> AddRole(string roleName)
        {
            if (!string.IsNullOrWhiteSpace(roleName))
                await _roleService.CreateRoleAsync(roleName);
            
            return RedirectToAction(nameof(Index));
        }

        // Delete role
        [HttpPost]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            await _roleService.DeleteRoleAsync(roleName);

            return RedirectToAction(nameof(Index));
        }
    }
}
