using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JirHub.Entities.ViNTD.Models;

namespace JirHub.Services.ViNTD.IServices
{
    /// <summary>
    /// Service for managing project configurations (Jira and GitHub settings).
    /// </summary>
    public interface IProjectConfigService
    {
        /// <summary>
        /// Saves project configuration with encrypted tokens.
        /// </summary>
        Task<(bool Success, string Message, List<string> Errors)> SaveProjectConfigAsync(
            int groupId,
            string jiraUrl,
            string jiraEmail,
            string jiraApiToken,
            string jiraProjectKey,
            string githubToken,
            List<string> githubRepoUrls);

        Task<(bool Success, string Message, List<string> Errors)> SaveGithubTokenAsync(
            int groupId,
            string githubToken);
        
        Task<(bool Success, string Message, List<string> Errors)> SaveJiraConfigAsync(
            int groupId,
            string jiraUrl,
            string jiraEmail,
            string jiraApiToken,
            string jiraProjectKey);

        /// <summary>
        /// Gets project configuration by group ID (with decrypted tokens).
        /// </summary>
        Task<ProjectConfigsViNtd> GetByGroupIdAsync(int groupId);

        /// <summary>

        /// Deletes project configuration and repositories for a group ID.
        /// </summary>
        Task<bool> DeleteProjectConfigAsync(int groupId);

        /// <summary>
        /// Gets all groups with their project configuration and active repositories, optionally filtered by search query.
        /// </summary>
        Task<List<ClassGroup>> GetAllGroupsWithConfigsAsync(string searchQuery = null);

        /// <summary>
        /// Gets all semesters.
        /// </summary>
        Task<List<Semester>> GetAllSemestersAsync();

        /// <summary>
        /// Checks if a user (by email) is the leader of a specific group.
        /// </summary>
        Task<bool> IsUserLeaderAsync(string userEmail, int groupId);

        /// <summary>
        /// Gets the Group ID for a user if they are a leader of any group. Returns null if not a leader.
        /// </summary>
        Task<int?> GetGroupIdForLeaderAsync(string userEmail);
    }
}
