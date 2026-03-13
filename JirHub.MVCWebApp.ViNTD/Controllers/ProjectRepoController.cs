using JirHub.MVCWebApp.ViNTD.Models;
using JirHub.Services.ViNTD.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JirHub.MVCWebApp.ViNTD.Controllers
{
    [Authorize]
    public class ProjectRepoController : Controller
    {
        private readonly IProjectRepoService _projectRepoService;
        private readonly IProjectConfigService _projectConfigService;
        private readonly ILogger<ProjectRepoController> _logger;

        public ProjectRepoController(
            IProjectRepoService projectRepoService,
            IProjectConfigService projectConfigService,
            ILogger<ProjectRepoController> logger)
        {
            _projectRepoService = projectRepoService;
            _projectConfigService = projectConfigService;
            _logger = logger;
        }

        public async Task<IActionResult> ManageRepos()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Name);
            var groupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);
            if (!groupId.HasValue) return Forbid();

            var repos = await _projectRepoService.GetReposByGroupIdAsync(groupId.Value);
            ViewData["GroupId"] = groupId.Value;
            
            return View(repos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRepo(int groupId, string repoUrl)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Name);
            var leaderGroupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);
            if (!leaderGroupId.HasValue || leaderGroupId.Value != groupId) return Forbid();

            var result = await _projectRepoService.AddGithubRepoAsync(groupId, repoUrl);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Repository added.";
            }
            else 
            {
                TempData["ErrorMessage"] = $"{result.Message}: {result.Error}";
            }
            
            return RedirectToAction("ManageRepos");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRepo(int repoId)
        {
            // Ideally check ownership here or in service by passing user/group context
            await _projectRepoService.DeleteRepoAsync(repoId);
            TempData["SuccessMessage"] = "Repository removed.";
            return RedirectToAction("ManageRepos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRepoStatus(int repoId)
        {
            await _projectRepoService.ToggleRepoStatusAsync(repoId);
             // Return JSON for AJAX or redirect
            return RedirectToAction("ManageRepos");
        }
    }
}
