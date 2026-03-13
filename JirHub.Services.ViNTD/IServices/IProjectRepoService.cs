using JirHub.Entities.ViNTD.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JirHub.Services.ViNTD.IServices
{
    public interface IProjectRepoService
    {
        Task<List<ProjectReposViNtd>> GetReposByGroupIdAsync(int groupId);
        Task<(bool Success, string Message, string? Error)> AddGithubRepoAsync(int groupId, string repoUrl);
        Task<bool> DeleteRepoAsync(int repoId);
        Task<bool> ToggleRepoStatusAsync(int repoId);
    }
}
