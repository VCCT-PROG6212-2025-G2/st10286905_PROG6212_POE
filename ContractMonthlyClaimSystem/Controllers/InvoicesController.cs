using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class InvoicesController(IHumanResourcesService hrService) : Controller
    {
        private readonly IHumanResourcesService _hrService = hrService;

        public async Task<IActionResult> Index()
        {
            var invoices = await _hrService.GetProcessedClaimInvoicesAsync();

            var vm = new InvoicesIndexViewModel
            {
                ApprovedClaims =
                [
                    .. (await _hrService.GetApprovedClaimsAsync()).Select(
                        c => new ApprovedClaimRowViewModel
                        {
                            Id = c.Id,
                            LecturerName = $"{c.LecturerUser.FirstName} {c.LecturerUser.LastName}",
                            ModuleName = c.Module.Name,
                            PaymentAmount = c.HourlyRate * c.HoursWorked,
                            ProgramCoordinatorDecision = c.ProgramCoordinatorDecision,
                            AcademicManagerDecision = c.AcademicManagerDecision,
                            ClaimStatus = c.ClaimStatus,
                            InvoiceFileName = invoices
                                .FirstOrDefault(i => i.ClaimId == c.Id)
                                ?.UploadedFile?.FileName,
                        }
                    ),
                ],
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> RunAutoReview()
        {
            var reviewed = await _hrService.AutoReviewClaimsForReviewersAsync();
            if (reviewed > 0)
                TempData["Success"] =
                    $"Successfully performed {reviewed} reviews of claims on behalf of reviewer users, applying all their auto review rules.";
            else
                TempData["Error"] = "No auto review rules apply to any pending claims.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ProcessAllInvoices()
        {
            var processed = await _hrService.ProcessApprovedClaimInvoicesAsync();
            if (processed > 0)
                TempData["Success"] =
                    $"Successfully processed {processed} approved claims to generate invoice pdf documents.";
            else
                TempData["Error"] =
                    "There are no new approved claims that need invoices generated.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ProcessInvoice(int claimId)
        {
            var res = await _hrService.GetClaimInvoicePdfAsync(claimId);
            if (res == null)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadInvoice(int claimId)
        {
            var res = await _hrService.GetClaimInvoicePdfAsync(claimId);
            if (res == null)
                return NotFound();

            return File(res.Value.FileStream, res.Value.ContentType, res.Value.FileName);
        }
    }
}
