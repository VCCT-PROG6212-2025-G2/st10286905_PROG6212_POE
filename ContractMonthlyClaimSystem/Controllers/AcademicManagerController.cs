using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "AcademicManager")]
    public class AcademicManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
