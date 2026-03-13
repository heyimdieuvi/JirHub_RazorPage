using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JirHub.Entities.ViNTD.Models;
using JirHub.Repository.ViNTD.Repositories;
using JirHub.Services.ViNTD.IServices;
using Microsoft.EntityFrameworkCore;

namespace JirHub.Services.ViNTD.Services
{
    public class ProjectRepoService : IProjectRepoService
    {
        private readonly ProjectReposRepository _reposRepository;
        private readonly ProjectConfigRepository _configRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly IConnectionService _connectionService;
        private readonly JirHub.Entities.ViNTD.Models.PRN222_SE1816_DbContext _dbContext;

        public ProjectRepoService(
            ProjectReposRepository reposRepository,
            ProjectConfigRepository configRepository,
            IEncryptionService encryptionService,
            IConnectionService connectionService,
            JirHub.Entities.ViNTD.Models.PRN222_SE1816_DbContext dbContext)
        {
            _reposRepository = reposRepository;
            _configRepository = configRepository;
            _encryptionService = encryptionService;
            _connectionService = connectionService;
            _dbContext = dbContext;
        }

        public async Task<List<ProjectReposViNtd>> GetReposByGroupIdAsync(int groupId)
        {
            return await _reposRepository.GetByGroupIdAsync(groupId);
        }

        public async Task<(bool Success, string Message, string? Error)> AddGithubRepoAsync(int groupId, string repoUrl)
        {
            if (string.IsNullOrWhiteSpace(repoUrl))
                return (false, "URL cannot be empty", "URL is required");

            var config = await _configRepository.GetByGroupIdAsync(groupId);
            if (config == null || string.IsNullOrEmpty(config.GithubToken))
                return (false, "No GitHub token configured", "Configure GitHub Token first");

            var token = _encryptionService.Unprotect(config.GithubToken);
            
            var parsed = ParseGitHubUrl(repoUrl);
            if (parsed == null)
                return (false, "Invalid GitHub URL format", "Invalid URL");

            // Verify access
            var accessResult = await _connectionService.VerifyGithubRepoAsync(token, parsed.Value.Owner, parsed.Value.RepoName);
            if (!accessResult.IsSuccess)
                return (false, "Access Denied or Not Found. Ensure token has access.", accessResult.ErrorMessage);

            // Check duplicate
            var existingRepos = await _reposRepository.GetByGroupIdAsync(groupId);
            if(existingRepos.Any(r => r.RepoUrl.Equals(repoUrl, StringComparison.OrdinalIgnoreCase)))
                 return (false, "Repository already added", "Duplicate URL");

            // Save repo
            var repo = new ProjectReposViNtd
            {
                GroupId = groupId,
                RepoName = parsed.Value.RepoName,
                RepoUrl = repoUrl.TrimEnd('/'),
                IsActive = true
            };

            await _reposRepository.UpsertAsync(repo);
            return (true, "Repository added successfully", null);
        }

        public async Task<bool> DeleteRepoAsync(int repoId)
        {
            var repo = await _dbContext.ProjectReposViNtds.FindAsync(repoId);
            if (repo != null)
            {
                _dbContext.ProjectReposViNtds.Remove(repo);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> ToggleRepoStatusAsync(int repoId)
        {
             var repo = await _dbContext.ProjectReposViNtds.FindAsync(repoId);
             if (repo == null) return false;
             
             repo.IsActive = !repo.IsActive; // Toggle
             await _dbContext.SaveChangesAsync();
             return true;
        }

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

                if (repoUrl.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
                {
                    var path = repoUrl.Substring("git@github.com:".Length);
                    var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        return (parts[0], parts[1]);
                    }
                }
                else if (repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(repoUrl);
                    var segments = uri.Segments
                        .Select(s => s.Trim('/'))
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();

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
