using JirHub.MVCWebApp.ViNTD.Models;
using JirHub.Services.ViNTD.IServices;
using Microsoft.AspNetCore.Mvc;

namespace JirHub.MVCWebApp.ViNTD.Controllers
{
    /// <summary>
    /// Controller for managing project configurations (Jira and GitHub integrations).
    /// Implements Member 2 requirements: Secure configuration with encryption and verification.
    /// </summary>
    public class ProjectConfigController : Controller
    {
        private readonly IProjectConfigService _projectConfigService;
        private readonly ILogger<ProjectConfigController> _logger;

        public ProjectConfigController(
            IProjectConfigService projectConfigService,
            ILogger<ProjectConfigController> logger)
        {
            _projectConfigService = projectConfigService;
            _logger = logger;
        }

        /// <summary>
        /// GET: Display the configuration form
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// POST: SaveSettings
        /// Receives configuration data, validates, verifies connectivity, encrypts, and persists.
        /// 
        /// Security Features:
        /// - Validates all inputs with Data Annotations
        /// - Verifies Jira and GitHub connectivity before saving
        /// - Encrypts tokens using IDataProtectionProvider
        /// - Never logs or returns plain text tokens
        /// - Provides meaningful error messages for failed connections
        /// </summary>
        /// <param name="dto">The configuration data from the UI</param>
        /// <returns>JSON response indicating success or failure with detailed errors</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings([FromBody] SaveProjectConfigDto dto)
        {
            try
            {
                // Step 1: Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new SaveProjectConfigResponseDto
                    {
                        Success = false,
                        Message = "Validation failed. Please check your inputs.",
                        Errors = errors
                    });
                }

                // Step 2: Log the attempt (without sensitive data)
                _logger.LogInformation("Processing SaveSettings request for GroupId: {GroupId}", dto.GroupId);

                // Step 3: Call service to validate URLs, verify connections, encrypt, and save
                var result = await _projectConfigService.SaveProjectConfigAsync(
                    dto.GroupId,
                    dto.JiraUrl,
                    dto.JiraEmail,
                    dto.JiraApiToken,
                    dto.JiraProjectKey,
                    dto.GithubToken,
                    dto.GithubRepoUrl
                );

                // Step 4: Return appropriate response
                if (result.Success)
                {
                    _logger.LogInformation("Successfully saved configuration for GroupId: {GroupId}", dto.GroupId);

                    return Ok(new SaveProjectConfigResponseDto
                    {
                        Success = true,
                        Message = result.Message,
                        Errors = new List<string>()
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to save configuration for GroupId: {GroupId}. Errors: {Errors}",
                        dto.GroupId, string.Join("; ", result.Errors));

                    return BadRequest(new SaveProjectConfigResponseDto
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the exception (without sensitive data)
                _logger.LogError(ex, "Unexpected error in SaveSettings for GroupId: {GroupId}", dto.GroupId);

                return StatusCode(500, new SaveProjectConfigResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred. Please try again later.",
                    Errors = new List<string> { "Internal server error. Contact support if the issue persists." }
                });
            }
        }

        /// <summary>
        /// POST: VerifyJiraConnection
        /// Endpoint to test Jira connection without saving.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> VerifyJiraConnection(
            [FromBody] VerifyJiraDto dto,
            [FromServices] IConnectionService connectionService)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input" });
            }

            var result = await connectionService.VerifyJiraAsync(
                dto.JiraUrl, dto.JiraEmail, dto.JiraApiToken, dto.JiraProjectKey);

            return Ok(new
            {
                success = result.IsSuccess,
                message = result.IsSuccess ? "Jira connection successful!" : result.ErrorMessage
            });
        }

        /// <summary>
        /// POST: VerifyGitHubConnection
        /// Endpoint to test GitHub connection without saving.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> VerifyGitHubConnection(
            [FromBody] VerifyGitHubDto dto,
            [FromServices] IConnectionService connectionService)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input" });
            }

            var result = await connectionService.VerifyGithubAsync(dto.GithubToken);

            return Ok(new
            {
                success = result.IsSuccess,
                message = result.IsSuccess ? "GitHub connection successful!" : result.ErrorMessage
            });
        }
    }

    #region Helper DTOs

    /// <summary>
    /// DTO for testing Jira connection independently.
    /// </summary>
    public class VerifyJiraDto
    {
        public string JiraUrl { get; set; }
        public string JiraEmail { get; set; }
        public string JiraApiToken { get; set; }
        public string JiraProjectKey { get; set; }
    }

    /// <summary>
    /// DTO for testing GitHub connection independently.
    /// </summary>
    public class VerifyGitHubDto
    {
        public string GithubToken { get; set; }
    }

    #endregion
}
