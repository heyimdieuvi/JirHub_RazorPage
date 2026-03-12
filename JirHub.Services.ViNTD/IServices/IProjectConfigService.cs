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
            string githubRepoUrl);

        /// <summary>
        /// Gets project configuration by group ID (with decrypted tokens).
        /// </summary>
        Task<ProjectConfigsViNtd> GetByGroupIdAsync(int groupId);
    }
}
