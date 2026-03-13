using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JirHub.Entities.ViNTD.Models;
using JirHub.Repository.ViNTD.Repositories;
using JirHub.Services.ViNTD.IServices;
using Microsoft.EntityFrameworkCore;

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
        private readonly JirHub.Entities.ViNTD.Models.PRN222_SE1816_DbContext _dbContext;

        public ProjectConfigService(
            ProjectConfigRepository configRepository,
            ProjectReposRepository reposRepository,
            IEncryptionService encryptionService,
            IConnectionService connectionService,
            JirHub.Entities.ViNTD.Models.PRN222_SE1816_DbContext dbContext)
        {
            _configRepository = configRepository;
            _reposRepository = reposRepository;
            _encryptionService = encryptionService;
            _connectionService = connectionService;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Saves Jira configuration ONLY.
        /// </summary>
        public async Task<(bool Success, string Message, List<string> Errors)> SaveJiraConfigAsync(
            int groupId,
            string jiraUrl,
            string jiraEmail,
            string jiraApiToken,
            string jiraProjectKey)
        {
            var errors = new List<string>();

            // Verify Jira connectivity
            var jiraResult = await _connectionService.VerifyJiraAsync(
                jiraUrl, jiraEmail, jiraApiToken, jiraProjectKey);

            if (!jiraResult.IsSuccess)
            {
                errors.Add($"Jira verification failed: {jiraResult.ErrorMessage}");
                return (false, "Jira connection failed", errors);
            }

            var encryptedJiraToken = _encryptionService.Protect(jiraApiToken);

            var config = await _configRepository.GetByGroupIdAsync(groupId);
            if (config == null)
            {
                config = new ProjectConfigsViNtd { GroupId = groupId };
            }

            config.JiraUrl = jiraUrl.TrimEnd('/');
            config.JiraEmail = jiraEmail;
            config.JiraApiToken = encryptedJiraToken;
            config.JiraProjectKey = jiraProjectKey;

            await _configRepository.UpsertAsync(config);
            return (true, "Jira configuration saved successfully.", errors);
        }

        /// <summary>
        /// Saves GitHub Token ONLY.
        /// </summary>
        public async Task<(bool Success, string Message, List<string> Errors)> SaveGithubTokenAsync(
            int groupId,
            string githubToken)

        {
            var errors = new List<string>();

            // Simple token verification (check user profile)
            var result = await _connectionService.VerifyGithubAsync(githubToken);
            if (!result.IsSuccess)
            {
                errors.Add($"GitHub token invalid: {result.ErrorMessage}");
                return (false, "GitHub token verification failed", errors);
            }

            var encryptedToken = _encryptionService.Protect(githubToken);

            var config = await _configRepository.GetByGroupIdAsync(groupId);
            if (config == null)
            {
                config = new ProjectConfigsViNtd { GroupId = groupId };
            }
            
            config.GithubToken = encryptedToken;
            // config.Repositories might need to be re-verified or deactivated if token changes?
            // For now, assume user knows what they are doing.
            
            await _configRepository.UpsertAsync(config);
            
            return (true, "GitHub token updated successfully.", errors);
        }


        /// <summary>
        /// Saves project configuration with full validation and encryption.
        /// </summary>
        public async Task<(bool Success, string Message, List<string> Errors)> SaveProjectConfigAsync(
            int groupId,
            string jiraUrl,
            string jiraEmail,
            string jiraApiToken,
            string jiraProjectKey,
            string githubToken,
            List<string> githubRepoUrls)
        {
            var errors = new List<string>();

            try
            {
                // Step 1: Parse and Validate GitHub URLs
                var validRepos = new List<(string Owner, string RepoName, string OriginalUrl)>();

                // Use a HashSet to ensure unique URLs
                var processedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var url in githubRepoUrls)
                {
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    if (processedUrls.Contains(url)) continue; // Skip duplicates

                    var parsed = ParseGitHubUrl(url);
                    if (parsed == null)
                    {
                        errors.Add($"Invalid GitHub URL: {url}");
                    }
                    else
                    {
                        validRepos.Add((parsed.Value.Owner, parsed.Value.RepoName, url));
                        processedUrls.Add(url);
                    }
                }

                if (errors.Any())
                {
                     return (false, "Validation failed for one or more repositories", errors);
                }

                if (!validRepos.Any())
                {
                    errors.Add("No valid GitHub repository URLs provided.");
                    return (false, "Validation failed", errors);
                }

                // Step 2: Verify Jira connectivity
                var jiraResult = await _connectionService.VerifyJiraAsync(
                    jiraUrl, jiraEmail, jiraApiToken, jiraProjectKey);

                if (!jiraResult.IsSuccess)
                {
                    errors.Add($"Jira verification failed: {jiraResult.ErrorMessage}");
                }

                // Step 3: Verify GitHub token has access to ALL repositories
                foreach (var repo in validRepos)
                {
                    var githubResult = await _connectionService.VerifyGithubRepoAsync(githubToken, repo.Owner, repo.RepoName);
                    if (!githubResult.IsSuccess)
                    {
                        errors.Add($"GitHub verification failed for {repo.OriginalUrl}: {githubResult.ErrorMessage}");
                    }
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

                // Then add/update each repository
                foreach (var repoInfo in validRepos)
                {
                    var repo = new ProjectReposViNtd
                    {
                        GroupId = groupId,
                        RepoName = repoInfo.RepoName,
                        RepoUrl = repoInfo.OriginalUrl.TrimEnd('/'),
                        IsActive = true
                    };
                    await _reposRepository.UpsertAsync(repo);
                }

                return (true, "Project configuration saved successfully for " + validRepos.Count + " repositories.", errors);
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
        /// Deletes project configuration and deactivates/deletes repositories for a group ID.
        /// </summary>
        public async Task<bool> DeleteProjectConfigAsync(int groupId)
        {
            try
            {
                // Deactivate repositories first
                await _reposRepository.DeactivateAllForGroupAsync(groupId);

                // Get and delete project config
                var config = await _configRepository.GetByGroupIdAsync(groupId);
                if (config != null)
                {
                    await _configRepository.RemoveAsync(config);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets all groups with their project configuration and active repositories, optionally filtered by search query.
        /// </summary>
        public async Task<List<ClassGroup>> GetAllGroupsWithConfigsAsync(string searchQuery = null)
        {
            // Note: In a real app we would use a dedicated repository for ClassGroup,
            // but for this example we inject the DbContext directly to the service.

            var query = _dbContext.ClassGroups
                .Include(g => g.ProjectConfigsViNtd)
                .Include(g => g.ProjectReposViNtds.Where(r => r.IsActive == true))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerQuery = searchQuery.ToLower();
                query = query.Where(g =>
                    (g.ProjectCode != null && g.ProjectCode.ToLower().Contains(lowerQuery)) ||
                    (g.ProjectConfigsViNtd != null && g.ProjectConfigsViNtd.JiraProjectKey != null && g.ProjectConfigsViNtd.JiraProjectKey.ToLower().Contains(lowerQuery)) ||
                    (g.GroupName != null && g.GroupName.ToLower().Contains(lowerQuery))
                );
            }

            return await query.ToListAsync();
        }

        public async Task<List<Semester>> GetAllSemestersAsync()
        {
            return await _dbContext.Semesters.ToListAsync();
        }

        public async Task<int?> GetGroupIdForLeaderAsync(string userEmail)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return null;

            var membership = await _dbContext.GroupMembers
                .FirstOrDefaultAsync(gm => gm.UserId == user.UserId && gm.IsLeader == true);
            
            return membership?.GroupId;
        }

        public async Task<bool> IsUserLeaderAsync(string userEmail, int groupId)
        {
             var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
             if (user == null) return false;

             return await _dbContext.GroupMembers
                 .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == user.UserId && gm.IsLeader == true);
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
