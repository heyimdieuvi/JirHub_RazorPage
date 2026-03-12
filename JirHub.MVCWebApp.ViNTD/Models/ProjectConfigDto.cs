using System.ComponentModel.DataAnnotations;

namespace JirHub.MVCWebApp.ViNTD.Models
{
    /// <summary>
    /// DTO for saving project configuration settings (Jira and GitHub).
    /// Used in the SaveSettings POST action.
    /// </summary>
    public class SaveProjectConfigDto
    {
        [Required(ErrorMessage = "Group ID is required.")]
        public int GroupId { get; set; }

        // Jira Configuration
        [Required(ErrorMessage = "Jira URL is required.")]
        [Url(ErrorMessage = "Please enter a valid Jira URL.")]
        public string JiraUrl { get; set; }

        [Required(ErrorMessage = "Jira email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string JiraEmail { get; set; }

        [Required(ErrorMessage = "Jira API token is required.")]
        public string JiraApiToken { get; set; }

        [Required(ErrorMessage = "Jira project key is required.")]
        [StringLength(20, ErrorMessage = "Project key cannot exceed 20 characters.")]
        public string JiraProjectKey { get; set; }

        // GitHub Configuration
        [Required(ErrorMessage = "GitHub token is required.")]
        public string GithubToken { get; set; }

        [Required(ErrorMessage = "GitHub repository URL is required.")]
        [Url(ErrorMessage = "Please enter a valid GitHub repository URL.")]
        public string GithubRepoUrl { get; set; }
    }

    /// <summary>
    /// Response DTO for SaveSettings action.
    /// </summary>
    public class SaveProjectConfigResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Helper class for parsing GitHub repository URLs.
    /// </summary>
    public static class GitHubUrlParser
    {
        /// <summary>
        /// Extracts owner and repository name from a GitHub URL.
        /// Supports formats:
        /// - https://github.com/owner/repo
        /// - https://github.com/owner/repo.git
        /// - git@github.com:owner/repo.git
        /// </summary>
        /// <param name="repoUrl">The GitHub repository URL.</param>
        /// <returns>A tuple containing (Owner, RepoName), or null if parsing fails.</returns>
        public static (string Owner, string RepoName)? ParseGitHubUrl(string repoUrl)
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

        /// <summary>
        /// Extracts just the repository name from a GitHub URL.
        /// </summary>
        public static string GetRepoName(string repoUrl)
        {
            var parsed = ParseGitHubUrl(repoUrl);
            return parsed?.RepoName;
        }
    }
}
