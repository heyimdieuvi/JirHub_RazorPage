using JirHub.MVCWebApp.ViNTD.Models;
using JirHub.Services.ViNTD.IServices;
using Microsoft.AspNetCore.Mvc;

namespace JirHub.MVCWebApp.ViNTD.Controllers
{
    using System.Security.Claims;
    using JirHub.MVCWebApp.ViNTD.Models;
    using JirHub.Services.ViNTD.IServices;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Controller for managing project configurations (Jira and GitHub integrations).
    /// RESTRICTED: Only accessible by Group Leaders (Students with IsLeader=true).
    /// </summary>
    [Authorize]
    public class ProjectConfigController : Controller
    {
        private readonly IProjectConfigService _projectConfigService;
        private readonly IProjectRepoService _projectRepoService;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<ProjectConfigController> _logger;

        public ProjectConfigController(
            IProjectConfigService projectConfigService,
            IProjectRepoService projectRepoService,
            IEncryptionService encryptionService,
            ILogger<ProjectConfigController> logger)
        {
            _projectConfigService = projectConfigService;
            _projectRepoService = projectRepoService;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        /// <summary>
        /// GET: Display the configuration DETAILS (Read-only)
        /// Leaders see this dashboard first.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

                var leaderGroupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);
                if (!leaderGroupId.HasValue)
                {
                    TempData["ErrorMessage"] = "Access Denied: You must be a Group Leader to access project settings.";
                    return RedirectToAction("Index", "Home"); 
                }

                // Load existing config
                var config = await _projectConfigService.GetByGroupIdAsync(leaderGroupId.Value);
                if (config == null || config.ConfigId <= 0)
                {
                    // No config exists -> Redirect to the monolithic Setup/Edit page
                    return RedirectToAction("Edit");
                }

                var model = new SaveProjectConfigDto 
                { 
                    GroupId = leaderGroupId.Value,
                    JiraUrl = config.JiraUrl,
                    JiraEmail = config.JiraEmail,
                    JiraProjectKey = config.JiraProjectKey,
                    // Mask tokens
                    JiraApiToken = "********", 
                    GithubToken = "********"
                };

                var repos = await _projectRepoService.GetReposByGroupIdAsync(leaderGroupId.Value);
                if (repos != null && repos.Any())
                {
                    model.GithubRepoUrls = repos.Select(r => r.RepoUrl).ToList();
                }

