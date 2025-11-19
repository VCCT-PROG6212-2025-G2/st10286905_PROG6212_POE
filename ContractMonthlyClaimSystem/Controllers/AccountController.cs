using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class AccountController(IUserService userService) : Controller
    {
        private readonly IUserService _userService = userService;

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null) =>
            View(new LoginViewModel { ReturnUrl = returnUrl });

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userService.AuthenticateAsync(model.UserName, model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View(model);
            }

            var principal = _userService.BuildClaimsPrincipal(user);
            await HttpContext.SignInAsync(principal);

            TempData["Success"] = $"Welcome back, {(user.FirstName + user.LastName) ?? user.UserName}!";

            // Redirect to ReturnUrl if provided, otherwise Home
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userService.RegisterAsync(
                username: model.UserName,
                password: model.Password,
                email: model.Email,
                firstName: model.FirstName,
                lastName: model.LastName,
                roleName: null
            );

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Registration failed. UserName or email may already be in use."
                );
                return View(model);
            }

            var principal = _userService.BuildClaimsPrincipal(user);
            await HttpContext.SignInAsync(principal);

            TempData["Success"] =
                $"Welcome, {(user.FirstName + user.LastName) ?? user.UserName}! Your account has been created.";
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            TempData["Success"] = "You have been signed out successfully.";
            return RedirectToAction(nameof(Login));
        }

        public IActionResult AccessDenied() => View();

        [Authorize]
        public async Task<IActionResult> Manage()
        {
            var user = await _userService.GetUserAsync(User.Identity?.Name);
            if (user == null)
                return RedirectToAction(nameof(Login));

            return View(
                new ManageAccountViewModel
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                }
            );
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageAccountViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userService.GetUserAsync(User.Identity?.Name);
            if (user == null)
                return RedirectToAction(nameof(Login));

            var updatedUser = await _userService.UpdateDetailsAsync(
                model.UserName,
                model.Password,
                model.Email,
                model.FirstName,
                model.LastName
            );

            if (updatedUser == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Update failed. Did you enter the correct password?"
                );
                return View(model);
            }

            TempData["Success"] = "Your details have been updated successfully.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Manage));

            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
                return RedirectToAction(nameof(Login));

            var user = await _userService.ChangePasswordAsync(
                username,
                model.OldPassword,
                model.NewPassword
            );
            if (user == null)
                TempData["Error"] =
                    "Failed to change password. Did you enter the correct password?";
            else
                TempData["Success"] = "Your password has been updated successfully.";

            return RedirectToAction(nameof(Manage));
        }

        // AI Disclosure: ChatGPT assisted here. Link: https://chatgpt.com/s/t_690b54777cec819198d75f5644cfd5fc
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
                return RedirectToAction(nameof(Login));

            try
            {
                var user = await _userService.GetUserAsync(username);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Manage));
                }

                var deleted = await _userService.DeleteUserAsync(username);
                if (!deleted)
                {
                    TempData["Error"] = "Failed to delete account.";
                    return RedirectToAction(nameof(Manage));
                }

                await HttpContext.SignOutAsync();

                TempData["Success"] = "Your account has been successfully deleted";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "An error occurred while deleting your account: " + ex.ToString();
                return RedirectToAction(nameof(Manage));
            }
        }
    }
}
