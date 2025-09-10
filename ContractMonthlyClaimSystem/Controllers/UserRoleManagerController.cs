// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68c17e75-6410-800b-922a-8487a7e06720
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserRoleManagerController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRoleManagerController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Display all users with their roles, plus list of roles
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var model = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new UserRolesViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Roles = roles
                });
            }

            ViewBag.AllRoles = _roleManager.Roles.ToList();
            return View(model);
        }

        // Manage user roles (GET)
        public async Task<IActionResult> Manage(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var model = new ManageUserRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName
            };

            foreach (var role in _roleManager.Roles)
            {
                model.Roles.Add(new RoleSelectionViewModel
                {
                    RoleName = role.Name,
                    Selected = await _userManager.IsInRoleAsync(user, role.Name)
                });
            }

            return View(model);
        }

        // Manage user roles (POST)
        [HttpPost]
        public async Task<IActionResult> Manage(ManageUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Roles.Where(r => r.Selected).Select(r => r.RoleName);

            // add newly selected roles
            var addResult = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            // remove unchecked roles
            var removeResult = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            return RedirectToAction(nameof(Index));
        }

        // Add new role
        [HttpPost]
        public async Task<IActionResult> AddRole(string roleName)
        {
            if (!string.IsNullOrWhiteSpace(roleName) && !await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
            return RedirectToAction(nameof(Index));
        }

        // Delete role
        [HttpPost]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                await _roleManager.DeleteAsync(role);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
