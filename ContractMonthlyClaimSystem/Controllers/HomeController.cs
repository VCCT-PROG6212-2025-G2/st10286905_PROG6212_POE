using System.Diagnostics;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class HomeController(ILogger<HomeController> logger, IUserService userService)
        : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly IUserService _userService = userService;

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var user = _userService.GetUser(User.Identity?.Name);
                if (user != null)
                {
                    HttpContext.Session.SetString("username", user.UserName);
                    HttpContext.Session.SetInt32("uid", user.Id);
                    HttpContext.Session.SetString("FullName", $"{user.FirstName} {user.LastName}");
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                }
            );
        }
    }
}
