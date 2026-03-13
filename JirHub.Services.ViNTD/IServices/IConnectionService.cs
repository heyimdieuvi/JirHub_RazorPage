using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JirHub.Services.ViNTD.IServices
{
    /// <summary>
    /// Service for verifying connectivity to external APIs (Jira and GitHub).
    /// </summary>
    public interface IConnectionService
    {
        /// <summary>
        /// Verifies Jira connectivity by attempting to authenticate with provided credentials.
        /// </summary>
        /// <param name="jiraUrl">The Jira instance URL (e.g., https://yourcompany.atlassian.net).</param>
        /// <param name="jiraEmail">The email address for Jira authentication.</param>
        /// <param name="jiraToken">The Jira API token.</param>
        /// <param name="projectKey">The Jira project key to verify access.</param>
        /// <returns>A tuple containing success status and error message if failed.</returns>
        Task<(bool IsSuccess, string ErrorMessage)> VerifyJiraAsync(
            string jiraUrl, 
            string jiraEmail, 
            string jiraToken, 
            string projectKey);

        /// <summary>
        /// Verifies GitHub token validity by calling the /user endpoint.
        /// </summary>
        Task<(bool IsSuccess, string ErrorMessage)> VerifyGithubAsync(string githubToken);

        /// <summary>
        /// Verifies GitHub token has access to a specific repository.
        /// </summary>
        Task<(bool IsSuccess, string ErrorMessage)> VerifyGithubRepoAsync(string githubToken, string owner, string repo);
    }
}
