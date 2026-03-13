using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using JirHub.Services.ViNTD.IServices;

namespace JirHub.Services.ViNTD.Services
{
    /// <summary>
    /// Implementation of IConnectionService for verifying Jira and GitHub API connectivity.
    /// Uses HttpClient to perform ping checks with proper authentication.
    /// </summary>
    public class ConnectionService : IConnectionService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ConnectionService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Verifies Jira connectivity by attempting to fetch project information.
        /// Uses Basic Authentication with email and API token.
        /// </summary>
        public async Task<(bool IsSuccess, string ErrorMessage)> VerifyJiraAsync(
            string jiraUrl, 
            string jiraEmail, 
            string jiraToken, 
            string projectKey)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(jiraUrl) || 
                    string.IsNullOrWhiteSpace(jiraEmail) || 
                    string.IsNullOrWhiteSpace(jiraToken) || 
                    string.IsNullOrWhiteSpace(projectKey))
                {
                    return (false, "All Jira fields are required.");
                }

                // Ensure URL doesn't end with slash
                jiraUrl = jiraUrl.TrimEnd('/');

                // Create HTTP client
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                // Create Basic Auth header (email:token in Base64)
                var authBytes = Encoding.UTF8.GetBytes($"{jiraEmail}:{jiraToken}");
                var authHeader = Convert.ToBase64String(authBytes);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

                // Try to fetch the project to verify credentials and access
                var endpoint = $"{jiraUrl}/rest/api/3/project/{projectKey}";
                var response = await client.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                // Handle specific error codes
                return response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => (false, "Jira authentication failed. Please check your email and API token."),
                    HttpStatusCode.Forbidden => (false, "Access forbidden. Your Jira account may not have permission to access this project."),
                    HttpStatusCode.NotFound => (false, $"Jira project '{projectKey}' not found. Please verify the project key."),
                    _ => (false, $"Failed to connect to Jira. Status: {response.StatusCode}. Please check your Jira URL.")
                };
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Network error connecting to Jira: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "Jira connection timed out. Please check your URL and network connection.");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error verifying Jira: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies GitHub token validity by calling the authenticated user endpoint.
        /// Uses Bearer token authentication.
        /// </summary>
        public async Task<(bool IsSuccess, string ErrorMessage)> VerifyGithubAsync(string githubToken)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(githubToken))
                {
                    return (false, "GitHub token is required.");
                }

                // Create HTTP client
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.BaseAddress = new Uri("https://api.github.com");

                // Set required headers for GitHub API
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("JirHub", "1.0"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                // Call the /user endpoint to verify token
                var response = await client.GetAsync("/user");

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                // Handle specific error codes
                return response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => (false, "GitHub token is invalid or expired. Please generate a new personal access token."),
                    HttpStatusCode.Forbidden => (false, "GitHub token lacks required permissions. Ensure it has 'repo' scope."),
                    _ => (false, $"Failed to verify GitHub token. Status: {response.StatusCode}")
                };
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Network error connecting to GitHub: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "GitHub connection timed out. Please check your network connection.");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error verifying GitHub: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies GitHub token has access to a specific repository.
        /// </summary>
        public async Task<(bool IsSuccess, string ErrorMessage)> VerifyGithubRepoAsync(string githubToken, string owner, string repo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(githubToken) || string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
                {
                    return (false, "GitHub token, owner, and repository name are required.");
                }

                // Create HTTP client
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.BaseAddress = new Uri("https://api.github.com");

                // Set required headers for GitHub API
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("JirHub", "1.0"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                // Call the /repos/{owner}/{repo} endpoint
                var response = await client.GetAsync($"/repos/{owner}/{repo}");

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                return response.StatusCode switch
                {
                    HttpStatusCode.NotFound => (false, $"Repository '{owner}/{repo}' not found or token lacks access (404)."),
                    HttpStatusCode.Unauthorized => (false, "GitHub token is invalid or expired."),
                    HttpStatusCode.Forbidden => (false, "Access to repository forbidden (403). Check permissions."),
                    _ => (false, $"Failed to access repository. Status: {response.StatusCode}")
                };
            }
            catch (Exception ex)
            {
                return (false, $"Error verifying GitHub repository: {ex.Message}");
            }
        }
    }
}
