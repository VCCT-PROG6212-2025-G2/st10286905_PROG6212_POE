// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/691e2fd9-3b94-800b-8e15-7ec9b0caa711

using ContractMonthlyClaimSystem.Controllers;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.Auth;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;

namespace ContractMonthlyClaimSystem.Tests.Controllers
{
    public class AutoReviewRulesControllerTests
    {
        private readonly Mock<IReviewerClaimService> _reviewerClaimServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly AutoReviewRulesController _controller;

        public AutoReviewRulesControllerTests()
        {
            _reviewerClaimServiceMock = new Mock<IReviewerClaimService>();
            _userServiceMock = new Mock<IUserService>();

            _controller = new AutoReviewRulesController(
                _reviewerClaimServiceMock.Object,
                _userServiceMock.Object
            );

            // Logged-in ProgramCoordinator + AcademicManager
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.Name, "reviewer@cmcs.app"),
                        new Claim(ClaimTypes.Role, "ProgramCoordinator"),
                        new Claim(ClaimTypes.Role, "AcademicManager"),
                    },
                    "mock"
                )
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            _controller.TempData = new TempDataDictionary(
                _controller.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>()
            );

            _userServiceMock
                .Setup(s => s.GetUserAsync("reviewer@cmcs.app"))
                .ReturnsAsync(new AppUser { Id = 20, FirstName = "Review", LastName = "User" });
        }

        // ---------------------------------------------------------
        // INDEX
        // ---------------------------------------------------------
        [Fact]
        public async Task Index_ReturnsViewWithRules()
        {
            var ruleList = new List<AutoReviewRule>
            {
                new() { Id = 1, Priority = 5, ComparisonValue = 20, ReviewerId = 20 },
                new() { Id = 2, Priority = 1, ComparisonValue = 10, ReviewerId = 20 },
            };

            _reviewerClaimServiceMock
                .Setup(s => s.GetAutoReviewRulesForUserAsync(20))
                .ReturnsAsync(ruleList);

            var result = await _controller.Index() as ViewResult;

            Assert.NotNull(result);

            var vm = Assert.IsType<AutoReviewRulesIndexViewModel>(result.Model);

            Assert.Equal(2, vm.Rules.Count);
            Assert.Equal(5, vm.Rules.First().Priority); // ordered descending
        }

        // ---------------------------------------------------------
        // ADD RULE
        // ---------------------------------------------------------
        [Fact]
        public async Task AddRule_AddsRule_AndRedirects()
        {
            var model = new AutoReviewRulesIndexViewModel
            {
                NewRule = new AutoReviewRule()
            };

            var result = await _controller.AddRule(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(AutoReviewRulesController.Index), result.ActionName);

            _reviewerClaimServiceMock.Verify(
                s => s.AddAutoReviewRuleAsync(It.Is<AutoReviewRule>(r => r.ReviewerId == 20)),
                Times.Once
            );
        }

        // ---------------------------------------------------------
        // REMOVE RULE
        // ---------------------------------------------------------
        [Fact]
        public async Task RemoveRule_Removes_AndRedirects()
        {
            var result = await _controller.RemoveRule(15) as RedirectToActionResult;

            Assert.NotNull(result);

            _reviewerClaimServiceMock.Verify(
                s => s.RemoveAutoReviewRuleAsync(15, 20),
                Times.Once
            );
        }

        // ---------------------------------------------------------
        // UPDATE RULE PRIORITY
        // ---------------------------------------------------------
        [Fact]
        public async Task UpdateRulePriority_Updates_WhenValid()
        {
            var rule = new AutoReviewRule { Id = 3, ReviewerId = 20, Priority = 1 };

            _reviewerClaimServiceMock.Setup(s => s.GetAutoReviewRule(3)).ReturnsAsync(rule);

            var result = await _controller.UpdateRulePriority(3, 10) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(AutoReviewRulesController.Index), result.ActionName);

            _reviewerClaimServiceMock.Verify(
                s => s.UpdateAutoReviewRuleAsync(3, 20, rule),
                Times.Once
            );

            Assert.Equal(10, rule.Priority);
        }

        [Fact]
        public async Task UpdateRulePriority_ReturnsNotFound_WhenMissing()
        {
            _reviewerClaimServiceMock.Setup(s => s.GetAutoReviewRule(999)).ReturnsAsync((AutoReviewRule?)null);

            var result = await _controller.UpdateRulePriority(999, 5);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // INCREASE PRIORITY
        // ---------------------------------------------------------
        [Fact]
        public async Task IncreaseRulePriority_Increases_AndRedirects()
        {
            var rule = new AutoReviewRule { Id = 7, ReviewerId = 20, Priority = 3 };

            _reviewerClaimServiceMock.Setup(s => s.GetAutoReviewRule(7)).ReturnsAsync(rule);

            var result = await _controller.IncreaseRulePriority(7) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(4, rule.Priority);

            _reviewerClaimServiceMock.Verify(
                s => s.UpdateAutoReviewRuleAsync(7, 20, rule),
                Times.Once
            );
        }

        // ---------------------------------------------------------
        // DECREASE PRIORITY
        // ---------------------------------------------------------
        [Fact]
        public async Task DecreaseRulePriority_Decreases_AndRedirects()
        {
            var rule = new AutoReviewRule { Id = 9, ReviewerId = 20, Priority = 4 };

            _reviewerClaimServiceMock.Setup(s => s.GetAutoReviewRule(9)).ReturnsAsync(rule);

            var result = await _controller.DecreaseRulePriority(9) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(3, rule.Priority);

            _reviewerClaimServiceMock.Verify(
                s => s.UpdateAutoReviewRuleAsync(9, 20, rule),
                Times.Once
            );
        }

        // ---------------------------------------------------------
        // EDIT RULE (GET)
        // ---------------------------------------------------------
        [Fact]
        public async Task EditRule_Get_ReturnsView_WhenValid()
        {
            var rule = new AutoReviewRule
            {
                Id = 11,
                ReviewerId = 20,
                Priority = 3,
                AutoComment = "test"
            };

            _reviewerClaimServiceMock
                .Setup(s => s.GetAutoReviewRule(11))
                .ReturnsAsync(rule);

            var result = await _controller.EditRule(11) as ViewResult;

            Assert.NotNull(result);

            var vm = Assert.IsType<AutoReviewRuleViewModel>(result.Model);
            Assert.Equal("test", vm.AutoComment);
        }

        [Fact]
        public async Task EditRule_Get_ReturnsNotFound_WhenMissing()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.GetAutoReviewRule(123))
                .ReturnsAsync((AutoReviewRule?)null);

            var result = await _controller.EditRule(123);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------------------------------------------------
        // EDIT RULE (POST)
        // ---------------------------------------------------------
        [Fact]
        public async Task EditRule_Post_Updates_AndRedirects()
        {
            var rule = new AutoReviewRule
            {
                Id = 50,
                ReviewerId = 20,
                Priority = 1
            };

            var model = new AutoReviewRuleViewModel
            {
                Id = 50,
                Priority = 8,
                AutoComment = "updated comment"
            };

            _reviewerClaimServiceMock
                .Setup(s => s.GetAutoReviewRule(50))
                .ReturnsAsync(rule);

            var result = await _controller.EditRule(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(AutoReviewRulesController.Index), result.ActionName);

            Assert.Equal(8, rule.Priority);
            Assert.Equal("updated comment", rule.AutoComment);

            _reviewerClaimServiceMock.Verify(
                s => s.UpdateAutoReviewRuleAsync(50, 20, rule),
                Times.Once
            );
        }

        // ---------------------------------------------------------
        // AUTO REVIEW
        // ---------------------------------------------------------
        [Fact]
        public async Task AutoReview_Runs_AndRedirects()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.AutoReviewPendingClaimsAsync(20))
                .ReturnsAsync((pending: 5, reviewed: 2));

            var result = await _controller.AutoReview(null) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal(nameof(AutoReviewRulesController.Index), result.ActionName);
            Assert.Contains("reviewed 2 applicable claims", _controller.TempData["Success"]!.ToString());
        }

        [Fact]
        public async Task AutoReview_NoPendingClaims_SetsSuccessMessage()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.AutoReviewPendingClaimsAsync(20))
                .ReturnsAsync((pending: 0, reviewed: 0));

            var result = await _controller.AutoReview();

            Assert.NotNull(result);
            Assert.Equal("AutoReview has no applicable pending claims to review.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task AutoReview_NoRules_SetsError()
        {
            _reviewerClaimServiceMock
                .Setup(s => s.AutoReviewPendingClaimsAsync(20))
                .ReturnsAsync((pending: -2, reviewed: 0));

            await _controller.AutoReview();

            Assert.Equal("Error: AutoReview has no configured review rules.", _controller.TempData["Error"]);
        }
    }
}
