// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68c17e75-6410-800b-922a-8487a7e06720
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class ManageUserRolesController(IRoleService roleService, IUserService userService)
        : Controller
    {
        private readonly IRoleService _roleService = roleService;
        private readonly IUserService _userService = userService;

        // Display all users with their roles, plus list of roles
        public async Task<IActionResult> Index()
        {
            var usersWithRoles = await _roleService.GetUsersWithRolesAsync();
            var allRoles = await _roleService.GetRolesAsync();
            var model = new ManageUserRolesIndexViewModel
            {
                Users =
                [
                    .. usersWithRoles.Select(ur => new UserRolesViewModel
                    {
                        UserId = ur.User.Id,
                        UserName = ur.User.UserName,
                        Roles = ur.Roles,
                    }),
                ],
                RoleSelectList = allRoles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name,
                    Selected = r.Name == "Lecturer",
                }),
            };

            ViewBag.AllRoles = allRoles;
            return View(model);
        }

        // Manage user roles (GET)
        public async Task<IActionResult> Manage(int userId)
        {
            var user = await _userService.GetUserAsync(userId);
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
                        Selected = await _userService.IsUserInRoleAsync(userId, role.Name),
                    }
                );
            }

            return View(model);
        }

        // Manage user roles (POST)
        [HttpPost]
        public async Task<IActionResult> Manage(ManageUserRolesViewModel model)
        {
            var user = await _userService.GetUserAsync(model.UserId);
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

        // Create user with role
        [HttpPost]
        public async Task<IActionResult> CreateUser(ManageUserRolesIndexViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.RegisterAsync(
                    model.CreateUser.UserName,
                    model.CreateUser.Password,
                    model.CreateUser.Email,
                    model.CreateUser.FirstName,
                    model.CreateUser.LastName,
                    model.CreateUser.Role
                );

                if (user != null)
                    TempData["Success"] =
                        $"Successfully created user: {user.UserName} with role: {model.CreateUser.Role}.";
                else
                    TempData["Error"] = "Error creating user: username is already taken.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