                return View(model); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ProjectConfig Index");
                return RedirectToAction("Error", "Home");
            }
        }
        
        /// <summary>
        /// GET: Form to edit ONLY Jira settings.
        /// </summary>
        public async Task<IActionResult> EditJira()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Name);
            var groupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);
            if (!groupId.HasValue) return Forbid();

            var model = new SaveProjectConfigDto { GroupId = groupId.Value };
            var config = await _projectConfigService.GetByGroupIdAsync(groupId.Value);
            
            if (config != null)
            {
                model.JiraUrl = config.JiraUrl;
                model.JiraEmail = config.JiraEmail;
                model.JiraProjectKey = config.JiraProjectKey;
                model.JiraApiToken = _encryptionService.Unprotect(config.JiraApiToken);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveJira(SaveProjectConfigDto dto)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Name);
            var groupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);
            if (!groupId.HasValue || groupId.Value != dto.GroupId) return Forbid();

            // Minimal validation for Jira fields
            if (string.IsNullOrEmpty(dto.JiraUrl) || string.IsNullOrEmpty(dto.JiraApiToken))
            {
                ModelState.AddModelError("", "Jira URL and Token are required.");
                return View("EditJira", dto);
            }

            var result = await _projectConfigService.SaveJiraConfigAsync(
                dto.GroupId, dto.JiraUrl, dto.JiraEmail, dto.JiraApiToken, dto.JiraProjectKey);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Jira configuration saved.";
                return RedirectToAction("Index");
            }
            
            TempData["ErrorMessage"] = result.Message;
            return View("EditJira", dto);
        }

        /// <summary>
        /// GET: Form to edit ONLY GitHub Token.
        /// </summary>
        public async Task<IActionResult> EditGithubToken()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Name);
            var groupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);
            if (!groupId.HasValue) return Forbid();

            var model = new SaveProjectConfigDto { GroupId = groupId.Value };
            var config = await _projectConfigService.GetByGroupIdAsync(groupId.Value);
            
            if (config != null)
            {
                model.GithubToken = _encryptionService.Unprotect(config.GithubToken);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGithubToken(SaveProjectConfigDto dto)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Name);
            var groupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);
            if (!groupId.HasValue || groupId.Value != dto.GroupId) return Forbid();

            if (string.IsNullOrEmpty(dto.GithubToken))
            {
                ModelState.AddModelError("", "GitHub Token is required.");
                return View("EditGithubToken", dto);
            }

            var result = await _projectConfigService.SaveGithubTokenAsync(dto.GroupId, dto.GithubToken);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "GitHub Token updated.";
                return RedirectToAction("Index");
            }
            
            TempData["ErrorMessage"] = result.Message;
            return View("EditGithubToken", dto);
        }


        /// <summary>
        /// GET: Display the configuration FORM (Edit/Create)
        /// </summary>
        public async Task<IActionResult> Edit()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

                var leaderGroupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);
                if (!leaderGroupId.HasValue) return RedirectToAction("Index"); // Should be caught by Index mainly

                var model = new SaveProjectConfigDto { GroupId = leaderGroupId.Value };

                // Load existing config if available for pre-filling
                var config = await _projectConfigService.GetByGroupIdAsync(leaderGroupId.Value);
                if (config != null && config.ConfigId > 0)
                {
                    model.JiraUrl = config.JiraUrl;
                    model.JiraEmail = config.JiraEmail;
                    model.JiraProjectKey = config.JiraProjectKey;
                    model.JiraApiToken = _encryptionService.Unprotect(config.JiraApiToken);
                    model.GithubToken = _encryptionService.Unprotect(config.GithubToken);

                    var repos = await _projectRepoService.GetReposByGroupIdAsync(leaderGroupId.Value);
                    if (repos != null && repos.Any())
                    {
                        model.GithubRepoUrls = repos.Select(r => r.RepoUrl).ToList();
                    }
                }

                return View(model); // Renders Views/ProjectConfig/Edit.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ProjectConfig Edit");
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings(SaveProjectConfigDto dto)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var leaderGroupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);
            
            // Security Check: Ensure the group ID being saved matches the user's leadership group
            if (!leaderGroupId.HasValue || leaderGroupId.Value != dto.GroupId)
            {
                _logger.LogWarning($"Unauthorized config save attempt by {userEmail} for Group {dto.GroupId}");
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Validation failed. Please check inputs.";
                return View("Index", dto);
            }

            try
            {
                var result = await _projectConfigService.SaveProjectConfigAsync(
                    dto.GroupId,
                    dto.JiraUrl,
                    dto.JiraEmail,
                    dto.JiraApiToken,
                    dto.JiraProjectKey,
                    dto.GithubToken,
                    dto.GithubRepoUrls
                );

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index)); // Go to Details view
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    TempData["Errors"] = result.Errors;
                    return View("Edit", dto); // Return to Edit form
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveSettings Failed");
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                return View("Edit", dto); // Return to Edit form
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSettings(int groupId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Name);
            var leaderGroupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);

            if (leaderGroupId != groupId)
            {
                return Forbid();
            }

            var success = await _projectConfigService.DeleteProjectConfigAsync(groupId);
            if (success)
            {
                TempData["SuccessMessage"] = "Configuration deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete configuration.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CheckStoredJiraHealth([FromServices] IConnectionService connectionService)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Name);
            var groupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);
            if (!groupId.HasValue) return Forbid();

            var config = await _projectConfigService.GetByGroupIdAsync(groupId.Value);
            if (config == null || string.IsNullOrEmpty(config.JiraApiToken))
                return Ok(new { success = false, message = "Jira not configured." });

            var token = _encryptionService.Unprotect(config.JiraApiToken);
            var result = await connectionService.VerifyJiraAsync(config.JiraUrl, config.JiraEmail, token, config.JiraProjectKey);
            
            return Ok(new { success = result.IsSuccess, message = result.IsSuccess ? "Jira Connection Healthy" : $"Jira Error: {result.ErrorMessage}" });
        }

        [HttpPost]
        public async Task<IActionResult> CheckStoredGithubHealth([FromServices] IConnectionService connectionService)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Name);
            var groupId = await _projectConfigService.GetGroupIdForLeaderAsync(userEmail);
            if (!groupId.HasValue) return Forbid();

            var config = await _projectConfigService.GetByGroupIdAsync(groupId.Value);
            if (config == null || string.IsNullOrEmpty(config.GithubToken))
                return Ok(new { success = false, message = "GitHub not configured." });

            var token = _encryptionService.Unprotect(config.GithubToken);
            
            // Basic token check
            var result = await connectionService.VerifyGithubAsync(token);
            
            return Ok(new { success = result.IsSuccess, message = result.IsSuccess ? "GitHub Token Valid" : $"GitHub Error: {result.ErrorMessage}" });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyJiraConnection(
            [FromBody] VerifyJiraDto dto,
            [FromServices] IConnectionService connectionService)
        {
            var result = await connectionService.VerifyJiraAsync(
                dto.JiraUrl, dto.JiraEmail, dto.JiraApiToken, dto.JiraProjectKey);
            
            return Ok(new { success = result.IsSuccess, message = result.IsSuccess ? "Connected!" : result.ErrorMessage });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyGitHubConnection(
            [FromBody] VerifyGitHubDto dto,
            [FromServices] IConnectionService connectionService)
        {
            if (!string.IsNullOrWhiteSpace(dto.GithubRepoUrl))
            {
                var parsed = ParseGitHub(dto.GithubRepoUrl);
                if (parsed != null)
                {
                    var (owner, repoName) = parsed.Value;
                    var repoResult = await connectionService.VerifyGithubRepoAsync(dto.GithubToken, owner, repoName);
                    return Ok(new { success = repoResult.IsSuccess, message = repoResult.IsSuccess ? "Repo Access Confirmed!" : repoResult.ErrorMessage });
                }
            }
            var result = await connectionService.VerifyGithubAsync(dto.GithubToken);
            return Ok(new { success = result.IsSuccess, message = result.IsSuccess ? "Token Valid!" : result.ErrorMessage });
        }
        
        [HttpPost]
        public async Task<IActionResult> ToggleRepo(int repoId)
        {
             // TODO: Verify ownership of repo via Group -> Leader check
             // For now, assuming if they can hit this endpoint they are authorized via filter, but really need to check repo->group->leader.
             // Implemented in service? 
             var success = await _projectRepoService.ToggleRepoStatusAsync(repoId);
             return Json(new { success });
        }

        private (string Owner, string RepoName)? ParseGitHub(string url)
        {
            try 
            {
                if(url.EndsWith(".git")) url = url[..^4];
                var uri = new Uri(url);
                var segs = uri.Segments.Where(s => s != "/").Select(s => s.Trim('/')).ToArray();
                if (segs.Length >= 2) return (segs[segs.Length-2], segs[segs.Length-1]);
                return null;
            }
            catch { return null; }
        }
    }

    public class VerifyJiraDto
    {
        public string JiraUrl { get; set; }
        public string JiraEmail { get; set; }
        public string JiraApiToken { get; set; }
        public string JiraProjectKey { get; set; }
    }

    public class VerifyGitHubDto
    {
        public string GithubToken { get; set; }
        public string? GithubRepoUrl { get; set; }
    }
}
