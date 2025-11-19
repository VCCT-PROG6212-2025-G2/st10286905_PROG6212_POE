using System.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "AcademicManager,ProgramCoordinator")]
    public class AutoReviewRulesController(
        IReviewerClaimService reviewerClaimService,
        IUserService userService
    ) : Controller
    {
        private readonly IReviewerClaimService _reviewerClaimService = reviewerClaimService;
        private readonly IUserService _userService = userService;

        public async Task<IActionResult> Index()
        {
            var user = await _userService.GetUserAsync(User.Identity?.Name);

            var rules = (await _reviewerClaimService.GetAutoReviewRulesForUserAsync(user!.Id))
                .Select(r => new AutoReviewRuleViewModel
                {
                    Id = r.Id,
                    Priority = r.Priority,
                    AutoDecision = r.AutoDecision,
                    ComparisonVar = r.ComparisonVar,
                    ComparisonOp = r.ComparisonOp,
                    ComparisonValue = r.ComparisonValue,
                    AutoComment = r.AutoComment,
                })
                .OrderByDescending(r => r.Priority)
                .ToList();

            var vm = new AutoReviewRulesIndexViewModel { Rules = rules };

            ViewBag.Decisions = Enum.GetValues<ClaimDecision>()
                .Where(x =>
                    User.IsInRole("ProgramCoordinator") && User.IsInRole("AcademicManager") // Either both roles
                    || ( // Or one role with appropriate decisions
                        User.IsInRole("ProgramCoordinator")
                            ? x != ClaimDecision.APPROVED
                            : !User.IsInRole("AcademicManager") || x != ClaimDecision.VERIFIED
                    )
                )
                .Select(x => new SelectListItem { Text = x.ToString(), Value = x.ToString() });
            ViewBag.ComparisonVars = Enum.GetValues<AutoReviewComparisonVar>()
                .Select(x => new SelectListItem { Text = x.ToString(), Value = x.ToString() });
            ViewBag.ComparisonOps = Enum.GetValues<AutoReviewComparisonOp>()
                .Select(x => new SelectListItem { Text = x.ToString(), Value = x.ToString() });

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AddRule(AutoReviewRulesIndexViewModel model)
        {
            var user = await _userService.GetUserAsync(User.Identity?.Name);
            model.NewRule.ReviewerId = user!.Id;
            await _reviewerClaimService.AddAutoReviewRuleAsync(model.NewRule);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveRule(int ruleId)
        {
            var user = await _userService.GetUserAsync(User.Identity?.Name);
            await _reviewerClaimService.RemoveAutoReviewRuleAsync(ruleId, user!.Id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRulePriority(int ruleId, int priority)
        {
            var user = await _userService.GetUserAsync(User.Identity?.Name);
            var rule = await _reviewerClaimService.GetAutoReviewRule(ruleId);
            if (rule == null)
                return NotFound();
            if (rule.ReviewerId != user!.Id)
            {
                TempData["Error"] = "Error: The AutoReviewRule is associated with a different user";
                return RedirectToAction("Login", "Account");
            }

            rule.Priority = priority;
            await _reviewerClaimService.UpdateAutoReviewRuleAsync(ruleId, user.Id, rule);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> IncreaseRulePriority(int ruleId)
        {
            var user = await _userService.GetUserAsync(User.Identity?.Name);
            var rule = await _reviewerClaimService.GetAutoReviewRule(ruleId);
            if (rule == null)
                return NotFound();
            if (rule.ReviewerId != user!.Id)
            {
                TempData["Error"] = "Error: The AutoReviewRule is associated with a different user";
                return RedirectToAction("Login", "Account");
            }

            rule.Priority++;
            await _reviewerClaimService.UpdateAutoReviewRuleAsync(ruleId, user.Id, rule);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DecreaseRulePriority(int ruleId)
        {
            var user = await _userService.GetUserAsync(User.Identity?.Name);
            var rule = await _reviewerClaimService.GetAutoReviewRule(ruleId);
            if (rule == null)
                return NotFound();
            if (rule.ReviewerId != user!.Id)
            {
                TempData["Error"] = "Error: The AutoReviewRule is associated with a different user";
                return RedirectToAction("Login", "Account");
            }

            rule.Priority--;
            await _reviewerClaimService.UpdateAutoReviewRuleAsync(ruleId, user.Id, rule);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditRule(int ruleId)
        {
            var user = await _userService.GetUserAsync(User.Identity?.Name);
            var rule = await _reviewerClaimService.GetAutoReviewRule(ruleId);
            if (rule == null)
                return NotFound();
            if (rule.ReviewerId != user!.Id)
            {
                TempData["Error"] = "Error: The AutoReviewRule is associated with a different user";
                return RedirectToAction("Login", "Account");
            }

            var vm = new AutoReviewRuleViewModel
            {
                Id = rule.Id,
                Priority = rule.Priority,
                AutoDecision = rule.AutoDecision,
                ComparisonVar = rule.ComparisonVar,
                ComparisonOp = rule.ComparisonOp,
                ComparisonValue = rule.ComparisonValue,
                AutoComment = rule.AutoComment,
            };

            ViewBag.Decisions = Enum.GetValues<ClaimDecision>()
                .Where(x =>
                    User.IsInRole("ProgramCoordinator") && User.IsInRole("AcademicManager") // Either both roles
                    || ( // Or one role with appropriate decisions
                        User.IsInRole("ProgramCoordinator")
                            ? x != ClaimDecision.APPROVED
                            : !User.IsInRole("AcademicManager") || x != ClaimDecision.VERIFIED
                    )
                )
                .Select(x => new SelectListItem { Text = x.ToString(), Value = x.ToString() });
            ViewBag.ComparisonVars = Enum.GetValues<AutoReviewComparisonVar>()
                .Select(x => new SelectListItem { Text = x.ToString(), Value = x.ToString() });
            ViewBag.ComparisonOps = Enum.GetValues<AutoReviewComparisonOp>()
                .Select(x => new SelectListItem { Text = x.ToString(), Value = x.ToString() });

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> EditRule(AutoReviewRuleViewModel model)
        {
            var user = await _userService.GetUserAsync(User.Identity?.Name);
            var rule = await _reviewerClaimService.GetAutoReviewRule(model.Id);
            if (rule == null)
                return NotFound();
            if (rule.ReviewerId != user!.Id)
            {
                TempData["Error"] = "Error: The AutoReviewRule is associated with a different user";
                return RedirectToAction("Login", "Account");
            }

            rule.Priority = model.Priority;
            rule.AutoDecision = model.AutoDecision;
            rule.ComparisonVar = model.ComparisonVar;
            rule.ComparisonOp = model.ComparisonOp;
            rule.ComparisonValue = model.ComparisonValue;
            rule.AutoComment = model.AutoComment;

            await _reviewerClaimService.UpdateAutoReviewRuleAsync(rule.Id, user.Id, rule);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AutoReview(string? returnUrl = null)
        {
            var user = await _userService.GetUserAsync(User.Identity?.Name);
            var res = await _reviewerClaimService.AutoReviewPendingClaimsAsync(user!.Id);
            if (res.pending <= 0)
            {
                if (res.pending == 0)
                    TempData["Success"] = $"AutoReview has no applicable pending claims to review.";
                if (res.pending == -1)
                    TempData["Error"] = "Error: AutoReview failed due to invalid authorization.";
                else if (res.pending == -2)
                    TempData["Error"] = "Error: AutoReview has no configured review rules.";
            }
            else
            {
                TempData["Success"] =
                    $"AutoReview reviewed {res.reviewed} applicable claims out of {res.pending} pending claims.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}
