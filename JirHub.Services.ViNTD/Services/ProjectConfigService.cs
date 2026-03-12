using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JirHub.Entities.ViNTD.Models;
using JirHub.Repository.ViNTD.Repositories;
using JirHub.Services.ViNTD.IServices;

namespace JirHub.Services.ViNTD.Services
{
    /// <summary>
    /// Service implementation for managing project configurations.
    /// Handles encryption, verification, and persistence of Jira and GitHub settings.
    /// </summary>
    public class ProjectConfigService : IProjectConfigService
    {
        private readonly ProjectConfigRepository _configRepository;
        private readonly ProjectReposRepository _reposRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly IConnectionService _connectionService;

        public ProjectConfigService(
            ProjectConfigRepository configRepository,
            ProjectReposRepository reposRepository,
            IEncryptionService encryptionService,
            IConnectionService connectionService)
        {
            _configRepository = configRepository;
            _reposRepository = reposRepository;
            _encryptionService = encryptionService;
            _connectionService = connectionService;
        }

        /// <summary>
        /// Saves project configuration with full validation and encryption.
        /// Steps:
        /// 1. Validate GitHub URL and extract owner/repo
        /// 2. Verify Jira connectivity
        /// 3. Verify GitHub token
        /// 4. Encrypt tokens
        /// 5. Save to database
        /// </summary>
        public async Task<(bool Success, string Message, List<string> Errors)> SaveProjectConfigAsync(
            int groupId,
            string jiraUrl,
            string jiraEmail,
            string jiraApiToken,
            string jiraProjectKey,
            string githubToken,
            string githubRepoUrl)
        {
            var errors = new List<string>();

            try
            {
                // Step 1: Parse GitHub URL
                var parsedGitHub = ParseGitHubUrl(githubRepoUrl);
                if (parsedGitHub == null)
                {
                    errors.Add("Invalid GitHub repository URL format. Expected format: https://github.com/owner/repo");
                    return (false, "Validation failed", errors);
                }

                var (owner, repoName) = parsedGitHub.Value;

                // Step 2: Verify Jira connectivity
                var jiraResult = await _connectionService.VerifyJiraAsync(
                    jiraUrl, jiraEmail, jiraApiToken, jiraProjectKey);

                if (!jiraResult.IsSuccess)
                {
                    errors.Add($"Jira verification failed: {jiraResult.ErrorMessage}");
                }

                // Step 3: Verify GitHub token
                var githubResult = await _connectionService.VerifyGithubAsync(githubToken);

                if (!githubResult.IsSuccess)
                {
                    errors.Add($"GitHub verification failed: {githubResult.ErrorMessage}");
                }

                // If any verification failed, return errors
                if (errors.Any())
                {
                    return (false, "Connection verification failed", errors);
                }

                // Step 4: Encrypt tokens
                var encryptedJiraToken = _encryptionService.Protect(jiraApiToken);
                var encryptedGithubToken = _encryptionService.Protect(githubToken);

                if (string.IsNullOrEmpty(encryptedJiraToken) || string.IsNullOrEmpty(encryptedGithubToken))
                {
                    errors.Add("Failed to encrypt tokens. Please try again.");
                    return (false, "Encryption failed", errors);
                }

                // Step 5: Save project configuration
                var config = new ProjectConfigsViNtd
                {
                    GroupId = groupId,
                    JiraUrl = jiraUrl.TrimEnd('/'),
                    JiraEmail = jiraEmail,
                    JiraApiToken = encryptedJiraToken, // Encrypted
                    JiraProjectKey = jiraProjectKey,
                    GithubToken = encryptedGithubToken // Encrypted
                };

                await _configRepository.UpsertAsync(config);

                // Step 6: Save repository information
                // First, deactivate any existing repos
                await _reposRepository.DeactivateAllForGroupAsync(groupId);

                // Then add the new repository
                var repo = new ProjectReposViNtd
                {
                    GroupId = groupId,
                    RepoName = repoName,
                    RepoUrl = githubRepoUrl.TrimEnd('/'),
                    IsActive = true
                };

                await _reposRepository.UpsertAsync(repo);

                return (true, "Project configuration saved successfully. All credentials verified and encrypted.", errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Unexpected error: {ex.Message}");
                return (false, "Failed to save configuration", errors);
            }
        }

        /// <summary>
        /// Gets project configuration by group ID.
        /// Note: Returns encrypted tokens - use EncryptionService to decrypt if needed.
        /// </summary>
        public async Task<ProjectConfigsViNtd> GetByGroupIdAsync(int groupId)
        {
            return await _configRepository.GetByGroupIdAsync(groupId);
        }

        /// <summary>
        /// Helper method to parse GitHub repository URLs.
        /// </summary>
        private (string Owner, string RepoName)? ParseGitHubUrl(string repoUrl)
        {
            if (string.IsNullOrWhiteSpace(repoUrl))
                return null;

            try
            {
                // Remove .git suffix if present
                repoUrl = repoUrl.TrimEnd('/');
                if (repoUrl.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                {
                    repoUrl = repoUrl.Substring(0, repoUrl.Length - 4);
                }

                // Handle SSH format: git@github.com:owner/repo
                if (repoUrl.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
                {
                    var path = repoUrl.Substring("git@github.com:".Length);
                    var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        return (parts[0], parts[1]);
                    }
                }
                // Handle HTTPS format: https://github.com/owner/repo
                else if (repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(repoUrl);
                    var segments = uri.Segments
                        .Select(s => s.Trim('/'))
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();

                    // Expected format: ["owner", "repo"]
                    if (segments.Length >= 2)
                    {
                        return (segments[segments.Length - 2], segments[segments.Length - 1]);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
